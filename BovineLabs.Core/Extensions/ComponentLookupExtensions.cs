// <copyright file="ComponentLookupExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class ComponentLookupExtensions
    {
        public static T* GetOptionalComponentDataRW<T>(this ComponentLookup<T> lookup, Entity entity)
            where T : unmanaged, IComponentData
        {
            ref var lookupInternal = ref UnsafeUtility.As<ComponentLookup<T>, ComponentLookupInternal>(ref lookup);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(lookupInternal.m_Safety);
#endif
            AssertExistsNotZeroSize(ref lookupInternal);

            var ecs = lookupInternal.m_Access->EntityComponentStore;
            ecs->AssertEntitiesExist(&entity, 1);

            return (T*)ecs->GetOptionalComponentDataWithTypeRW(entity, lookupInternal.m_TypeIndex, lookupInternal.m_GlobalSystemVersion, ref lookupInternal.m_Cache);
        }

        private struct ComponentLookupInternal
        {
            [NativeDisableUnsafePtrRestriction]
            public readonly EntityDataAccess* m_Access;

            public LookupCache m_Cache;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public AtomicSafetyHandle m_Safety;
#endif
            public readonly TypeIndex m_TypeIndex;
            public uint m_GlobalSystemVersion;
            public readonly byte m_IsZeroSized; // cache of whether T is zero-sized
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public readonly byte m_IsReadOnly;
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void AssertExistsNotZeroSize(ref ComponentLookupInternal lookup)
        {
            if (lookup.m_IsZeroSized != 0)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                throw new ArgumentException("zero sized component");
#endif
            }
        }
    }
}
