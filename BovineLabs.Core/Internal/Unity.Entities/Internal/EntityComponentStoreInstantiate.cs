namespace Unity.Entities
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// Instantiation with one shared component value replaced. Clones are allocated straight into the chunks of that value instead of the
    /// source's, so no clone moves chunk afterwards. <see cref="EntityComponentStore"/> lives in UnityEngine.EntitiesModule, so this cannot
    /// be a partial of it; it mirrors <c>InstantiateEntities(Entity, Entity*, int)</c> and the private <c>InstantiateEntitiesGroup</c>,
    /// <c>InstantiateEntitiesOne</c> and <c>BuildResidueSharedComponentIndices</c> in EntityComponentStoreCreateDestroyEntities.cs through
    /// its internal API, with <c>outputRootEntityOnly</c> and <c>removePrefab</c> fixed to true. Keep them in step when Entities changes.
    /// The private <c>FireOnAddedCallbacks</c> cannot be reached, so component lifecycle debug callbacks registered with
    /// <see cref="TypeManager"/> do not fire for these clones.
    /// </summary>
    [BurstCompile]
    internal static unsafe class EntityComponentStoreInstantiate
    {
        /// <summary>
        /// Instantiates <paramref name="srcEntity"/> and its <see cref="LinkedEntityGroup"/>, giving each clone whose archetype has
        /// <paramref name="typeIndex"/> the shared value <paramref name="sharedComponentIndex"/>. The caller holds a reference to that value
        /// for the call; each new chunk takes its own. Must run inside a structural change.
        /// </summary>
        [BurstCompile]
        public static void InstantiateWithSharedComponent(EntityComponentStore* store, Entity* srcEntity, Entity* outputEntities, int instanceCount,
            TypeIndex typeIndex, int sharedComponentIndex)
        {
            store->AssertEntityExists(*srcEntity);

            var linkedGroupType = TypeManager.GetTypeIndex<LinkedEntityGroup>();
            if (store->HasComponent(*srcEntity, linkedGroupType, out _))
            {
                var header = (BufferHeader*)store->GetComponentDataWithTypeRO(*srcEntity, linkedGroupType);
                var entityPtr = (Entity*)BufferHeader.GetElementPointer(header);
                InstantiateGroup(store, entityPtr, header->Length, outputEntities, instanceCount, typeIndex, sharedComponentIndex);
            }
            else
            {
                InstantiateGroup(store, srcEntity, 1, outputEntities, instanceCount, typeIndex, sharedComponentIndex);
            }
        }

        private static void InstantiateGroup(EntityComponentStore* store, Entity* srcEntities, int srcEntityCount, Entity* outputRootEntities,
            int instanceCount, TypeIndex typeIndex, int sharedComponentIndex)
        {
            var totalCount = srcEntityCount * instanceCount;

            var tempAllocSize = (sizeof(Entity) * totalCount) + (sizeof(RemapChunk) * totalCount) + (sizeof(Entity) * instanceCount);
            byte* allocation;
            const int kMaxStackAllocSize = 16 * 1024;

            if (tempAllocSize > kMaxStackAllocSize)
            {
                allocation = (byte*)UnsafeUtility.Malloc(tempAllocSize, 16, Allocator.Temp);
            }
            else
            {
                var temp = stackalloc byte[tempAllocSize];
                allocation = temp;
            }

            var entityRemap = (Entity*)allocation;
            var remapChunks = (RemapChunk*)(entityRemap + totalCount);
            var outputEntities = (Entity*)(remapChunks + totalCount);

            var remapChunksCount = 0;

            for (var i = 0; i != srcEntityCount; i++)
            {
                remapChunksCount = InstantiateOne(
                    store, srcEntities[i], outputEntities, instanceCount, remapChunks, remapChunksCount, typeIndex, sharedComponentIndex);

                for (var r = 0; r != instanceCount; r++)
                {
                    entityRemap[(r * srcEntityCount) + i] = outputEntities[r];
                }

                if (i == 0)
                {
                    for (var r = 0; r != instanceCount; r++)
                    {
                        outputRootEntities[r] = outputEntities[r];
                    }
                }
            }

            for (var i = 0; i != remapChunksCount; i++)
            {
                var chunk = remapChunks[i].Chunk;
                var dstArchetype = store->GetArchetype(chunk);
                var localRemap = entityRemap + (remapChunks[i].InstanceBeginIndex * srcEntityCount);

                EntityRemapUtility.PatchEntitiesForPrefab(dstArchetype->ScalarEntityPatches + 1, dstArchetype->ScalarEntityPatchCount - 1,
                    dstArchetype->BufferEntityPatches, dstArchetype->BufferEntityPatchCount,
                    chunk.Buffer, remapChunks[i].IndexInChunk, remapChunks[i].AllocatedCount, srcEntities, localRemap, srcEntityCount);
            }

            if (tempAllocSize > kMaxStackAllocSize)
            {
                UnsafeUtility.Free(allocation, Allocator.Temp);
            }
        }

        private static int InstantiateOne(EntityComponentStore* store, Entity srcEntity, Entity* outputEntities, int instanceCount, RemapChunk* remapChunks,
            int remapChunksCount, TypeIndex typeIndex, int sharedComponentIndex)
        {
            var src = store->GetEntityInChunk(srcEntity);
            var srcArchetype = store->GetArchetype(src.Chunk);
            var dstArchetype = srcArchetype->InstantiateArchetype;

            var archetypeChunkFilter = new EntityComponentStore.ArchetypeChunkFilter { Archetype = dstArchetype };

            var srcSharedComponentValues = srcArchetype->Chunks.GetSharedComponentValues(src.Chunk.ListIndex);
            if (dstArchetype->NumSharedComponents > 0 && dstArchetype->NumSharedComponents != srcArchetype->NumSharedComponents)
            {
                // Cleanup shared components are not instantiated: keep the values of the types that remain.
                var oldFirstShared = srcArchetype->FirstSharedComponent;
                var newFirstShared = dstArchetype->FirstSharedComponent;
                for (int oldIndex = 0, newIndex = 0; newIndex < dstArchetype->NumSharedComponents; ++newIndex, ++oldIndex)
                {
                    var t = dstArchetype->Types[newIndex + newFirstShared];
                    while (t != srcArchetype->Types[oldIndex + oldFirstShared])
                    {
                        ++oldIndex;
                    }

                    archetypeChunkFilter.SharedComponentValues[newIndex] = srcSharedComponentValues[oldIndex];
                }
            }
            else
            {
                // Always copy shared component indices since GetChunkWithEmptySlots might reallocate the storage of SharedComponentValues
                srcSharedComponentValues.CopyTo(archetypeChunkFilter.SharedComponentValues, 0, dstArchetype->NumSharedComponents);
            }

            // The only change from InstantiateEntitiesOne: clones go to the chunks of the requested value.
            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(dstArchetype, typeIndex);
            if (indexInTypeArray != -1)
            {
                archetypeChunkFilter.SharedComponentValues[indexInTypeArray - dstArchetype->FirstSharedComponent] = sharedComponentIndex;
            }

            var instanceBeginIndex = 0;
            while (instanceBeginIndex != instanceCount)
            {
                var chunk = store->GetChunkWithEmptySlots(ref archetypeChunkFilter);
                var indexInChunk = chunk.Count;
                var unusedCount = dstArchetype->ChunkCapacity - indexInChunk;
                var allocateCount = math.min(instanceCount - instanceBeginIndex, unusedCount);

                ChunkDataUtility.AllocateClone(dstArchetype, chunk, outputEntities + instanceBeginIndex, allocateCount, srcEntity);

                remapChunks[remapChunksCount] = new RemapChunk
                {
                    Chunk = chunk,
                    IndexInChunk = indexInChunk,
                    AllocatedCount = allocateCount,
                    InstanceBeginIndex = instanceBeginIndex,
                };

                remapChunksCount++;
                instanceBeginIndex += allocateCount;
            }

            return remapChunksCount;
        }

        private struct RemapChunk
        {
            public ChunkIndex Chunk;
            public int IndexInChunk;
            public int AllocatedCount;
            public int InstanceBeginIndex;
        }
    }
}
