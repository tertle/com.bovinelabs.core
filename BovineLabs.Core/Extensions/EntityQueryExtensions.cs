// <copyright file="EntityQueryExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Internal;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class EntityQueryExtensions
    {
        public static bool QueryHasSharedFilter<T>(this EntityQuery query, out int scdIndex)
            where T : unmanaged, ISharedComponentData
        {
            var filters = query.GetSharedFilters();
            var requiredType = TypeManager.GetTypeIndex<T>();

            for (var i = 0; i < filters.Count; i++)
            {
                var indexInEntityQuery = filters.IndexInEntityQuery[i];
                var component = query.__impl->_QueryData->RequiredComponents[indexInEntityQuery].TypeIndex;
                if (component == requiredType)
                {
                    scdIndex = filters.SharedComponentIndex[i];
                    return true;
                }
            }

            scdIndex = -1;
            return false;
        }

        public static bool QueryHasSharedFilter<T>(this EntityQuery query, int index)
            where T : unmanaged, ISharedComponentData
        {
            var impl = query._GetImpl();
            var filters = query.GetSharedFilters();
            var requiredType = TypeManager.GetTypeIndex<T>();

            AssertRange(index, impl->_Filter.Shared.Count);

            var indexInEntityQuery = filters.IndexInEntityQuery[index];
            var component = query.__impl->_QueryData->RequiredComponents[indexInEntityQuery].TypeIndex;
            if (component == requiredType)
            {
                return true;
            }

            return false;
        }

        public static void ReplaceSharedComponentFilter<T>(this EntityQuery query, int index, T sharedComponent)
            where T : unmanaged, ISharedComponentData
        {
            var impl = query._GetImpl();

            AssertRange(index, impl->_Filter.Shared.Count);

            // Reset only the index - from ResetFilter
            impl->_Access->EntityComponentStore->RemoveSharedComponentReference_Unmanaged(
                impl->_Filter.Shared.SharedComponentIndex[index]);

            // Replace with our new component - from AddSharedComponentFilter
            impl->_Filter.Shared.IndexInEntityQuery[index] = query.GetIndexInEntityQuery(TypeManager.GetTypeIndex<T>());
            impl->_Filter.Shared.SharedComponentIndex[index] = impl->_Access->InsertSharedComponent_Unmanaged(sharedComponent);
        }

        public static Entity GetFirstEntity(this EntityQuery query)
        {
            EntityQueryImpl* impl = query._GetImpl();

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (impl->_QueryData->HasEnableableComponents != 0)
                throw new InvalidOperationException("Can't call GetFirstEntity() on queries containing enableable component types.");
#endif
            impl->GetFirstChunkAndEntity(TypeManager.GetTypeIndex<Entity>(), out _, out var chunk, out var entityIndexInChunk);
            var archetype = impl->_Access->EntityComponentStore->GetArchetype(chunk);
            Entity* chunkEntities = (Entity*)ChunkIterationUtility.GetChunkComponentDataROPtr(archetype, chunk, 0);
            return UnsafeUtility.AsRef<Entity>(chunkEntities + entityIndexInChunk);
        }

        public static UntypedDynamicBuffer GetSingletonUntypedBuffer(this EntityQuery query, ComponentType componentType, bool isReadOnly)
        {
            var impl = query._GetImpl();

            var typeIndex = componentType.TypeIndex;
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (TypeManager.IsEnableable(typeIndex))
            {
                var typeName = typeIndex.ToFixedString();
                throw new InvalidOperationException($"Can't call GetSingletonBuffer<{typeName}>() with enableable component type {typeName}.");
            }
#endif

            impl->GetSingletonChunkAndEntity(typeIndex, out var indexInArchetype, out var chunk, out var entityIndexInChunk);
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(impl->_Access->EntityComponentStore->m_RecordToJournal != 0) && !isReadOnly)
            {
                impl->RecordSingletonJournalRW(chunk, typeIndex, EntitiesJournaling.RecordType.GetBufferRW);
            }
#endif

            var archetype = impl->_Access->EntityComponentStore->GetArchetype(chunk);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandles = &impl->_Access->DependencyManager->Safety;

            var bufferAccessor = GetChunkBufferAccessor(archetype, chunk, typeIndex, !isReadOnly, indexInArchetype,
                impl->_Access->EntityComponentStore->GlobalSystemVersion, safetyHandles->GetSafetyHandle(typeIndex, isReadOnly),
                safetyHandles->GetBufferSafetyHandle(typeIndex));
#else
            var bufferAccessor = GetChunkBufferAccessor(archetype, chunk, typeIndex, !isReadOnly, indexInArchetype,
                impl->_Access->EntityComponentStore->GlobalSystemVersion);
#endif

            return bufferAccessor.GetUntypedBuffer(entityIndexInChunk);
        }

        public static DynamicBuffer<T> GetSingletonBufferNoSync<T>(this EntityQuery query, bool isReadOnly)
            where T : unmanaged, IBufferElementData
        {
            var impl = query._GetImpl();

            var typeIndex = TypeManager.GetTypeIndex<T>();
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (TypeManager.IsEnableable(typeIndex))
            {
                var typeName = typeIndex.ToFixedString();
                throw new InvalidOperationException($"Can't call GetSingletonBuffer<{typeName}>() with enableable component type {typeName}.");
            }
#endif

            impl->GetSingletonChunkAndEntity(typeIndex, out var indexInArchetype, out var chunk, out var entityIndexInChunk);
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(impl->_Access->EntityComponentStore->m_RecordToJournal != 0) && !isReadOnly)
            {
                impl->RecordSingletonJournalRW(chunk, typeIndex, EntitiesJournaling.RecordType.GetBufferRW);
            }
