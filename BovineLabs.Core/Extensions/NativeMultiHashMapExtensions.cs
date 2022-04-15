// <copyright file="NativeMultiHashMapExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> Extensions for <see cref="NativeMultiHashMap{TKey,TValue}"/>. </summary>
    public static class NativeMultiHashMapExtensions
    {
        /// <summary>
        /// Clear a <see cref="NativeMultiHashMap{TKey,TValue}"/> then efficiently add a collection of keys and values to it.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static unsafe void ClearAndAddBatch<TKey, TValue>(
            [NoAlias] this NativeMultiHashMap<TKey, TValue> hashMap,
            [NoAlias] NativeArray<TKey> keys,
            [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys, values);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            hashMap.Clear();

            if (hashMap.Capacity < keys.Length)
            {
                hashMap.Capacity = keys.Length;
            }

            var data = hashMap.GetUnsafeBucketData();
            UnsafeUtility.MemCpy(data.keys, keys.GetUnsafeReadOnlyPtr(), keys.Length * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(data.values, values.GetUnsafeReadOnlyPtr(), values.Length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next;

            for (var idx = 0; idx < keys.Length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength = keys.Length;
        }

        /// <summary>
        /// Clear a <see cref="NativeMultiHashMap{TKey,TValue}"/> then efficiently add a collection of keys and values to it.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static unsafe void ClearAndAddBatch<TKey, TValue>(
            [NoAlias] this NativeMultiHashMap<TKey, TValue> hashMap,
            [NoAlias] NativeSlice<TKey> keys,
            [NoAlias] NativeSlice<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys, values);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            hashMap.Clear();

            if (hashMap.Capacity < keys.Length)
            {
                hashMap.Capacity = keys.Length;
            }

            var data = hashMap.GetUnsafeBucketData();

            UnsafeUtility.MemCpyStride(data.keys, UnsafeUtility.SizeOf<TKey>(), keys.GetUnsafeReadOnlyPtr(), keys.Stride, UnsafeUtility.SizeOf<TValue>(), keys.Length);
            UnsafeUtility.MemCpyStride(data.values, UnsafeUtility.SizeOf<TValue>(), values.GetUnsafeReadOnlyPtr(), values.Stride, UnsafeUtility.SizeOf<TValue>(), values.Length);

            // UnsafeUtility.MemCpy(data.keys, keys.GetUnsafeReadOnlyPtr(), keys.Length * UnsafeUtility.SizeOf<TKey>());
            // UnsafeUtility.MemCpy(data.values, values.GetUnsafeReadOnlyPtr(), values.Length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next;

            for (var idx = 0; idx < keys.Length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength = keys.Length;
        }

        /// <summary>
        /// Efficiently add a collection of keys and values to a <see cref="NativeMultiHashMap{TKey,TValue}"/>.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <remarks> Should only be used on a hashmap that has not had an element removed. </remarks>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static unsafe void AddBatch<TKey, TValue>(
            [NoAlias] this NativeMultiHashMap<TKey, TValue> hashMap,
            [NoAlias] NativeArray<TKey> keys,
            [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys, values);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var oldLength = hashMap.Count();
            var newLength = oldLength + keys.Length;

            if (hashMap.Capacity < newLength)
            {
                hashMap.Capacity = newLength;
            }

            var data = hashMap.GetUnsafeBucketData();

            var keyPtr = ((TKey*)data.keys) + oldLength;
            var valuePtr = ((TValue*)data.values) + oldLength;

            UnsafeUtility.MemCpy(keyPtr, keys.GetUnsafeReadOnlyPtr(), keys.Length * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(valuePtr, values.GetUnsafeReadOnlyPtr(), values.Length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)data.buckets;
            var nextPtrs = ((int*)data.next) + oldLength;

            for (var idx = 0; idx < keys.Length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength += keys.Length;
        }

        /// <summary>
        /// Efficiently adds a collection of values for a single key and values to a <see cref="NativeMultiHashMap{TKey,TValue}"/>.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <remarks> Should only be used on a hashmap that has not had an element removed. </remarks>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="key"> The key to use. </param>
        /// <param name="values"> Collection of values. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static unsafe void AddBatch<TKey, TValue>(
            [NoAlias] this NativeMultiHashMap<TKey, TValue> hashMap,
            [NoAlias] TKey key,
            [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var oldLength = hashMap.Count();
            var newLength = oldLength + values.Length;

            if (hashMap.Capacity < newLength)
            {
                hashMap.Capacity = newLength;
            }

            var data = hashMap.GetUnsafeBucketData();

            var keyPtr = ((TKey*)data.keys) + oldLength;
            var valuePtr = ((TValue*)data.values) + oldLength;

            UnsafeUtility.MemCpyReplicate(keyPtr, &key, UnsafeUtility.SizeOf<TKey>(), values.Length);
            UnsafeUtility.MemCpy(valuePtr, values.GetUnsafeReadOnlyPtr(), values.Length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)data.buckets;
            var nextPtrs = ((int*)data.next) + oldLength;

            var bucket = key.GetHashCode() & data.bucketCapacityMask;

            for (var idx = 0; idx < values.Length; idx++)
            {
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength += values.Length;
        }

        /// <summary>
        /// Efficiently adds a collection of values for a single key and values to a <see cref="NativeMultiHashMap{TKey,TValue}"/>.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <remarks> Should only be used on a hashmap that has not had an element removed. </remarks>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="key"> The key to use. </param>
        /// <param name="values"> Pointer to the values. </param>
        /// <param name="length"> The length of the values. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static unsafe void AddBatch<TKey, TValue>(
            [NoAlias] this NativeMultiHashMap<TKey, TValue> hashMap,
            TKey key,
            [NoAlias] TValue* values,
            int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var oldLength = hashMap.Count();
            var newLength = oldLength + length;

            if (hashMap.Capacity < newLength)
            {
                hashMap.Capacity = newLength;
            }

            var data = hashMap.GetUnsafeBucketData();

            var keyPtr = ((TKey*)data.keys) + oldLength;
            var valuePtr = ((TValue*)data.values) + oldLength;

            UnsafeUtility.MemCpyReplicate(keyPtr, &key, UnsafeUtility.SizeOf<TKey>(), length);
            UnsafeUtility.MemCpy(valuePtr, values, length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)data.buckets;
            var nextPtrs = ((int*)data.next) + oldLength;

            var bucket = key.GetHashCode() & data.bucketCapacityMask;

            for (var idx = 0; idx < length; idx++)
            {
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength += length;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckLengthsMatch<TKey, TValue>(NativeArray<TKey> keys, NativeArray<TValue> values)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (keys.Length != values.Length)
            {
                throw new ArgumentException("Key and value array don't match");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckLengthsMatch<TKey, TValue>(NativeSlice<TKey> keys, NativeSlice<TValue> values)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (keys.Length != values.Length)
            {
                throw new ArgumentException("Key and value array don't match");
            }
#endif
        }
    }
}
