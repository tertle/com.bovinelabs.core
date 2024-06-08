// <copyright file="NativeHashMapFactory.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeHashMapFactory<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static readonly SharedStatic<int> StaticSafetyId = SharedStatic<int>.GetOrCreate<NativeHashMap<TKey, TValue>>();
#endif

        public static NativeHashMap<TKey, TValue> Create(int initialCapacity, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            var hashMap = default(NativeHashMap<TKey, TValue>);
            hashMap.m_Data = HashMapHelper<TKey>.Alloc(initialCapacity, sizeof(TValue), minGrowth, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            hashMap.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<TKey>() || UnsafeUtility.IsNativeContainerType<TValue>())
            {
                AtomicSafetyHandle.SetNestedContainer(hashMap.m_Safety, true);
            }

            CollectionHelper.SetStaticSafetyId<NativeHashMap<TKey, TValue>>(ref hashMap.m_Safety, ref StaticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(hashMap.m_Safety, true);
#endif

            return hashMap;
        }
    }
}
