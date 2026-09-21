namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Authoring.Settings;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;
    using UnityEditor;

    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [UpdateAfter(typeof(SceneSystemGroup))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class EditorSettingsFallbackSystem : SystemBase
    {
        private readonly List<Fallback> _fallbacks = new();
        private EntityQuery _settingsQuery;

        protected override void OnCreate()
        {
            _settingsQuery = GetEntityQuery(ComponentType.ReadOnly<SettingsPrefabIdentity>());

            if (!EditorSettingsUtility.TryGetSettings<EditorSettings>(out var settings))
            {
                World.GetExistingSystemManaged<InitializationSystemGroup>().Enabled = false;
                World.GetExistingSystemManaged<SimulationSystemGroup>().Enabled = false;
                World.GetExistingSystemManaged<PresentationSystemGroup>().Enabled = false;
                BLGlobalLogger.LogErrorString("Could not load EditorSettings, disabling EditorWorld. This should work again after a domain reload");
                return;
            }

            var loaded = new HashSet<Hash128>();
            if (settings.DefaultSettingsAuthoring)
            {
                LoadFallback(settings.DefaultSettingsAuthoring, loaded);
            }

            foreach (var world in settings.AdditionalEditorWorldSettings)
            {
                if (string.IsNullOrWhiteSpace(world))
                {
                    throw new InvalidOperationException("Additional editor world settings keys must not be empty.");
                }

                if (!settings.TryGetAuthoring(world, out var authoring) || !authoring)
                {
                    continue;
                }

                LoadFallback(authoring, loaded);
            }
        }

        protected override void OnDestroy()
        {
            foreach (var fallback in _fallbacks)
            {
                SetActive(fallback, false);
            }
        }

        protected override void OnUpdate()
        {
            foreach (var fallback in _fallbacks)
            {
                RefreshRoot(fallback);
            }

            using var settingsEntities = _settingsQuery.ToEntityArray(Allocator.Temp);
            using var settingsIdentities = _settingsQuery.ToComponentDataArray<SettingsPrefabIdentity>(Allocator.Temp);

            foreach (var fallback in _fallbacks)
            {
                if (!fallback.Valid)
                {
                    continue;
                }

                var authoritativeCount = 0;
                for (var i = 0; i < settingsEntities.Length; i++)
                {
                    if (!fallback.PrefabEntities.Contains(settingsEntities[i]) && settingsIdentities[i].PrefabGuid == fallback.PrefabGuid)
                    {
                        authoritativeCount++;
                    }
                }

                SetActive(fallback, authoritativeCount == 0);

                if (authoritativeCount > 1 && fallback.AuthoritativeCount <= 1)
                {
                    BLGlobalLogger.LogErrorString(
                        $"More than one authoritative instance of editor settings prefab '{fallback.Path}' exists in the Editor world.");
                }

                fallback.AuthoritativeCount = authoritativeCount;
            }
        }

        private void LoadFallback(SettingsAuthoring authoring, HashSet<Hash128> loaded)
        {
            var prefabGuid = SettingsAuthoring.GetPrefabGuid(authoring);
            if (!loaded.Add(prefabGuid))
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(authoring);
            var sceneEntity = SceneSystem.LoadSceneAsync(World.Unmanaged, prefabGuid, new SceneSystem.LoadParameters
            {
                Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn | SceneLoadFlags.NewInstance,
            });

            _fallbacks.Add(new Fallback(prefabGuid, sceneEntity, path));
        }

        private void RefreshRoot(Fallback fallback)
        {
            if (!EntityManager.Exists(fallback.SceneEntity) || !EntityManager.HasComponent<PrefabRoot>(fallback.SceneEntity))
            {
                if (fallback.Root != Entity.Null && !EntityManager.Exists(fallback.Root))
                {
                    fallback.Reset();
                }

                return;
            }

            var root = EntityManager.GetComponentData<PrefabRoot>(fallback.SceneEntity).Root;
            if (root == fallback.Root && EntityManager.Exists(root))
            {
                return;
            }

            SetActive(fallback, false);
            fallback.Reset(root);

            if (!EntityManager.Exists(root) || !EntityManager.HasComponent<SettingsPrefabIdentity>(root))
            {
                throw new InvalidOperationException($"Loaded editor settings prefab '{fallback.Path}' has no settings identity on its root.");
            }

            var identity = EntityManager.GetComponentData<SettingsPrefabIdentity>(root);
            if (identity.PrefabGuid != fallback.PrefabGuid)
            {
                throw new InvalidOperationException($"Loaded editor settings prefab '{fallback.Path}' has an unexpected settings identity.");
            }

            if (EntityManager.HasBuffer<LinkedEntityGroup>(root))
            {
                foreach (var linkedEntity in EntityManager.GetBuffer<LinkedEntityGroup>(root))
                {
                    CapturePrefabEntity(fallback, linkedEntity.Value);
                }
            }
            else
            {
                CapturePrefabEntity(fallback, root);
            }

            if (fallback.PrefabEntities.Count == 0)
            {
                throw new InvalidOperationException($"Loaded editor settings prefab '{fallback.Path}' has no Prefab entities to activate.");
            }

            fallback.Valid = true;
        }

        private void CapturePrefabEntity(Fallback fallback, Entity entity)
        {
            if (EntityManager.Exists(entity) && EntityManager.HasComponent<Prefab>(entity))
            {
                fallback.PrefabEntities.Add(entity);
            }
        }

        private void SetActive(Fallback fallback, bool active)
        {
            foreach (var entity in fallback.PrefabEntities)
            {
                if (!EntityManager.Exists(entity))
                {
                    continue;
                }

                var isPrefab = EntityManager.HasComponent<Prefab>(entity);
                if (active && isPrefab)
                {
                    EntityManager.RemoveComponent<Prefab>(entity);
                }
                else if (!active && !isPrefab)
                {
                    EntityManager.AddComponent<Prefab>(entity);
                }
            }
        }

        private sealed class Fallback
        {
            public Fallback(Hash128 prefabGuid, Entity sceneEntity, string path)
            {
                PrefabGuid = prefabGuid;
                SceneEntity = sceneEntity;
                Path = path;
            }

            public Hash128 PrefabGuid { get; }

            public Entity SceneEntity { get; }

            public string Path { get; }

            public List<Entity> PrefabEntities { get; } = new();

            public Entity Root { get; private set; }

            public bool Valid { get; set; }

            public int AuthoritativeCount { get; set; } = -1;

            public void Reset(Entity root = default)
            {
                Root = root;
                PrefabEntities.Clear();
                Valid = false;
                AuthoritativeCount = -1;
            }
        }
    }
}
