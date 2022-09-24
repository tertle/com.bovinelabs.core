// <copyright file="CollectionInternals.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static class CollectionInternals
    {
        // NativeHashMap
        public static UnsafeParallelHashMap<TKey, TValue> GetUnsafeHashMap<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> hashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif
            return hashMap.m_HashMapData;
        }

        public static UnsafeParallelHashMap<TKey, TValue> GetReadOnlyUnsafeHashMap<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> hashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.m_Safety);
#endif
            return hashMap.m_HashMapData;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static void SetSafety<TKey, TValue>(this ref NativeParallelHashMap<TKey, TValue> hashMap, AtomicSafetyHandle safetyHandle)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            hashMap.m_Safety = safetyHandle;
        }
#endif

        public static NativeParallelHashMap<TKey, TValue> AsNative<TKey, TValue>(this UnsafeParallelHashMap<TKey, TValue> hashMapData)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return new NativeParallelHashMap<TKey, TValue> { m_HashMapData = hashMapData };
        }

        // NativeMultiHashMap
        public static UnsafeParallelMultiHashMap<TKey, TValue> GetUnsafeMultiHashMap<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> hashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif
            return hashMap.m_MultiHashMapData;
        }

        public static UnsafeParallelMultiHashMap<TKey, TValue> GetReadOnlyUnsafeMultiHashMap<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> hashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.m_Safety);
#endif
            return hashMap.m_MultiHashMapData;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static void SetSafety<TKey, TValue>(this ref NativeParallelMultiHashMap<TKey, TValue> hashMap, AtomicSafetyHandle safetyHandle)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            hashMap.m_Safety = safetyHandle;
        }
#endif

        public static NativeParallelMultiHashMap<TKey, TValue> AsNative<TKey, TValue>(this UnsafeParallelMultiHashMap<TKey, TValue> hashMapData)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return new NativeParallelMultiHashMap<TKey, TValue> { m_MultiHashMapData = hashMapData };
        }

        public static unsafe byte* Buffer<T>(this FixedList32Bytes<T> list)
            where T : unmanaged => list.Buffer;

        public static unsafe byte* Buffer<T>(this FixedList64Bytes<T> list)
            where T : unmanaged => list.Buffer;

        public static unsafe byte* Buffer<T>(this FixedList128Bytes<T> list)
            where T : unmanaged => list.Buffer;

        public static unsafe byte* Buffer<T>(this FixedList512Bytes<T> list)
            where T : unmanaged => list.Buffer;

        public static unsafe byte* Buffer<T>(this FixedList4096Bytes<T> list)
            where T : unmanaged => list.Buffer;

        // NativeReference
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static AtomicSafetyHandle GetSafetyHandle<T>(this NativeReference<T> hashMap)
            where T : unmanaged
        {
            return hashMap.m_Safety;
        }
#endif
    }
}
