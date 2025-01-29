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
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Remove any existing blob data for live baking
            state.EntityManager.RemoveComponent<EntityBlob>(SystemAPI.QueryBuilder().WithAll<EntityBlob>().Build());

            var query = SystemAPI.QueryBuilder().WithAll<EntityBlobBakedData>().Build();
            var map = new NativeParallelMultiHashMap<Entity, EntityBlobBakedData>(query.CalculateEntityCount(), state.WorldUpdateAllocator);

            state.Dependency = new GroupJob
            {
                EntityBlobBakedDataHandle = SystemAPI.GetComponentTypeHandle<EntityBlobBakedData>(true),
                GroupMap = map.AsParallelWriter(),
            }.ScheduleParallel(query, state.Dependency);

            state.Dependency.Complete();
            (NativeArray<Entity> Keys, int Length) keys = map.GetUniqueKeyArray(state.WorldUpdateAllocator);

            var tempBlobMap = new NativeHashMap<int, int>(8, state.WorldUpdateAllocator);

            for (var i = 0; i < keys.Length; i++)
            {
                var blobBuilder = new BlobBuilder(state.WorldUpdateAllocator);
                tempBlobMap.Clear();

                var entity = keys.Keys[i];

                ref var root = ref blobBuilder.ConstructRoot<BlobPerfectHashMap<int, int>>();

                // Construct the perfect hash map first
                map.TryGetFirstValue(entity, out var bakedData, out var it);
                do
                {
                    if (!tempBlobMap.TryAdd(bakedData.Key, default))
                    {
                        Debug.LogError($"Duplicate blob keys added to {entity}. A blob data will be ignored");
                    }
                }
                while (map.TryGetNextValue(out bakedData, ref it));

                var blobMap = blobBuilder.ConstructPerfectHashMap(ref root, tempBlobMap);

                map.TryGetFirstValue(entity, out bakedData, out it);
                do
                {
                    ref var header = ref UnsafeUtility.As<int, BlobPtr<BlobAssetHeader>>(ref blobMap[bakedData.Key]);

                    if (header.IsValid)
                    {
                        // Must have hit a duplicate case we've already setup, we already threw an error before so just continue
                        continue;
                    }

                    var size = bakedData.Blob.m_data.Header->Length + sizeof(BlobAssetHeader);
                    var ptr = blobBuilder.Allocate(ref header, size);
                    UnsafeUtility.MemCpy(ptr, bakedData.Blob.m_data.Header, size);
                }
                while (map.TryGetNextValue(out bakedData, ref it));

                var bar = blobBuilder.CreateBlobAssetReference<BlobPerfectHashMap<int, int>>(Allocator.Persistent);
                state.EntityManager.AddComponentData(entity, new EntityBlob { Value = bar });
            }
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
