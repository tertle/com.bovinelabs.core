// <copyright file="NativeHashSetFactory.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeHashSetFactory<T>
        where T : unmanaged, IEquatable<T>
    {
        public static NativeHashSet<T> Create(int initialCapacity, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            var hashSet = default(NativeHashSet<T>);
            hashSet.m_Data = HashMapHelper<T>.Alloc(initialCapacity, 0, minGrowth, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            hashSet.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<T>())
            {
                AtomicSafetyHandle.SetNestedContainer(hashSet.m_Safety, true);
            }

            CollectionHelper.SetStaticSafetyId<NativeHashSet<T>>(ref hashSet.m_Safety, ref NativeHashSet<T>.s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(hashSet.m_Safety, true);
#endif

            return hashSet;
        }
    }
}
