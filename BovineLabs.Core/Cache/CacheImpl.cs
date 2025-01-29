// <copyright file="CacheImpl.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Cache
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;

    public unsafe struct CacheImpl<T, TC, TCC>
        where TCC : unmanaged, ICacheComponent<TC>
        where TC : unmanaged, IEntityCache
    {
        private EntityQuery updateQuery;
        private EntityQuery newQuery;
        private EntityQuery cleanupQuery;

        private EntityTypeHandle entityHandle;
        private ComponentTypeHandle<TCC> cacheHandle;
        private ComponentTypeHandle<CacheCleanup> cacheCleanupHandle;

        private NativeQueue<New> newQueue;
        private NativeHashSet<Ptr<UnsafeList<TC>>> allocated;

        private JobHandle lastFrameDependency;

        public void OnCreate(ref SystemState state)
        {
            this.newQueue = new NativeQueue<New>(Allocator.Persistent);
            this.allocated = new NativeHashSet<Ptr<UnsafeList<TC>>>(256, Allocator.Persistent);

            using var builder = new EntityQueryBuilder(Allocator.Temp);

            this.updateQuery = builder.WithAllChunkComponentRW<TCC>().Build(ref state);
            this.updateQuery.AddOrderVersionFilter();

            // By ensuring prefabs are given the chunk component we avoid entities instantiated ending up in individual chunks
            builder.Reset();
            this.newQuery = builder.WithAll<T>().WithNoneChunkComponent<TCC>().WithOptions(EntityQueryOptions.IncludePrefab).Build(ref state);

            builder.Reset();
            this.cleanupQuery = builder.WithAll<CacheCleanup>().WithNone<TCC>().Build(ref state);

            this.entityHandle = state.GetEntityTypeHandle();
            this.cacheHandle = state.GetComponentTypeHandle<TCC>();
            this.cacheCleanupHandle = state.GetComponentTypeHandle<CacheCleanup>();
        }

        public void OnDestroy()
        {
            while (this.newQueue.TryDequeue(out var list))
            {
                UnsafeList<TC>.Destroy(list.Cache.Value);
            }

            this.newQueue.Dispose();

            foreach (var p in this.allocated)
            {
                UnsafeList<TC>.Destroy(p);
            }

            this.allocated.Dispose();
        }

        public void OnUpdate(ref SystemState state, UpdateCacheJob job = default)
        {
            this.lastFrameDependency.Complete(); // last frame's job

            this.AddCleanupState(ref state);
            this.CleanupOldCache(ref state);

            state.EntityManager.AddComponent(this.newQuery, ComponentType.ChunkComponent<TCC>());

            this.UpdateCache(ref state, ref job);

            this.lastFrameDependency = state.Dependency;
        }

        private void AddCleanupState(ref SystemState state)
        {
            while (this.newQueue.TryDequeue(out var list))
            {
                if (!state.EntityManager.Exists(list.MetaEntity))
                {
                    // Meta Entity no longer exists
                    UnsafeList<TC>.Destroy(list.Cache.Value);
                    continue;
                }

                this.allocated.Add(list.Cache);
                state.EntityManager.AddComponentData(list.MetaEntity, new CacheCleanup { Ptr = list.Cache });
            }
        }

        private void CleanupOldCache(ref SystemState state)
        {
            if (this.cleanupQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            this.cacheCleanupHandle.Update(ref state);

            foreach (var chunk in this.cleanupQuery.ToArchetypeChunkArray(state.WorldUpdateAllocator))
            {
                var cleanup = (CacheCleanup*)chunk.GetRequiredComponentDataPtrRO(ref this.cacheCleanupHandle);
                for (var i = 0; i < chunk.Count; i++)
                {
                    if (cleanup[i].Ptr == null)
                    {
                        Debug.Log("Null cache how?");
                        continue;
                    }

                    var cache = (Ptr<UnsafeList<TC>>)cleanup[i].Ptr;

                    if (!this.allocated.Remove(cache))
                    {
                        Debug.LogError("Somehow cache was not stored");
                    }

                    UnsafeList<TC>.Destroy(cache);
                }
            }

            state.EntityManager.RemoveComponent<CacheCleanup>(this.cleanupQuery);
        }

        private void UpdateCache(ref SystemState state, ref UpdateCacheJob job)
        {
            this.cacheHandle.Update(ref state);
            this.entityHandle.Update(ref state);

            job.EntityHandle = this.entityHandle;
            job.CacheHandle = this.cacheHandle;
            job.Lists = this.newQueue.AsParallelWriter();
            state.Dependency = job.ScheduleParallel(this.updateQuery, state.Dependency);
        }

        public struct New
        {
            public Entity MetaEntity;
            public Ptr<UnsafeList<TC>> Cache;
        }

        [BurstCompile]
        public struct UpdateCacheJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle EntityHandle;

            public ComponentTypeHandle<TCC> CacheHandle;

            public NativeQueue<New>.ParallelWriter Lists;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                ref var cacheComponent = ref chunk.GetChunkComponentDataRW(ref this.CacheHandle);

                if (cacheComponent.Cache == null)
                {
                    cacheComponent.Cache = UnsafeList<TC>.Create(chunk.Capacity, Allocator.Persistent);

                    this.Lists.Enqueue(new New
                    {
                        MetaEntity = chunk.m_Chunk.MetaChunkEntity,
                        Cache = cacheComponent.Cache,
                    });
                }

                var cache = *cacheComponent.Cache;

                var entities = chunk.GetEntityDataPtrRO(this.EntityHandle);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    // At end of cache but still have entities to go, just populate
                    if (entityIndex == cache.Length)
                    {
                        for (; entityIndex < chunk.Count; entityIndex++)
                        {
                            cache.Add(new TC { Entity = entities[entityIndex] });
                        }

                        break;
                    }

                    // Already in place
                    if (cache.Ptr[entityIndex].Entity.Equals(entities[entityIndex]))
                    {
                        continue;
                    }

                    // Order is messed up, try find existing data
                    int cacheIndex;
                    for (cacheIndex = entityIndex + 1; cacheIndex < cache.Length; cacheIndex++)
                    {
                        if (cache.Ptr[cacheIndex].Entity.Equals(entities[entityIndex]))
                        {
                            break;
                        }
                    }

                    // Wasn't found, just add it to end and we'll swap it in
                    if (cacheIndex == cache.Length)
                    {
                        cache.Add(new TC { Entity = entities[entityIndex] });
                    }

                    // Swap
                    (cache[entityIndex], cache[cacheIndex]) = (cache[cacheIndex], cache[entityIndex]);
                }

                // Shrink cache if we need
                cache.Resize(chunk.Count);
                cache.TrimExcess();
                *cacheComponent.Cache = cache;
            }
        }
    }

    // Outside the struct to avoid generic issues
    internal unsafe struct CacheCleanup : ICleanupComponentData
    {
        [NativeDisableUnsafePtrRestriction]
        public void* Ptr;
    }
}
