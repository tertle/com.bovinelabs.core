// <copyright file="SubSceneLoadingSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using BovineLabs.Core.App;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Groups;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Scenes;
    using UnityEngine;

    /// <summary> System that loads our SubScenes depending on the world and the SubScene load mode. </summary>
    [UpdateInGroup(typeof(AfterSceneSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation | Worlds.Service)]
    [CreateAfter(typeof(BLDebugSystem))]
    public partial struct SubSceneLoadingSystem : ISystem, ISystemStartStop
    {
        private NativeList<Volume> volumes;
        private NativeList<Entity> requiredScenes;
        private NativeHashSet<Entity> waitingForLoad;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.volumes = new NativeList<Volume>(16, Allocator.Persistent);
            this.requiredScenes = new NativeList<Entity>(4, Allocator.Persistent);
            this.waitingForLoad = new NativeHashSet<Entity>(16, Allocator.Persistent);
        }

        /// <inheritdoc/>
        public void OnDestroy(ref SystemState state)
        {
            this.volumes.Dispose();
            this.requiredScenes.Dispose();
            this.waitingForLoad.Dispose();
        }

        /// <inheritdoc />
        public void OnStartRunning(ref SystemState state)
        {
            this.LoadSubScenes(ref state);
        }

        /// <inheritdoc />
        public void OnStopRunning(ref SystemState state)
        {
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
#if UNITY_EDITOR
            var loadRequired = false;

            foreach (var required in this.requiredScenes.AsArray())
            {
                if (!this.IsSceneLoad(ref state, required))
                {
                    if (this.waitingForLoad.Add(required))
                    {
                        loadRequired = true;
                    }
                }
            }

            if (loadRequired)
            {
                PauseGame.Pause(ref state, true);
            }
#endif

            if (this.waitingForLoad.Count != 0)
            {
                this.WaitToLoad(ref state);
            }
        }

        private bool IsSceneLoad(ref SystemState state, Entity entity)
        {
            return SceneSystem.IsSceneLoaded(state.WorldUnmanaged, entity);
        }

        private void LoadSubScenes(ref SystemState state)
        {
            PauseGame.Pause(ref state, true);

            var debug = SystemAPI.GetSingleton<BLDebug>();
            var flags = state.WorldUnmanaged.Flags;

            foreach (var subScene in Object.FindObjectsByType<SubScene>(FindObjectsSortMode.None))
            {
                if (subScene.SceneGUID == default)
                {
                    continue;
                }

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
                    // SubScene OnEnable can load sub scenes before we have a chance to stop it so let's just revert this here
                    SceneSystem.UnloadScene(state.WorldUnmanaged, subScene.SceneGUID, SceneSystem.UnloadParameters.DestroyMetaEntities);
                    continue;
                }

                var isServer = state.WorldUnmanaged.IsServerWorld();

                var requiredForLoading = loadingMode == SubSceneLoadMode.AutoLoad && isRequired;
                requiredForLoading |= isServer && loadingMode == SubSceneLoadMode.BoundingVolume;

                var loadingParams = default(SceneSystem.LoadParameters);
                loadingParams.AutoLoad = (isServer && loadingMode != SubSceneLoadMode.OnDemand) || loadingMode == SubSceneLoadMode.AutoLoad;
                var entity = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, subScene.SceneGUID, loadingParams);
                state.EntityManager.AddComponentObject(entity, subScene);

#if UNITY_EDITOR
                // Only when loaded are names set from entity but for debugging it's nice to have a name now
                var sceneNameFs = default(FixedString64Bytes);
                sceneNameFs.CopyFromTruncated($"Scene: {subScene.SceneName}");
                state.EntityManager.SetName(entity, sceneNameFs);
#endif

                if (requiredForLoading)
                {
                    this.waitingForLoad.Add(entity);
                    this.requiredScenes.Add(entity);

                    state.EntityManager.AddComponent<RequiredSubScene>(entity);
                }

                // We want to create the subscene entity but not request scene loading for volumes
                if (!loadingParams.AutoLoad)
                {
                    state.EntityManager.RemoveComponent<RequestSceneLoaded>(entity);
                }

                debug.Debug($"Loading SubScene {subScene.name}\nrequiredForLoading: {requiredForLoading}, loadingMode: {loadingMode}");

                if (!isServer && loadingMode == SubSceneLoadMode.BoundingVolume)
                {
                    this.volumes.Add(new Volume
                    {
                        Entity = entity,
                        LoadMaxDistanceOverride = subSceneLoadConfig.LoadMaxDistanceOverride,
                        UnloadMaxDistanceOverride = subSceneLoadConfig.UnloadMaxDistanceOverride,
                    });
                }
            }

            if (this.waitingForLoad.Count == 0)
            {
                PauseGame.Unpause(ref state);
            }
        }

        private void WaitToLoad(ref SystemState state)
        {
            using var e = this.waitingForLoad.GetEnumerator();
            while (e.MoveNext())
            {
                if (!this.IsSceneLoad(ref state, e.Current))
                {
                    return;
                }
            }

            this.waitingForLoad.Clear();

            for (var index = this.volumes.Length - 1; index >= 0; index--)
            {
                var subScene = this.volumes[index];

                if (!state.EntityManager.HasComponent<ResolvedSectionEntity>(subScene.Entity))
                {
                    continue;
                }

                var sections = state.EntityManager.GetBuffer<ResolvedSectionEntity>(subScene.Entity).AsNativeArray();

                var bounds = this.GetBounds(ref state, sections);
                if (!bounds.Equals(MinMaxAABB.Empty))
                {
                    state.EntityManager.AddComponentData(subScene.Entity, new LoadWithBoundingVolume
                    {
                        Bounds = bounds,
                        LoadMaxDistanceOverrideSq = subScene.LoadMaxDistanceOverride * subScene.LoadMaxDistanceOverride,
                        UnloadMaxDistanceOverrideSq = subScene.UnloadMaxDistanceOverride * subScene.UnloadMaxDistanceOverride,
                    });
                }

                this.volumes.RemoveAt(index);
            }

            if (this.volumes.Length != 0)
            {
                return;
            }

            var debug = SystemAPI.GetSingleton<BLDebug>();
            debug.Debug("All required SubScenes loaded.");

            PauseGame.Unpause(ref state);

            // In builds once scenes are loaded there is no need to keep checking state
            // However in editor SubScenes can be opened and reloaded so we keep the system running to handle this
#if !UNITY_EDITOR
            state.Enabled = false;
#endif
        }

        private MinMaxAABB GetBounds(ref SystemState state, NativeArray<ResolvedSectionEntity> sections)
        {
            var bounds = MinMaxAABB.Empty;

            foreach (var section in sections)
            {
                if (!state.EntityManager.HasComponent<SceneSectionData>(section.SectionEntity))
                {
                    // If we don't have a SceneSectionData it means subscene is open and we should always load it
                    return MinMaxAABB.Empty;
                }

                var sceneSectionData = state.EntityManager.GetComponentData<SceneSectionData>(section.SectionEntity);
                bounds.Encapsulate(sceneSectionData.BoundingVolume);
            }

            return bounds;
        }

        private struct Volume
        {
            public Entity Entity;
            public float LoadMaxDistanceOverride;
            public float UnloadMaxDistanceOverride;
        }
    }
}
#endif
