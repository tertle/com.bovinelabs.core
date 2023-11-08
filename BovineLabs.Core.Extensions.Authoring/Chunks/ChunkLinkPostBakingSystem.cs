// <copyright file="ChunkLinkPostBakingSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Authoring.Chunks
{
    using BovineLabs.Core.Chunks;
    using BovineLabs.Core.Chunks.Data;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Provides post processing operations after virtual chunks have been setup.
    /// Responsible for things like adding required padding to owner if chunks don't have enough space.
    /// </summary>
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [UpdateAfter(typeof(VirtualChunkRootBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct ChunkLinkPostBakingSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var chunkOwnerQuery = SystemAPI.QueryBuilder()
                .WithAll<LinkedEntityGroup, VirtualChunkMask>()
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities).Build();

            this.AddPaddingIfRequired(ref state, chunkOwnerQuery);
            this.SetChunkLinkEntities(ref state, chunkOwnerQuery);
        }

        private void AddPaddingIfRequired(ref SystemState state, EntityQuery chunkOwnerQuery)
        {
            using var chunks = chunkOwnerQuery.ToArchetypeChunkArray(Allocator.Temp);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var entityTypeHandle = SystemAPI.GetEntityTypeHandle();
            var linkedEntityGroupHandle = SystemAPI.GetBufferTypeHandle<LinkedEntityGroup>(true);
            var uniqueArchetypes = new NativeParallelMultiHashMap<EntityArchetype, ArchetypeChunk>(0, state.WorldUpdateAllocator);

            foreach (var chunk in chunks)
            {
                uniqueArchetypes.Add(chunk.Archetype, chunk);
            }

            using var archetypes = uniqueArchetypes.GetKeyArray(Allocator.Temp);

            foreach (var archetype in archetypes)
            {
                uniqueArchetypes.TryGetFirstValue(archetype, out var chunk, out var it);

                var linkedEntityGroupAccessor = chunk.GetBufferAccessor(ref linkedEntityGroupHandle);
                var linkedEntityGroup = linkedEntityGroupAccessor[0].AsNativeArray().Reinterpret<Entity>();

                int largestChunkLinkSize = 0;
                var largestChunk = default(ArchetypeChunk);

                foreach (var entity in linkedEntityGroup)
                {
                    if (!state.EntityManager.HasComponent<ChunkChild>(entity))
                    {
                        continue;
                    }

                    var linkArchetype = state.EntityManager.GetChunk(entity);
                    var instanceSizeWithOverhead = this.InstanceSizeWithoutBaking(linkArchetype.Archetype);

                    if (instanceSizeWithOverhead > largestChunkLinkSize)
                    {
                        largestChunkLinkSize = instanceSizeWithOverhead;
                        largestChunk = linkArchetype;
                    }
                }

                if (largestChunkLinkSize == 0)
                {
                    continue;
                }

                var capacityLargestChunk = this.ChunkCapacityWithoutBaking(largestChunk.Archetype);
                var capacityChunk = this.ChunkCapacityWithoutBaking(chunk.Archetype);

                if (capacityLargestChunk >= capacityChunk)
                {
                    return;
                }

                // We need some padding, find the required extra capacity
                var chunkSize = this.InstanceSizeWithoutBaking(chunk.Archetype);
                var extraCapacity = largestChunkLinkSize - chunkSize; // TODO check if alignment works fine here
                var paddingComponent = ChunkPadding.Get(extraCapacity);

                Debug.LogWarning($"Required to add padding {extraCapacity} using {paddingComponent.ToFixedString()}");

                do
                {
                    var entities = chunk.GetEntityDataPtrRO(entityTypeHandle);
                    for (var i = 0; i < chunk.Count; i++)
                    {
                        // Looking at source you can only batch add without enableable components so for now this is safer
                        ecb.AddComponent(entities[i], paddingComponent);
                    }
                }
                while (uniqueArchetypes.TryGetNextValue(out chunk, ref it));
            }

            ecb.Playback(state.EntityManager);
        }

        private void SetChunkLinkEntities(ref SystemState state, EntityQuery chunkOwnerQuery)
        {
            // var chunkLinkedMask = SystemAPI.QueryBuilder().WithAll<ChunkLinkedEntity>().Build().GetEntityQueryMask();
            //
            // var ecb = new EntityCommandBuffer(Allocator.Temp);
            //
            // using var chunks = chunkOwnerQuery.ToArchetypeChunkArray(Allocator.Temp);
            // var entityHandle = SystemAPI.GetEntityTypeHandle();
            //
            // foreach (var chunk in chunks)
            // {
            //     var entities = chunk.GetEntityDataPtrRO(entityHandle);
            //     for (var i = 0; i < chunk.Count; i++)
            //     {
            //         ecb.SetComponentForLinkedEntityGroup(entities[i], chunkLinkedMask, new ChunkLinkedEntity { Value = entities[i] });
            //     }
            // }
            //
            // ecb.Playback(state.EntityManager);
        }

        // EntityComponentStore.CreateArchetype
        private int InstanceSizeWithoutBaking(EntityArchetype entityArchetype)
        {
            var nonZeroSizedTypeCount = entityArchetype.ArchetypeChunkNonZeroSizedTypesCount();
            var size = 0;

            for (var index = 0; index < nonZeroSizedTypeCount; index++)
            {
                var type = entityArchetype.ArchetypeChunkGetTypeIndex(index);
                ref readonly var cType = ref TypeManager.GetTypeInfo(type);

                if (cType.TemporaryBakingType || cType.BakingOnlyType)
                {
                    continue;
                }

                size += cType.SizeInChunk;
            }

            return size;
        }

        // EntityComponentStore.CreateArchetype
        private int ChunkCapacityWithoutBaking(EntityArchetype entityArchetype)
        {
            var bufferSize = ChunkInternals.GetChunkBufferSize();
            var maxCapacity = TypeManager.MaximumChunkCapacity;
            var count = entityArchetype.ArchetypeChunkNonZeroSizedTypesCount();

            int totalSize = 0;
            for (int index = 0; index < count; ++index)
            {
                var type = entityArchetype.ArchetypeChunkGetTypeIndex(index);
                ref readonly var cType = ref TypeManager.GetTypeInfo(type);

                if (cType.TemporaryBakingType || cType.BakingOnlyType)
                {
                    continue;
                }

                totalSize += cType.SizeInChunk;
            }

            if (totalSize == 0)
            {
                return maxCapacity;
            }

            int capacity = bufferSize / totalSize;
            while (CalculateSpaceRequirement(entityArchetype, count, capacity) > bufferSize)
            {
                --capacity;
            }

            return math.min(maxCapacity, capacity);
        }

        // EntityComponentStore.CalculateSpaceRequirement
        private static int CalculateSpaceRequirement(EntityArchetype entityArchetype, int componentCount, int entityCount)
        {
            var size = 0;
            for (int index = 0; index < componentCount; ++index)
            {
                // var componentSize = entityArchetype.ArchetypeChunkGetSize(i);
                var type = entityArchetype.ArchetypeChunkGetTypeIndex(index);
                ref readonly var cType = ref TypeManager.GetTypeInfo(type);

                if (cType.TemporaryBakingType || cType.BakingOnlyType)
                {
                    continue;
                }

                size += CollectionHelper.Align(cType.SizeInChunk * entityCount, CollectionHelper.CacheLineSize);
            }

            return size;
        }
    }
}
#endif
