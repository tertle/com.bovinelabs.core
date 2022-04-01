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
        public static UnsafeHashMap<TKey, TValue> GetUnsafeHashMap<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif
            return hashMap.m_HashMapData;
        }

        public static UnsafeHashMap<TKey, TValue> GetReadOnlyUnsafeHashMap<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.m_Safety);
#endif
            return hashMap.m_HashMapData;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static void SetSafety<TKey, TValue>(this ref NativeHashMap<TKey, TValue> hashMap, AtomicSafetyHandle safetyHandle)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            hashMap.m_Safety = safetyHandle;
        }
#endif

        public static NativeHashMap<TKey, TValue> AsNative<TKey, TValue>(this UnsafeHashMap<TKey, TValue> hashMapData)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return new NativeHashMap<TKey, TValue> { m_HashMapData = hashMapData };
        }

        // NativeMultiHashMap
        public static UnsafeMultiHashMap<TKey, TValue> GetUnsafeMultiHashMap<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> hashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif
            return hashMap.m_MultiHashMapData;
        }

        public static UnsafeMultiHashMap<TKey, TValue> GetReadOnlyUnsafeMultiHashMap<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> hashMap)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(hashMap.m_Safety);
#endif
            return hashMap.m_MultiHashMapData;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static void SetSafety<TKey, TValue>(this ref NativeMultiHashMap<TKey, TValue> hashMap, AtomicSafetyHandle safetyHandle)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            hashMap.m_Safety = safetyHandle;
        }
#endif

        public static NativeMultiHashMap<TKey, TValue> AsNative<TKey, TValue>(this UnsafeMultiHashMap<TKey, TValue> hashMapData)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return new NativeMultiHashMap<TKey, TValue> { m_MultiHashMapData = hashMapData };
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
