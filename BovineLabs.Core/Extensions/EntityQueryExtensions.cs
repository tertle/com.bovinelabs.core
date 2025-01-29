// <copyright file="EntityQueryExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.Internal;
    using Unity.Burst.CompilerServices;
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
            AssertRange(index, query._GetImpl()->_Filter.Shared.Count);

            // Reset only the index - from ResetFilter
            query._GetImpl()->_Access->EntityComponentStore->RemoveSharedComponentReference_Unmanaged(
                query._GetImpl()->_Filter.Shared.SharedComponentIndex[index]);

            // Replace with our new component - from AddSharedComponentFilter
            query._GetImpl()->_Filter.Shared.IndexInEntityQuery[index] = query.GetIndexInEntityQuery(TypeManager.GetTypeIndex<T>());
            query._GetImpl()->_Filter.Shared.SharedComponentIndex[index] = query._GetImpl()->_Access->InsertSharedComponent_Unmanaged(sharedComponent);
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
    }
}
