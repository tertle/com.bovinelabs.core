// <copyright file="EntityBlobBakingSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Blobs
{
    using BovineLabs.Core.Blobs;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using Unity;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct EntityBlobBakingSystem : ISystem
    {
        private NativeHashMap<int, int> tempBlobMap;

        private BlobAssetStore worldBlobStore;
        private BlobAssetStore localBlobAssetStore;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.tempBlobMap = new NativeHashMap<int, int>(0, Allocator.Persistent);
            this.worldBlobStore = state.World.GetExistingSystemManaged<BakingSystem>().BlobAssetStore;

            this.localBlobAssetStore = new BlobAssetStore(128);
        }

        /// <inheritdoc/>
        public void OnDestroy(ref SystemState state)
        {
            this.tempBlobMap.Dispose();
            this.localBlobAssetStore.Dispose();
        }

#if BL_ENTITIES_CUSTOM
        [BurstCompile]
#endif
        public void OnUpdate(ref SystemState state)
        {
            // Remove any existing blob data for live baking
            state.EntityManager.RemoveComponent<EntityBlob>(SystemAPI.QueryBuilder().WithAll<EntityBlob>().Build());

            var map = this.GroupBlobs(ref state);

            (NativeArray<Entity> Keys, int Length) keys = map.GetUniqueKeyArray(state.WorldUpdateAllocator);

            for (var i = 0; i < keys.Length; i++)
            {
                var blobBuilder = new BlobBuilder(state.WorldUpdateAllocator);
                var entity = keys.Keys[i];

                var blobMap = this.ConstructPerfectHashMap(ref state, ref blobBuilder, map, entity);
                this.PopulateMap(ref blobBuilder, ref blobMap, map, entity);

                var bar = blobBuilder.CreateBlobAssetReference<BlobPerfectHashMap<int, int>>(Allocator.Persistent);
                this.worldBlobStore.TryAdd(ref bar);
                state.EntityManager.AddComponentData(entity, new EntityBlob { Value = bar });
            }
        }

        private NativeParallelMultiHashMap<Entity, EntityBlobBakedData> GroupBlobs(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<EntityBlobBakedData>().Build();
            var map = new NativeParallelMultiHashMap<Entity, EntityBlobBakedData>(query.CalculateEntityCount(), state.WorldUpdateAllocator);

            state.Dependency = new GroupJob
            {
                EntityBlobBakedDataHandle = SystemAPI.GetComponentTypeHandle<EntityBlobBakedData>(true),
                GroupMap = map.AsParallelWriter(),
            }.ScheduleParallel(query, state.Dependency);

            state.Dependency.Complete();
            return map;
        }

        private BlobBuilderPerfectHashMap<int, int> ConstructPerfectHashMap(
            ref SystemState state, ref BlobBuilder blobBuilder, NativeParallelMultiHashMap<Entity, EntityBlobBakedData> map, Entity entity)
        {
            this.tempBlobMap.Clear();

            ref var root = ref blobBuilder.ConstructRoot<BlobPerfectHashMap<int, int>>();

            // Construct the perfect hash map first
            map.TryGetFirstValue(entity, out var bakedData, out var it);
            do
            {
                if (!this.tempBlobMap.TryAdd(bakedData.Key, 0))
                {
                    BLGlobalLogger.LogError512($"Duplicate blob keys added to {entity}. A blob data will be ignored");
                }
            }
            while (map.TryGetNextValue(out bakedData, ref it));

            return blobBuilder.ConstructPerfectHashMap(ref root, this.tempBlobMap);
        }

        private void PopulateMap(ref BlobBuilder blobBuilder, ref BlobBuilderPerfectHashMap<int, int> blobMap, NativeParallelMultiHashMap<Entity, EntityBlobBakedData> map, Entity entity)
        {
            map.TryGetFirstValue(entity, out var bakedData, out var it);
            do
            {
                ref var header = ref UnsafeUtility.As<int, BlobPtr<BlobAssetHeader>>(ref blobMap[bakedData.Key]);

                if (header.IsValid)
                {
                    // Must have hit a duplicate case we've already setup, we already threw an error before so just continue
                    continue;
                }

                this.localBlobAssetStore.TryAdd(ref bakedData.Blob);

                var size = bakedData.Blob.m_data.Header->Length + sizeof(BlobAssetHeader);
                var ptr = blobBuilder.Allocate(ref header, size);
                UnsafeUtility.MemCpy(ptr, bakedData.Blob.m_data.Header, size);
            }
            while (map.TryGetNextValue(out bakedData, ref it));
        }

        [BurstCompile]
        private struct GroupJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<EntityBlobBakedData> EntityBlobBakedDataHandle;

            public NativeParallelMultiHashMap<Entity, EntityBlobBakedData>.ParallelWriter GroupMap;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entityBlobBakedDatas = chunk.GetNativeArray(ref this.EntityBlobBakedDataHandle);

                var keys = entityBlobBakedDatas.Slice().SliceWithStride<Entity>();
                this.GroupMap.AddBatchUnsafe(keys, entityBlobBakedDatas);
            }
        }
    }
}
