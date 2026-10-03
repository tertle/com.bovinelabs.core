namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;
    using UnityEditor;
    using UnityEngine;
    using Hash128 = Unity.Entities.Hash128;
    using PackageInfo = UnityEditor.PackageManager.PackageInfo;

    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [UpdateAfter(typeof(SceneSystemGroup))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    // Asset discovery, imports and editor-world control require managed Unity APIs.
    public partial class EditorSettingsFallbackSystem : SystemBase
    {
        private EntityQuery _settingsQuery;
        private NativeList<EditorSettingsFallbackState> _fallbacks;
        private bool _initialized;
        private uint _revision;
        private EditorApplication.CallbackFunction _resumeUpdate;

        protected override void OnCreate()
        {
            _settingsQuery = SystemAPI.QueryBuilder().WithAll<SettingsPrefabIdentity>().Build();
            _fallbacks = new NativeList<EditorSettingsFallbackState>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            EditorApplication.update -= _resumeUpdate;

            try
            {
                for (var index = 0; index < _fallbacks.Length; index++)
                {
                    SetActive(EntityManager, ref _fallbacks.ElementAt(index), false);
                }
            }
            finally
            {
                _fallbacks.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                // Pause consumers until the Editor is ready, without treating normal compilation/imports as settings failures.
                var initialization = World.GetExistingSystemManaged<InitializationSystemGroup>();
                var simulation = World.GetExistingSystemManaged<SimulationSystemGroup>();
                var presentation = World.GetExistingSystemManaged<PresentationSystemGroup>();
                var simulationEnabled = simulation.Enabled;
                var presentationEnabled = presentation.Enabled;
                World.QuitUpdate = true;
                initialization.Enabled = false;
                simulation.Enabled = false;
                presentation.Enabled = false;

                _resumeUpdate = () =>
                {
                    if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    {
                        return;
                    }

                    EditorApplication.update -= _resumeUpdate;
                    _resumeUpdate = null;
                    World.QuitUpdate = false;
                    initialization.Enabled = true;
                    simulation.Enabled = simulationEnabled;
                    presentation.Enabled = presentationEnabled;
                    EditorApplication.QueuePlayerLoopUpdate();
                };
                EditorApplication.update += _resumeUpdate;
                return;
            }

            // Asset discovery, import and streaming form one editor boundary. Failure must stop consumers in this frame.
            try
            {
                if (!_initialized || _revision != EditorSettingsAssetPostprocessor.Revision)
                {
                    ReconcileSettings();
                }

                for (var index = 0; index < _fallbacks.Length; index++)
                {
                    RefreshRoot(ref _fallbacks.ElementAt(index));
                }

                var settingsEntities = _settingsQuery.ToEntityArray(Allocator.Temp);
                var settingsIdentities = _settingsQuery.ToComponentDataArray<SettingsPrefabIdentity>(Allocator.Temp);
                foreach (var identity in settingsIdentities)
                {
                    if (!identity.Ready)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(identity.PrefabGuid.ToString());
                        throw new InvalidOperationException($"Editor settings prefab '{path}' did not finish baking all of its settings.");
                    }
                }

                // Resolve every route before changing any fallback instance.
                for (var index = 0; index < _fallbacks.Length; index++)
                {
                    ref var fallback = ref _fallbacks.ElementAt(index);
                    var authoritativeCount = 0;
                    for (var i = 0; i < settingsEntities.Length; i++)
                    {
                        if (settingsEntities[i] != fallback.Instance && settingsIdentities[i].PrefabGuid == fallback.PrefabGuid)
                        {
                            authoritativeCount++;
                        }
                    }

                    if (authoritativeCount > 1)
                    {
                        throw new InvalidOperationException($"More than one authoritative instance of editor settings prefab '{fallback.Path}' exists.");
                    }

                    fallback.AuthoritativeCount = authoritativeCount;
                }

                for (var index = 0; index < _fallbacks.Length; index++)
                {
                    ref var fallback = ref _fallbacks.ElementAt(index);
                    SetActive(EntityManager, ref fallback, fallback.AuthoritativeCount == 0);
                    if (fallback.AuthoritativeCount == 0 && !_settingsQuery.MatchesIgnoreFilter(fallback.Instance))
                    {
                        throw new InvalidOperationException($"Editor settings prefab '{fallback.Path}' produced an inactive fallback. Enable its root before reloading the domain.");
                    }
                }
            }
            catch (Exception exception)
            {
                World.QuitUpdate = true;
                World.GetExistingSystemManaged<InitializationSystemGroup>().Enabled = false;
                World.GetExistingSystemManaged<SimulationSystemGroup>().Enabled = false;
                World.GetExistingSystemManaged<PresentationSystemGroup>().Enabled = false;
                Enabled = false;
                BLGlobalLogger.LogErrorString($"Editor world settings could not be prepared. Previews are disabled; fix the settings and reload the domain.\n{exception}");
            }
        }

        private void ReconcileSettings()
        {
            var revision = EditorSettingsAssetPostprocessor.Revision;
            if (!EditorSettingsUtility.TryGetSettings<EditorSettings>(out var settings))
            {
                throw new InvalidOperationException("EditorSettings could not be loaded. Open BovineLabs > Settings to configure it.");
            }

            foreach (var type in ReflectionUtility.GetAllImplementationsRootOnly<ISettings, ScriptableObject>())
            {
                if (typeof(SettingsBase).IsAssignableFrom(type) && PackageInfo.FindForAssembly(type.Assembly) != null &&
                    !EditorSettingsUtility.TryGetSettings(type, out _))
                {
                    throw new InvalidOperationException($"Package settings asset '{type.FullName}' is missing. Open BovineLabs > Settings to create it.");
                }
            }

            // Open SubScenes completed live baking before this system. Their existing settings may still use the old assignments.
            if (EditorSettingsUtility.UpdateSettings(settings) && !_settingsQuery.IsEmptyIgnoreFilter)
            {
                throw new InvalidOperationException("Settings authoring assignments were repaired while editor settings were already active. Reload the domain to bake the repaired assignments before consumers run.");
            }

            var authorings = new Dictionary<Hash128, SettingsAuthoring>();
            AddAuthoring(settings.DefaultSettingsAuthoring);
            foreach (var world in settings.AdditionalEditorWorldSettings)
            {
                if (string.IsNullOrWhiteSpace(world))
                {
                    throw new InvalidOperationException("Additional editor world settings keys must not be empty.");
                }

                if (!settings.TryGetAuthoring(world, out var authoring) || !authoring)
                {
                    throw new InvalidOperationException($"Additional editor world settings route '{world}' has no settings authoring prefab.");
                }

                AddAuthoring(authoring);
            }

            if (_initialized)
            {
                if (authorings.Count != _fallbacks.Length)
                {
                    throw new InvalidOperationException("The editor world settings prefab routes changed. Reload the domain to load the new routes.");
                }

                foreach (var fallback in _fallbacks)
                {
                    if (!authorings.ContainsKey(fallback.PrefabGuid))
                    {
                        throw new InvalidOperationException("The editor world settings prefab routes changed. Reload the domain to load the new routes.");
                    }
                }
            }
            else
            {
                foreach (var pair in authorings)
                {
                    var sceneEntity = SceneSystem.LoadSceneAsync(World.Unmanaged, pair.Key, new SceneSystem.LoadParameters
                    {
                        Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn | SceneLoadFlags.NewInstance,
                    });

                    _fallbacks.Add(new EditorSettingsFallbackState
                    {
                        PrefabGuid = pair.Key,
                        SceneEntity = sceneEntity,
                    });
                }

                _initialized = true;
            }

            // Initial requests and changed prefab assignments were submitted after the normal scene-group update.
            World.GetExistingSystemManaged<SceneSystemGroup>().Update();
            _revision = revision;
            return;

            void AddAuthoring(SettingsAuthoring authoring)
            {
                var prefabGuid = SettingsAuthoring.GetPrefabGuid(authoring);
                authorings.TryAdd(prefabGuid, authoring);
            }
        }

        private void RefreshRoot(ref EditorSettingsFallbackState fallback)
        {
            var entityManager = EntityManager;
            if (!SceneSystem.IsSceneLoaded(World.Unmanaged, fallback.SceneEntity) || !entityManager.HasComponent<PrefabRoot>(fallback.SceneEntity))
            {
                var streamingState = SceneSystem.GetSceneStreamingState(World.Unmanaged, fallback.SceneEntity);
                throw new InvalidOperationException($"Editor settings prefab '{fallback.Path}' did not finish loading ({streamingState}).");
            }

            var root = entityManager.GetComponentData<PrefabRoot>(fallback.SceneEntity).Root;
            if (!entityManager.HasComponent<SettingsPrefabIdentity>(root))
            {
                throw new InvalidOperationException($"Loaded editor settings prefab '{fallback.Path}' has no settings identity on its root.");
            }

            var identity = entityManager.GetComponentData<SettingsPrefabIdentity>(root);
            if (identity.PrefabGuid != fallback.PrefabGuid)
            {
                throw new InvalidOperationException($"Loaded editor settings prefab '{fallback.Path}' has an unexpected settings identity.");
            }

            if (!identity.Ready)
            {
                throw new InvalidOperationException($"Editor settings prefab '{fallback.Path}' did not finish baking all of its settings.");
            }

            if (!entityManager.HasComponent<Prefab>(root))
            {
                throw new InvalidOperationException($"Loaded editor settings prefab '{fallback.Path}' is not a prefab.");
            }

            if (!entityManager.HasComponent<SettingsPrefabIdentity>(fallback.Instance))
            {
                fallback.Instance = Entity.Null;
            }

            if (root == fallback.Root)
            {
                return;
            }

            if (entityManager.HasBuffer<LinkedEntityGroup>(root))
            {
                foreach (var linkedEntity in entityManager.GetBuffer<LinkedEntityGroup>(root))
                {
                    if (linkedEntity.Value != root && entityManager.HasComponent<SettingsPrefabIdentity>(linkedEntity.Value))
                    {
                        throw new InvalidOperationException($"Editor settings prefab '{fallback.Path}' contains another settings authoring prefab. Use separate routes for settings prefabs.");
                    }
                }
            }

            SetActive(entityManager, ref fallback, false);
            fallback.Root = root;
        }

        private static void SetActive(EntityManager entityManager, ref EditorSettingsFallbackState fallback, bool active)
        {
            if (active)
            {
                if (!entityManager.HasComponent<SettingsPrefabIdentity>(fallback.Instance))
                {
                    fallback.Instance = entityManager.Instantiate(fallback.Root);
                }
            }
            else
            {
                if (entityManager.HasComponent<SettingsPrefabIdentity>(fallback.Instance))
                {
                    entityManager.DestroyEntity(fallback.Instance);
                }

                fallback.Instance = Entity.Null;
            }
        }
    }
}
