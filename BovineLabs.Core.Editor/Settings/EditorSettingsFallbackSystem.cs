// <copyright file="EditorSettingsFallbackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Authoring.Settings;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;
    using UnityEditor;

    /// <summary> Keeps configured settings prefabs available when their normal SubScene instance is absent from the Editor world. </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [UpdateAfter(typeof(SceneSystemGroup))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class EditorSettingsFallbackSystem : SystemBase
    {
        private readonly List<Fallback> fallbacks = new();
        private EntityQuery settingsQuery;

        /// <inheritdoc />
        protected override void OnCreate()
        {
            this.settingsQuery = this.GetEntityQuery(ComponentType.ReadOnly<SettingsPrefabIdentity>());

            if (!EditorSettingsUtility.TryGetSettings<EditorSettings>(out var settings))
            {
                return;
            }

            var loaded = new HashSet<Hash128>();
            this.LoadFallback(settings.DefaultSettingsAuthoring, loaded);

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

                this.LoadFallback(authoring, loaded);
            }
        }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            foreach (var fallback in this.fallbacks)
            {
                this.SetActive(fallback, false);
            }
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            foreach (var fallback in this.fallbacks)
            {
                this.RefreshRoot(fallback);
            }

            using var settingsEntities = this.settingsQuery.ToEntityArray(Allocator.Temp);
            using var settingsIdentities = this.settingsQuery.ToComponentDataArray<SettingsPrefabIdentity>(Allocator.Temp);

            foreach (var fallback in this.fallbacks)
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

                this.SetActive(fallback, authoritativeCount == 0);

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
            var sceneEntity = SceneSystem.LoadSceneAsync(this.World.Unmanaged, prefabGuid, new SceneSystem.LoadParameters
            {
                Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn | SceneLoadFlags.NewInstance,
            });

            this.fallbacks.Add(new Fallback(prefabGuid, sceneEntity, path));
        }

        private void RefreshRoot(Fallback fallback)
        {
            if (!this.EntityManager.Exists(fallback.SceneEntity) || !this.EntityManager.HasComponent<PrefabRoot>(fallback.SceneEntity))
            {
                if (fallback.Root != Entity.Null && !this.EntityManager.Exists(fallback.Root))
                {
                    fallback.Reset();
                }

                return;
            }

            var root = this.EntityManager.GetComponentData<PrefabRoot>(fallback.SceneEntity).Root;
            if (root == fallback.Root && this.EntityManager.Exists(root))
            {
                return;
            }

            this.SetActive(fallback, false);
            fallback.Reset(root);

            if (!this.EntityManager.Exists(root) || !this.EntityManager.HasComponent<SettingsPrefabIdentity>(root))
            {
                throw new InvalidOperationException($"Loaded editor settings prefab '{fallback.Path}' has no settings identity on its root.");
            }

            var identity = this.EntityManager.GetComponentData<SettingsPrefabIdentity>(root);
            if (identity.PrefabGuid != fallback.PrefabGuid)
            {
                throw new InvalidOperationException($"Loaded editor settings prefab '{fallback.Path}' has an unexpected settings identity.");
            }

            if (this.EntityManager.HasBuffer<LinkedEntityGroup>(root))
            {
                foreach (var linkedEntity in this.EntityManager.GetBuffer<LinkedEntityGroup>(root))
                {
                    this.CapturePrefabEntity(fallback, linkedEntity.Value);
                }
            }
            else
            {
                this.CapturePrefabEntity(fallback, root);
            }

            if (fallback.PrefabEntities.Count == 0)
            {
                throw new InvalidOperationException($"Loaded editor settings prefab '{fallback.Path}' has no Prefab entities to activate.");
            }

            fallback.Valid = true;
        }

        private void CapturePrefabEntity(Fallback fallback, Entity entity)
        {
            if (this.EntityManager.Exists(entity) && this.EntityManager.HasComponent<Prefab>(entity))
            {
                fallback.PrefabEntities.Add(entity);
            }
        }

        private void SetActive(Fallback fallback, bool active)
        {
            foreach (var entity in fallback.PrefabEntities)
            {
                if (!this.EntityManager.Exists(entity))
                {
                    continue;
                }

                var isPrefab = this.EntityManager.HasComponent<Prefab>(entity);
                if (active && isPrefab)
                {
                    this.EntityManager.RemoveComponent<Prefab>(entity);
                }
                else if (!active && !isPrefab)
                {
                    this.EntityManager.AddComponent<Prefab>(entity);
                }
            }
        }

        private sealed class Fallback
        {
            public Fallback(Hash128 prefabGuid, Entity sceneEntity, string path)
            {
                this.PrefabGuid = prefabGuid;
                this.SceneEntity = sceneEntity;
                this.Path = path;
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
                this.Root = root;
                this.PrefabEntities.Clear();
                this.Valid = false;
                this.AuthoritativeCount = -1;
            }
        }
    }
}
