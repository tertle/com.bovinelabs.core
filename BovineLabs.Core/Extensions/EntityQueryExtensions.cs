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
        public static bool TryGetSharedComponentInFilter<T>(this EntityQuery query, EntityManager entityManager, out T sharedComponent)
            where T : struct, ISharedComponentData
        {
            var filters = query.GetSharedFilters();

            for (var i = 0; i < filters.Count; i++)
            {
                var scdIndex = filters.SharedComponentIndex[i];
                var scd = entityManager.GetSharedComponentDataNonDefaultBoxed(scdIndex);

                if (scd is T component)
                {
                    sharedComponent = component;
                    return true;
                }
            }

            sharedComponent = default;
            return false;
        }

        public static bool TryGetSharedComponentInFilter<T>(this EntityQuery query, EntityManager entityManager, int index, out T sharedComponent)
            where T : struct, ISharedComponentData
        {
            AssertRange(index, query._GetImpl()->_Filter.Shared.Count);

            var scdIndex = query._GetImpl()->_Filter.Shared.SharedComponentIndex[index];
            var scd = entityManager.GetSharedComponentDataNonDefaultBoxed(scdIndex);

            if (scd is T component)
            {
                sharedComponent = component;
                return true;
            }

            sharedComponent = default;
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