#endif

            var archetype = impl->_Access->EntityComponentStore->GetArchetype(chunk);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandles = &impl->_Access->DependencyManager->Safety;
            var bufferAccessor = ChunkIterationUtility.GetChunkBufferAccessor<T>(archetype, chunk, !isReadOnly, indexInArchetype,
                impl->_Access->EntityComponentStore->GlobalSystemVersion, safetyHandles->GetSafetyHandle(typeIndex, isReadOnly),
                safetyHandles->GetBufferSafetyHandle(typeIndex));
#else
            var bufferAccessor = ChunkIterationUtility.GetChunkBufferAccessor<T>(archetype, chunk, !isReadOnly, indexInArchetype,
                impl->_Access->EntityComponentStore->GlobalSystemVersion);
#endif

            return bufferAccessor.GetUnsafe(entityIndexInChunk);
        }

        public static bool TryGetSingletonBufferNoSync<T>(this EntityQuery query, out DynamicBuffer<T> buffer, bool isReadOnly)
            where T : unmanaged, IBufferElementData
        {
            var hasSingleton = query.HasSingleton<T>();
            buffer = hasSingleton ? query.GetSingletonBufferNoSync<T>(isReadOnly) : default;
            return hasSingleton;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void AssertRange(int index, int count)
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Trying to replace shared filter outside of range");
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static DynamicBufferAccessor GetChunkBufferAccessor(Archetype* archetype, ChunkIndex chunk, TypeIndex typeIndex, bool isWriting, int typeIndexInArchetype, uint systemVersion, AtomicSafetyHandle safety0, AtomicSafetyHandle safety1)
#else
        private static DynamicBufferAccessor GetChunkBufferAccessor(Archetype* archetype, ChunkIndex chunk, TypeIndex typeIndex, bool isWriting, int typeIndexInArchetype, uint systemVersion)
#endif
        {
            int internalCapacity = archetype->BufferCapacities[typeIndexInArchetype];

            byte* ptr = (!isWriting)
                ? ChunkDataUtility.GetComponentDataRO(chunk, archetype, 0, typeIndexInArchetype)
                : ChunkDataUtility.GetComponentDataRW(chunk, archetype, 0, typeIndexInArchetype, systemVersion);

            var length = chunk.Count;
            int stride = archetype->SizeOfs[typeIndexInArchetype];

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(archetype->EntityComponentStore->m_RecordToJournal != 0) && isWriting)
            {
                EntitiesJournaling.AddRecord(
                    recordType: EntitiesJournaling.RecordType.GetBufferRW,
                    entityComponentStore: archetype->EntityComponentStore,
                    globalSystemVersion: systemVersion,
                    archetype: archetype,
                    chunk: chunk,
                    types: &archetype->Types[typeIndexInArchetype].TypeIndex,
                    typeCount: 1);
            }
#endif

            var typeInfo = TypeManager.GetTypeInfo(typeIndex);
            var elementSize = typeInfo.ElementSize;
            var elementAlign = typeInfo.AlignmentInBytes;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBufferAccessor(ptr, length, stride, elementSize, elementAlign, internalCapacity, !isWriting, safety0, safety1);
#else
            return new DynamicBufferAccessor(ptr, length, stride, elementSize, elementAlign, internalCapacity);
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void GetFirstChunkAndEntity(this EntityQueryImpl impl, TypeIndex typeIndex, out int outIndexInArchetype, out ChunkIndex outChunk, out int outEntityIndexInChunk)
        {
            if (!impl._Filter.RequiresMatchesFilter && impl._QueryData->HasEnableableComponents == 0 &&
                impl._QueryData->RequiredComponentsCount <= 2 && impl._QueryData->RequiredComponents[1].TypeIndex == typeIndex)
            {
                // Fast path with no filtering
                var matchingChunkCache = impl.GetMatchingChunkCache();
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (matchingChunkCache.Length == 0 || matchingChunkCache.ChunkIndices[0].Count == 0)
                {
                    throw new InvalidOperationException($"GetFirst() requires that exactly one entity exists that match this query, but there are none.");
                }
#endif
                outChunk = matchingChunkCache.ChunkIndices[0];
                var matchIndex = matchingChunkCache.PerChunkMatchingArchetypeIndex->Ptr[0];
                var match = impl._QueryData->MatchingArchetypes.Ptr[matchIndex];
                outIndexInArchetype = match->IndexInArchetype[1];
                outEntityIndexInChunk = 0;
            }
            else
            {
                // Slow path with filtering, can't just use first matching archetype/chunk
                impl.SyncFilterTypes();
                int queryEntityCount = ChunkIterationUtility.CalculateEntityCountAndSingleton(impl.GetMatchingChunkCache(),
                    ref impl._QueryData->MatchingArchetypes, ref impl._Filter, impl._QueryData->HasEnableableComponents,
                    out var firstMatchArchetype, out outChunk, out outEntityIndexInChunk);
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (queryEntityCount == 1)
                {
                    impl._QueryData->CheckChunkListCacheConsistency(false);
                    throw new InvalidOperationException("GetFirst() requires at least one entity exists that matches this query, but there are none.");
                }
#endif
                var indexInQuery = impl.GetIndexInEntityQuery(typeIndex);
                outIndexInArchetype = firstMatchArchetype->IndexInArchetype[indexInQuery];
            }
        }
    }
}
