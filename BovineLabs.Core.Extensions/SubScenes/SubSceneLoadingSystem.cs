// <copyright file="SubSceneLoadingSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.SubScenes
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Pause;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;

    /// <summary> System that loads our SubScenes depending on the world and the SubScene load mode. </summary>
    [UpdateInGroup(typeof(AfterSceneSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(Worlds.SimulationThinService)]
    [CreateAfter(typeof(BLDebugSystem))]
    public unsafe partial struct SubSceneLoadingSystem : ISystem
    {
        private NativeList<Entity> waitingForLoad;

#if UNITY_EDITOR
        private NativeHashSet<Entity> requiredScenes;
#endif

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            this.waitingForLoad = new NativeList<Entity>(4, Allocator.Persistent);
#if UNITY_EDITOR
            this.requiredScenes = new NativeHashSet<Entity>(4, Allocator.Persistent);
#endif
        }

        /// <inheritdoc />
        public void OnDestroy(ref SystemState state)
        {
            this.waitingForLoad.Dispose();
#if UNITY_EDITOR
            this.requiredScenes.Dispose();
#endif
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
#if UNITY_EDITOR
            this.HandleMissingSubScenes(ref state);
#endif

            this.UnloadSubScenes(ref state);
            this.LoadSubSceneEntities(ref state);
            this.LoadSubScenes(ref state);

            if (this.waitingForLoad.Length > 0)
            {
                this.WaitToLoad(ref state);
            }
        }

#if UNITY_EDITOR
        private void HandleMissingSubScenes(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<SubSceneLoadData, SubSceneBuffer, SubSceneEntity>().Build();

            var entityTypeHandle = SystemAPI.GetEntityTypeHandle();
            var subSceneEntityHandle = SystemAPI.GetBufferTypeHandle<SubSceneEntity>();

            var list = new NativeList<(Entity Entity, int Index)>(state.WorldUpdateAllocator);

            var queryIterator = new QueryEntityEnumerator(query);
            while (queryIterator.MoveNextChunk(out var chunk, out var chunkIterator))
            {
                var entities = chunk.GetEntityDataPtrRO(entityTypeHandle);
                var subSceneEntityAccessor = chunk.GetBufferAccessorRO(ref subSceneEntityHandle);

                while (chunkIterator.NextEntityIndex(out var entityIndexInChunk))
                {
                    var subSceneEntities = subSceneEntityAccessor[entityIndexInChunk];
                    for (var index = 0; index < subSceneEntities.Length; index++)
                    {
                        if (state.EntityManager.Exists(subSceneEntities[index].Entity))
                        {
                            continue;
                        }

                        list.Add((entities[entityIndexInChunk], index));
                    }
                }
            }

            var loadRequired = false;

            foreach (var (entity, index) in list)
            {
                var buffer = state.EntityManager.GetBuffer<SubSceneEntity>(entity);
                ref var element = ref buffer.ElementAt(index);

                var oldEntity = element.Entity;

                var autoLoad = state.EntityManager.IsComponentEnabled<LoadSubScene>(entity);
                var newEntity = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, element.Scene, new SceneSystem.LoadParameters { AutoLoad = autoLoad });
                element.Entity = newEntity;

                if (this.requiredScenes.Remove(oldEntity))
                {
                    var oldIndex = this.waitingForLoad.IndexOf(oldEntity);
                    if (oldIndex != -1)
                    {
                        this.waitingForLoad.RemoveAtSwapBack(oldIndex);
                    }

                    this.waitingForLoad.Add(element.Entity);

                    loadRequired = true;
                }
            }

            foreach (var required in this.requiredScenes)
            {
                if (!SceneSystem.IsSceneLoaded(state.WorldUnmanaged, required))
                {
                    if (this.waitingForLoad.IndexOf(required) == -1)
                    {
                        this.waitingForLoad.Add(required);
                        loadRequired = true;
                    }
                }
            }

            if (loadRequired)
            {
                PauseGame.Pause(ref state, true);
            }
        }
#endif

        private void LoadSubSceneEntities(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<SubSceneLoadData, SubSceneBuffer>().WithDisabledRW<SubSceneEntity>().Build();
            if (query.IsEmpty)
            {
                return;
            }

            var toLoad = new NativeList<(Entity Entity, NativeList<SubSceneBuffer> SceneEntities)>(4, state.WorldUpdateAllocator);

            var flags = state.WorldUnmanaged.Flags;

            var entityHandle = SystemAPI.GetEntityTypeHandle();
            var subSceneLoadDataHandle = SystemAPI.GetComponentTypeHandle<SubSceneLoadData>(true);
            var subSceneBufferHandle = SystemAPI.GetBufferTypeHandle<SubSceneBuffer>(true);
            var subSceneEntityHandle = SystemAPI.GetBufferTypeHandle<SubSceneEntity>();

            var queryIterator = new QueryEntityEnumerator(query);
            while (queryIterator.MoveNextChunk(out var chunk, out var chunkIterator))
            {
                var entities = chunk.GetEntityDataPtrRO(entityHandle);
                var subSceneLoadDatas = (SubSceneLoadData*)chunk.GetRequiredComponentDataPtrRO(ref subSceneLoadDataHandle);
                var subSceneBufferAccessor = chunk.GetBufferAccessor(ref subSceneBufferHandle);

                while (chunkIterator.NextEntityIndex(out var entityIndexInChunk))
                {
                    chunk.SetComponentEnabled(ref subSceneEntityHandle, entityIndexInChunk, true);

                    var data = subSceneLoadDatas[entityIndexInChunk];
                    var subSceneBuffer = subSceneBufferAccessor[entityIndexInChunk];
                    var doesNotMatchWorld = (data.TargetWorld & flags) == 0;

                    if (doesNotMatchWorld)
                    {
                        continue;
                    }

                    var list = new NativeList<SubSceneBuffer>(16, state.WorldUpdateAllocator);
                    toLoad.Add((entities[entityIndexInChunk], list));
                    list.AddRange(subSceneBuffer.AsNativeArray());
                }
            }

            // This is all just to work around structural changes and DynamicBuffers
            var loadingParams = default(SceneSystem.LoadParameters);
            loadingParams.AutoLoad = false;

            foreach (var (entity, sceneEntities) in toLoad)
            {
                foreach (var scene in sceneEntities)
                {
                    var sceneGuid = scene.Scene.SceneGUID();
                    var sceneEntity = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, sceneGuid, loadingParams);

                    state
                    .EntityManager
                    .GetBuffer<SubSceneEntity>(entity)
                    .Add(new SubSceneEntity
                    {
                        Entity = sceneEntity,
                        Scene = sceneGuid,
                    });
#if UNITY_EDITOR
                    state.EntityManager.SetName(sceneEntity, scene.Name);
#endif
                }
            }
        }

        private void LoadSubScenes(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<SubSceneLoadData, SubSceneEntity, LoadSubScene>().WithDisabledRW<SubSceneLoaded>().Build();
            if (query.IsEmpty)
            {
                return;
            }

#if UNITY_EDITOR
            var debug = SystemAPI.GetSingleton<BLDebug>();
#endif

            var toLoad = new NativeList<Entity>(64, state.WorldUpdateAllocator);
            var subSceneLoadDataHandle = SystemAPI.GetComponentTypeHandle<SubSceneLoadData>(true);
            var subSceneEntityHandle = SystemAPI.GetBufferTypeHandle<SubSceneEntity>(true);
            var subSceneLoadedHandle = SystemAPI.GetComponentTypeHandle<SubSceneLoaded>();

            var queryIterator = new QueryEntityEnumerator(query);
            while (queryIterator.MoveNextChunk(out var chunk, out var e))
            {
                var subSceneLoadDatas = (SubSceneLoadData*)chunk.GetRequiredComponentDataPtrRO(ref subSceneLoadDataHandle);
                var subSceneEntityAccessor = chunk.GetBufferAccessor(ref subSceneEntityHandle);

                while (e.NextEntityIndex(out var entityIndexInChunk))
                {
                    chunk.SetComponentEnabled(ref subSceneLoadedHandle, entityIndexInChunk, true);

                    var data = subSceneLoadDatas[entityIndexInChunk];
                    var subSceneEntities = subSceneEntityAccessor[entityIndexInChunk];

                    foreach (var ss in subSceneEntities)
                    {
                        toLoad.Add(ss.Entity);

#if UNITY_EDITOR
                        state.EntityManager.GetName(ss.Entity, out var name);
                        debug.Debug($"Loading SubScene | {name}");
#endif
                    }

                    if (data.WaitForLoad)
                    {
                        foreach (var ss in subSceneEntities)
                        {
                            this.waitingForLoad.Add(ss.Entity);
#if UNITY_EDITOR
                            this.requiredScenes.Add(ss.Entity);
#endif
                        }
                    }
                }
            }

            var loadingParams = new SceneSystem.LoadParameters { AutoLoad = true };
            foreach (var entity in toLoad)
            {
                SceneSystem.LoadSceneAsync(state.WorldUnmanaged, entity, loadingParams);
            }

            if (this.waitingForLoad.Length > 0)
            {
                PauseGame.Pause(ref state, true);
            }
        }

        private void UnloadSubScenes(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<SubSceneLoadData, SubSceneEntity>().WithAllRW<SubSceneLoaded>().WithDisabled<LoadSubScene>().Build();
            if (query.IsEmpty)
            {
                return;
            }

#if UNITY_EDITOR
            var debug = SystemAPI.GetSingleton<BLDebug>();
#endif

            var toUnload = new NativeList<Entity>(64, state.WorldUpdateAllocator);

            var subSceneLoadDataHandle = SystemAPI.GetComponentTypeHandle<SubSceneLoadData>(true);
            var subSceneEntityHandle = SystemAPI.GetBufferTypeHandle<SubSceneEntity>();
            var subSceneLoadedHandle = SystemAPI.GetComponentTypeHandle<SubSceneLoaded>();

            var queryIterator = new QueryEntityEnumerator(query);
            while (queryIterator.MoveNextChunk(out var chunk, out var e))
            {
                var subSceneLoadDatas = (SubSceneLoadData*)chunk.GetRequiredComponentDataPtrRO(ref subSceneLoadDataHandle);
                var subSceneEntityAccessor = chunk.GetBufferAccessor(ref subSceneEntityHandle);

                while (e.NextEntityIndex(out var entityIndexInChunk))
                {
                    chunk.SetComponentEnabled(ref subSceneLoadedHandle, entityIndexInChunk, true);

                    var data = subSceneLoadDatas[entityIndexInChunk];
                    var subSceneEntities = subSceneEntityAccessor[entityIndexInChunk];

                    foreach (var ss in subSceneEntities)
                    {
                        toUnload.Add(ss.Entity);

#if UNITY_EDITOR
                        state.EntityManager.GetName(ss.Entity, out var name);
                        debug.Debug($"Unloading SubScene | {name}");
#endif
                    }

                    // This should be uncommon but if we were waiting to load these subscenes we should remove them from the queue if they never loaded
                    if (data.WaitForLoad)
                    {
                        foreach (var ss in subSceneEntities)
                        {
                            var index = this.waitingForLoad.IndexOf(ss.Entity);
                            if (index != -1)
                            {
                                this.waitingForLoad.RemoveAtSwapBack(index);
                            }

#if UNITY_EDITOR
                            this.requiredScenes.Remove(ss.Entity);
#endif
                        }
                    }

                    subSceneEntities.Clear();
                }
            }

            foreach (var e in toUnload)
            {
                SceneSystem.UnloadScene(state.WorldUnmanaged, e);
            }
        }

        private void WaitToLoad(ref SystemState state)
        {
            for (var i = this.waitingForLoad.Length - 1; i >= 0; i--)
            {
                if (SceneSystem.IsSceneLoaded(state.WorldUnmanaged, this.waitingForLoad[i]))
                {
                    this.waitingForLoad.RemoveAtSwapBack(i);
                }
            }

            if (this.waitingForLoad.Length > 0)
            {
                return;
            }

            var debug = SystemAPI.GetSingleton<BLDebug>();
            debug.Debug("All required SubScenes loaded.");

            PauseGame.Unpause(ref state);
        }
    }
}
