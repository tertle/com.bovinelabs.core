// <copyright file="EntityQueryInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct SharedComponentData
    {
        public int Count;
        public fixed int IndexInEntityQuery[EntityQueryFilter.SharedComponentData.Capacity];
        public fixed int SharedComponentIndex[EntityQueryFilter.SharedComponentData.Capacity];
    }

    public static unsafe class EntityQueryInternal
    {
        public static SharedComponentData GetSharedFilters(this EntityQuery query)
        {
            return UnsafeUtility.As<EntityQueryFilter.SharedComponentData, SharedComponentData>(ref query._GetImpl()->_Filter.Shared);
        }

        public static ref SharedComponentData GetSharedFiltersAsRef(this EntityQuery query)
        {
            ref var entityQueryImpl = ref UnsafeUtility.AsRef<EntityQueryImpl>(query._GetImpl());
            return ref UnsafeUtility.As<EntityQueryFilter.SharedComponentData, SharedComponentData>(ref entityQueryImpl._Filter.Shared);
        }
    }
}
