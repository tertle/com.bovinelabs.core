// <copyright file="SubSceneLoadingSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Pause;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Scenes;
    using Hash128 = Unity.Entities.Hash128;

    /// <summary> System that loads our SubScenes depending on the world and the SubScene load mode. </summary>
    [UpdateInGroup(typeof(AfterSceneSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation | Worlds.Service)]
    [CreateAfter(typeof(BLDebugSystem))]
    public partial struct SubSceneLoadingSystem : ISystem
    {
        private NativeList<Volume> volumes;
        private NativeList<Entity> requiredScenes;
        private NativeHashSet<Entity> waitingForLoad;
        private NativeHashSet<Hash128> scenes;

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            this.volumes = new NativeList<Volume>(16, Allocator.Persistent);
            this.requiredScenes = new NativeList<Entity>(4, Allocator.Persistent);
            this.waitingForLoad = new NativeHashSet<Entity>(16, Allocator.Persistent);
            this.scenes = new NativeHashSet<Hash128>(16, Allocator.Persistent);
        }

        /// <inheritdoc />
        public void OnDestroy(ref SystemState state)
        {
            this.volumes.Dispose();
            this.requiredScenes.Dispose();
            this.waitingForLoad.Dispose();
            this.scenes.Dispose();
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

            this.LoadSubSceneEntities(ref state);

            if (this.waitingForLoad.Count != 0)
            {
                this.WaitToLoad(ref state);
            }
        }

        private bool IsSceneLoad(ref SystemState state, Entity entity)
        {
            return SceneSystem.IsSceneLoaded(state.WorldUnmanaged, entity);
        }

        private void LoadSubSceneEntities(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<SubSceneLoad>().Build();

            if (query.IsEmptyIgnoreFilter)
            {
                return;
            }

            var scenesToLoad = new NativeList<SceneLoadData>(state.WorldUpdateAllocator);
            var ecb = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            var entityHandle = SystemAPI.GetEntityTypeHandle();
            var subSceneLoadWeakReferenceHandle = SystemAPI.GetBufferTypeHandle<SubSceneLoad>(true);

            var chunks = query.ToArchetypeChunkArray(state.WorldUpdateAllocator);
            foreach (var chunk in chunks)
            {
                var entities = chunk.GetNativeArray(entityHandle);
                var subSceneLoadWeakReferenceAccessor = chunk.GetBufferAccessor(ref subSceneLoadWeakReferenceHandle);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var subSceneLoadWeakReferences = subSceneLoadWeakReferenceAccessor[i];

                    foreach (var scene in subSceneLoadWeakReferences.AsNativeArray())
                    {
                        scenesToLoad.Add(new SceneLoadData { Data = scene });
                    }

                    ecb.AddComponent<Disabled>(entities[i]);
                }
            }

            this.LoadScenes(ref state, scenesToLoad.AsArray());
        }

        private void LoadScenes(ref SystemState state, NativeArray<SceneLoadData> scenesToLoad)
        {
            var debug = SystemAPI.GetSingleton<BLDebug>();
            var flags = state.WorldUnmanaged.Flags;

            for (var index = 0; index < scenesToLoad.Length; index++)
            {
                ref var subScene = ref scenesToLoad.ElementAt(index);
                var sceneGuid = subScene.Data.Scene.SceneGUID();

                if (!this.scenes.Add(sceneGuid))
                {
                    // Already been loaded
                    continue;
                }

                var loadingMode = subScene.Data.LoadingMode;
                var isRequired = subScene.Data.IsRequired;
                var targetWorld = subScene.Data.TargetWorld;

                if ((targetWorld & flags) == 0)
                {
                    // SubScene OnEnable can load sub scenes before we have a chance to stop it so let's just revert this here
                    var sceneEntity = this.GetSceneEntity(ref state, sceneGuid);
                    if (sceneEntity != Entity.Null)
                    {
                        // The guid overload variation is not burst compatible so we get the entity ourselves
                        SceneSystem.UnloadScene(state.WorldUnmanaged, sceneEntity, SceneSystem.UnloadParameters.DestroyMetaEntities);
                    }

                    continue;
                }

                var isServer = state.WorldUnmanaged.IsServerWorld();

                var requiredForLoading = loadingMode == SubSceneLoadMode.AutoLoad && isRequired;
                requiredForLoading |= isServer && loadingMode == SubSceneLoadMode.BoundingVolume;

                var loadingParams = default(SceneSystem.LoadParameters);
                loadingParams.AutoLoad = (isServer && loadingMode != SubSceneLoadMode.OnDemand) || loadingMode == SubSceneLoadMode.AutoLoad;
                subScene.Entity = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, sceneGuid, loadingParams);
                state.EntityManager.AddComponentData(subScene.Entity, new RequestSceneLoaded { LoadFlags = loadingParams.Flags });

#if UNITY_EDITOR
                state.EntityManager.SetName(subScene.Entity, subScene.Data.Name);
                debug.Debug($"Loading SubScene {subScene.Data.Name}\nrequiredForLoading: {requiredForLoading}, loadingMode: {loadingMode}");
#endif
                if (requiredForLoading)
                {
                    this.waitingForLoad.Add(subScene.Entity);
                    this.requiredScenes.Add(subScene.Entity);

                    state.EntityManager.AddComponent<RequiredSubScene>(subScene.Entity);
                }

                if (!isServer && loadingMode == SubSceneLoadMode.BoundingVolume)
                {
                    this.volumes.Add(new Volume
                    {
                        Entity = subScene.Entity,
                        LoadMaxDistance = subScene.Data.LoadMaxDistance,
                        UnloadMaxDistance = subScene.Data.UnloadMaxDistance,
                    });
                }
            }

            if (this.waitingForLoad.Count != 0)
            {
                PauseGame.Pause(ref state, true);
            }
        }

        private Entity GetSceneEntity(ref SystemState state, Hash128 sceneGUID)
        {
            foreach (var (r, e) in SystemAPI.Query<SceneReference>().WithEntityAccess())
            {
                if (r.SceneGUID == sceneGUID)
                {
                    return e;
                }
            }

            return Entity.Null;
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
                        LoadMaxDistanceSq = subScene.LoadMaxDistance * subScene.LoadMaxDistance,
                        UnloadMaxDistanceSq = subScene.UnloadMaxDistance * subScene.UnloadMaxDistance,
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
            public float LoadMaxDistance;
            public float UnloadMaxDistance;
        }

        private struct SceneLoadData
        {
            public Entity Entity;
            public SubSceneLoad Data;
        }
    }
}
#endif
