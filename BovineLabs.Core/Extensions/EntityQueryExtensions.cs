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
            var impl = query._GetImpl();

            AssertRange(index, impl->_Filter.Shared.Count);

            // Reset only the index - from ResetFilter
            impl->_Access->EntityComponentStore->RemoveSharedComponentReference_Unmanaged(
                impl->_Filter.Shared.SharedComponentIndex[index]);

            // Replace with our new component - from AddSharedComponentFilter
            impl->_Filter.Shared.IndexInEntityQuery[index] = query.GetIndexInEntityQuery(TypeManager.GetTypeIndex<T>());
            impl->_Filter.Shared.SharedComponentIndex[index] = impl->_Access->InsertSharedComponent_Unmanaged(sharedComponent);
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

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            CheckSingletonBufferQueryAccess(impl, typeIndex, isReadOnly);
#endif

            impl->GetSingletonChunkAndEntity(typeIndex, out var indexInArchetype, out var chunk, out var entityIndexInChunk);
#if UNITY_INCLUDE_INSTRUMENTATION && !DISABLE_ENTITIES_JOURNALING
#pragma warning disable 0618
            if (Hint.Unlikely(impl->_Access->EntityComponentStore->m_RecordToJournal != 0) && !isReadOnly)
            {
                impl->RecordSingletonJournalRW(chunk, typeIndex, EntitiesJournaling.RecordType.GetBufferRW);
#pragma warning restore 0618
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

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
        private static void CheckSingletonBufferQueryAccess(EntityQueryImpl* impl, TypeIndex typeIndex, bool isReadOnly)
        {
            for (var i = 0; i < impl->_QueryData->RequiredComponentsCount; i++)
            {
                var component = impl->_QueryData->RequiredComponents[i];
                if (component.TypeIndex != typeIndex || component.AccessModeType == ComponentType.AccessMode.Exclude)
                {
                    continue;
                }

                if (isReadOnly)
                {
                    return;
                }

                if (component.AccessModeType == ComponentType.AccessMode.ReadWrite)
                {
                    return;
                }

                break;
            }

            var typeName = typeIndex.ToFixedString();
            if (isReadOnly)
            {
                throw new InvalidOperationException(
                    $"GetSingletonBufferNoSync<{typeName}>(true) requires {typeName} to be included in the EntityQuery.");
            }

            throw new InvalidOperationException(
                $"GetSingletonBufferNoSync<{typeName}>(false) requires {typeName} to be included in the EntityQuery with read-write access.");
        }
#endif

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
