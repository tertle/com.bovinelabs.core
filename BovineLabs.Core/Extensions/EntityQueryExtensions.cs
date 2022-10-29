// <copyright file="EntityQueryExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.Internal;
    using Unity.Entities;

    public static unsafe class EntityQueryExtensions
    {
        public static bool QueryHasSharedFilter<T>(this EntityQuery query, out int scdIndex)
            where T : struct, ISharedComponentData
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
            where T : struct, ISharedComponentData
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
            where T : struct, ISharedComponentData
        {
            AssertRange(index, query._GetImpl()->_Filter.Shared.Count);

            // Reset only the index - from ResetFilter
            query._GetImpl()->_Access->RemoveSharedComponentReference(query._GetImpl()->_Filter.Shared.SharedComponentIndex[index]);

            // Replace with our new component - from AddSharedComponentFilter
            query._GetImpl()->_Filter.Shared.IndexInEntityQuery[index] = query.GetIndexInEntityQuery(TypeManager.GetTypeIndex<T>());
            query._GetImpl()->_Filter.Shared.SharedComponentIndex[index] = query._GetImpl()->_Access->InsertSharedComponent(sharedComponent);
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
