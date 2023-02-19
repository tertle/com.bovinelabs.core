// <copyright file="SubSceneLoadingSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.SubScenes
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.App;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Scenes;
    using UnityEngine;
    using UnityEngine.Assertions;

    /// <summary> System that loads our SubScenes depending on the world and the SubScene load mode. </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class SubSceneLoadingSystem : SystemBase
    {
        private readonly Dictionary<SubScene, Entity> requiredScenes = new();
        private readonly Dictionary<SubSceneLoadConfig, Entity> volumes = new();
        private readonly HashSet<Entity> waitingForLoad = new();

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            this.LoadSubScenes();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
#if UNITY_EDITOR
            var loadRequired = false;

            foreach (var s in this.requiredScenes)
            {
                if (!this.IsSceneLoad(s.Value))
                {
                    if (this.waitingForLoad.Add(s.Value))
                    {
                        loadRequired = true;
                    }
                }
            }

            if (loadRequired)
            {
                this.EntityManager.AddComponent<PauseGame>(this.SystemHandle);
            }
#endif

            if (this.waitingForLoad.Count != 0)
            {
                this.WaitToLoad();
            }
        }

        private bool IsSceneLoad(Entity entity)
        {
#if UNITY_EDITOR
            return SceneSystem.IsSceneLoaded(this.World.Unmanaged, entity) && this.EntityManager.HasComponent<LinkedEntityGroup>(entity);
#else
            return SceneSystem.IsSceneLoaded(this.World.Unmanaged, entity);
#endif
        }

        private void LoadSubScenes()
        {
            var debug = SystemAPI.GetSingleton<BLDebug>();
            var flags = this.World.Flags;

            foreach (var subScene in Object.FindObjectsOfType<SubScene>())
            {
                var subSceneLoadConfig = subScene.GetComponent<SubSceneLoadConfig>();

                if (subSceneLoadConfig == null)
                {
                    continue;
                }

                var loadingMode = subSceneLoadConfig.LoadingMode;
                var isRequired = subSceneLoadConfig.IsRequired;
                var targetWorld = subSceneLoadConfig.TargetWorld;

                if ((targetWorld & flags) == 0)
                {
                    continue;
                }

                var isServer = this.World.IsServerWorld();

                var requiredForLoading = loadingMode == SubSceneLoadMode.AutoLoad && isRequired;
                requiredForLoading |= isServer && loadingMode == SubSceneLoadMode.BoundingVolume;

                var loadingParams = default(SceneSystem.LoadParameters);
                loadingParams.AutoLoad = (this.World.IsServerWorld() && loadingMode != SubSceneLoadMode.OnDemand) || loadingMode == SubSceneLoadMode.AutoLoad;
                var entity = SceneSystem.LoadSceneAsync(this.World.Unmanaged, subScene.SceneGUID, loadingParams);
                this.EntityManager.AddComponentObject(entity, subScene);

                if (requiredForLoading)
                {
                    this.waitingForLoad.Add(entity);
                    this.requiredScenes.Add(subScene, entity);
                }

                debug.Debug($"Loading SubScene {subScene.name}\nrequiredForLoading: {requiredForLoading}, loadingMode: {loadingMode}");

                if (!isServer && loadingMode == SubSceneLoadMode.BoundingVolume)
                {
                    this.volumes.Add(subSceneLoadConfig, entity);
                }
            }

            if (this.waitingForLoad.Count > 0)
            {
                this.EntityManager.AddComponent<PauseGame>(this.SystemHandle);
            }
        }

        private void WaitToLoad()
        {
            while (this.waitingForLoad.Any(w => !this.IsSceneLoad(w)))
            {
                return;
            }

            this.waitingForLoad.Clear();

            foreach (var subScene in this.volumes)
            {
                var sections = this.EntityManager.GetBuffer<ResolvedSectionEntity>(subScene.Value).ToNativeArray(Allocator.Temp);
                var bounds = MinMaxAABB.Empty;

                foreach (var section in sections)
                {
                    Assert.IsTrue(
                        this.EntityManager.HasComponent<SceneSectionData>(section.SectionEntity),
                        $"{section.SectionEntity} missing SceneSectionData");

                    var sceneSectionData = this.EntityManager.GetComponentData<SceneSectionData>(section.SectionEntity);
                    bounds.Encapsulate(sceneSectionData.BoundingVolume);
                }

                this.EntityManager.AddComponentData(subScene.Value, new LoadWithBoundingVolume
                {
                    Bounds = bounds,
                    LoadMaxDistanceOverrideSq = subScene.Key.LoadMaxDistanceOverride * subScene.Key.LoadMaxDistanceOverride,
                    UnloadMaxDistanceOverrideSq = subScene.Key.UnloadMaxDistanceOverride * subScene.Key.UnloadMaxDistanceOverride,
                });
            }

            // This only needs to be setup once
            this.volumes.Clear();

            this.EntityManager.RemoveComponent<PauseGame>(this.SystemHandle);

            // In builds once scenes are loaded there is no need to keep checking state
            // However in editor SubScenes can be opened and reloaded so we keep the system running to handle this
#if !UNITY_EDITOR
            this.Enabled = false;
#endif
        }
    }
}
