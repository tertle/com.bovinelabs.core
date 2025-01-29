// <copyright file="NativeParallelMultiHashMapExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary> Extensions for <see cref="NativeParallelMultiHashMap{TKey,TValue}" />. </summary>
    public static unsafe class NativeParallelMultiHashMapExtensions
    {
        public static void GetUniqueKeyArray<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> container, NativeList<TKey> keys)
            where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
            where TValue : unmanaged
        {
            // Count provides read safety
            keys.ResizeUninitialized(container.Count());
            GetUniqueArray(container.m_MultiHashMapData, keys);
        }

        public static void GetUniqueKeyArray<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue>.ReadOnly container, NativeList<TKey> keys)
            where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
            where TValue : unmanaged
        {
            // Count provides read safety
            keys.ResizeUninitialized(container.Count());
            GetUniqueArray(container.m_MultiHashMapData, keys);
        }

        public static int Reserve<TKey, TValue>([NoAlias] this NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.m_Writer.m_Buffer->ReserveParallel(length);
        }

        public static bool TryReserve<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, int length, out int oldLength)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.m_Writer.m_Buffer->TryReserveParallel(length, out oldLength);
        }

        public static UnsafeParallelHashMapBucketData GetUnsafeBucketData<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.m_Writer.m_Buffer->GetBucketData();
        }

        public static void ClearLengthBuckets<TKey, TValue>([NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var data = hashMap.m_MultiHashMapData.m_Buffer;
            UnsafeUtility.MemSet(data->buckets, 0xff, (data->bucketCapacityMask + 1) * 4);
            data->allocatedIndexLength = 0;
        }

        /// <summary>
        /// Clear a <see cref="NativeParallelMultiHashMap{TKey,TValue}" /> then efficiently add a collection of keys and values to it.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void ClearAndAddBatch<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, [NoAlias] NativeArray<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys.Length, values.Length);

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
        /// Clear a <see cref="NativeParallelMultiHashMap{TKey,TValue}" /> then efficiently add a collection of keys and values to it.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void ClearAndAddBatch<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, [NoAlias] NativeSlice<TKey> keys, [NoAlias] NativeSlice<TValue> values)
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

            UnsafeUtility.MemCpyStride(data.keys, UnsafeUtility.SizeOf<TKey>(), keys.GetUnsafeReadOnlyPtr(), keys.Stride, UnsafeUtility.SizeOf<TKey>(),
                keys.Length);

            UnsafeUtility.MemCpyStride(data.values, UnsafeUtility.SizeOf<TValue>(), values.GetUnsafeReadOnlyPtr(), values.Stride,
                UnsafeUtility.SizeOf<TValue>(), values.Length);

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
        /// Efficiently add a collection of keys and values to a <see cref="NativeParallelMultiHashMap{TKey,TValue}" />.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <remarks> Should only be used on a hashmap that has not had an element removed. </remarks>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, [NoAlias] NativeArray<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys.Length, values.Length);
            AddBatchUnsafe(hashMap, (TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, [NoAlias] TKey* keys, [NoAlias] TValue* values, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var oldLength = hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength;
            var newLength = oldLength + length;

            if (hashMap.Capacity < newLength)
            {
                hashMap.Capacity = newLength;
            }

            var data = hashMap.GetUnsafeBucketData();

            var keyPtr = (TKey*)data.keys + oldLength;
            var valuePtr = (TValue*)data.values + oldLength;

            UnsafeUtility.MemCpy(keyPtr, keys, length * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(valuePtr, values, length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next + oldLength;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength += length;
        }

        /// <summary>
        /// Efficiently add a collection of keys and values to a <see cref="NativeParallelMultiHashMap{TKey,TValue}" />.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, [NoAlias] NativeSlice<TKey> keys, [NoAlias] NativeSlice<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif
            CheckLengthsMatch(keys.Length, values.Length);

            var length = keys.Length;
            var oldLength = hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength;
            var newLength = oldLength + length;

            if (hashMap.Capacity < newLength)
            {
                hashMap.Capacity = newLength;
            }

            var data = hashMap.GetUnsafeBucketData();

            var keyPtr = (TKey*)data.keys + oldLength;
            var valuePtr = (TValue*)data.values + oldLength;

            UnsafeUtility.MemCpyStride(keyPtr, UnsafeUtility.SizeOf<TKey>(), keys.GetUnsafeReadOnlyPtr(), keys.Stride, UnsafeUtility.SizeOf<TKey>(), length);
            UnsafeUtility.MemCpyStride(valuePtr, UnsafeUtility.SizeOf<TValue>(), values.GetUnsafeReadOnlyPtr(), values.Stride, UnsafeUtility.SizeOf<TValue>(),
                length);

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next + oldLength;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keyPtr[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength += length;
        }

        /// <summary>
        /// Efficiently add a collection of keys and values to a <see cref="NativeParallelMultiHashMap{TKey,TValue}" />.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, [NoAlias] NativeSlice<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys.Length, values.Length);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var length = keys.Length;
            var oldLength = hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength;
            var newLength = oldLength + length;

            if (hashMap.Capacity < newLength)
            {
                hashMap.Capacity = newLength;
            }

            var data = hashMap.GetUnsafeBucketData();

            var keyPtr = (TKey*)data.keys + oldLength;
            var valuePtr = (TValue*)data.values + oldLength;

            UnsafeUtility.MemCpyStride(keyPtr, UnsafeUtility.SizeOf<TKey>(), keys.GetUnsafeReadOnlyPtr(), keys.Stride, UnsafeUtility.SizeOf<TKey>(), length);
            UnsafeUtility.MemCpy(valuePtr, values.GetUnsafeReadOnlyPtr(), length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next + oldLength;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keyPtr[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength += length;
        }

        /// <summary>
        /// Efficiently adds a collection of values for a single key and values to a <see cref="NativeParallelMultiHashMap{TKey,TValue}" />.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <remarks> Should only be used on a hashmap that has not had an element removed. </remarks>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="key"> The key to use. </param>
        /// <param name="values"> Pointer to the values. </param>
        /// <param name="length"> The length of the values. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, TKey key, [NoAlias] TValue* values, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var oldLength = hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength;
            var newLength = oldLength + length;

            if (hashMap.Capacity < newLength)
            {
                hashMap.Capacity = newLength;
            }

            var data = hashMap.GetUnsafeBucketData();

            var keyPtr = (TKey*)data.keys + oldLength;
            var valuePtr = (TValue*)data.values + oldLength;

            UnsafeUtility.MemCpyReplicate(keyPtr, &key, UnsafeUtility.SizeOf<TKey>(), length);
            UnsafeUtility.MemCpy(valuePtr, values, length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next + oldLength;

            var bucket = key.GetHashCode() & data.bucketCapacityMask;

            for (var idx = 0; idx < length; idx++)
            {
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength += length;
        }

        /// <summary>
        /// Efficiently adds a collection of values for keys and single value to a <see cref="NativeParallelMultiHashMap{TKey,TValue}" />.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <remarks> Should only be used on a hashmap that has not had an element removed. </remarks>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys. </param>
        /// <param name="value"> The single value. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void AddBatchUnsafe<TKey, TValue>([NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, NativeArray<TKey> keys, TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var oldLength = hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength;
            var newLength = oldLength + keys.Length;

            if (hashMap.Capacity < newLength)
            {
                hashMap.Capacity = newLength;
            }

            var data = hashMap.GetUnsafeBucketData();

            var keyPtr = (TKey*)data.keys + oldLength;
            var valuePtr = (TValue*)data.values + oldLength;

            UnsafeUtility.MemCpy(keyPtr, keys.GetUnsafeReadOnlyPtr(), keys.Length * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpyReplicate(valuePtr, &value, UnsafeUtility.SizeOf<TValue>(), keys.Length);

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next + oldLength;

            for (var idx = 0; idx < keys.Length; idx++)
            {
                var bucket = keyPtr[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength += keys.Length;
        }

        /// <summary>
        /// Efficiently adds a collection of values for keys and single value to a <see cref="NativeParallelMultiHashMap{TKey,TValue}" />.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <remarks> Should only be used on a hashmap that has not had an element removed. </remarks>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys. </param>
        /// <param name="value"> The single value. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void AddBatchUnsafe<TKey, TValue>([NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, NativeSlice<TKey> keys, TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var oldLength = hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength;
            var newLength = oldLength + keys.Length;

            if (hashMap.Capacity < newLength)
            {
                hashMap.Capacity = newLength;
            }

            var data = hashMap.GetUnsafeBucketData();

            var keyPtr = (TKey*)data.keys + oldLength;
            var valuePtr = (TValue*)data.values + oldLength;

            UnsafeUtility.MemCpyStride(keyPtr, UnsafeUtility.SizeOf<TKey>(), keys.GetUnsafeReadOnlyPtr(), keys.Stride, UnsafeUtility.SizeOf<TKey>(),
                keys.Length);

            UnsafeUtility.MemCpyReplicate(valuePtr, &value, UnsafeUtility.SizeOf<TValue>(), keys.Length);

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next + oldLength;

            for (var idx = 0; idx < keys.Length; idx++)
            {
                var bucket = keyPtr[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength += keys.Length;
        }

        /// <summary>
        /// Efficiently adds a collection of values for a single key and values to a <see cref="NativeParallelMultiHashMap{TKey,TValue}" />.
        /// This is much faster than iterating and using Add.
        /// </summary>
        /// <remarks> Should only be used on a hashmap that has not had an element removed. </remarks>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="key"> The key to use. </param>
        /// <param name="values"> Collection of values. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, [NoAlias] TKey key, [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var oldLength = hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength;
            var newLength = oldLength + values.Length;

            if (hashMap.Capacity < newLength)
            {
                hashMap.Capacity = newLength;
            }

            var data = hashMap.GetUnsafeBucketData();

            var keyPtr = (TKey*)data.keys + oldLength;
            var valuePtr = (TValue*)data.values + oldLength;

            UnsafeUtility.MemCpyReplicate(keyPtr, &key, UnsafeUtility.SizeOf<TKey>(), values.Length);
            UnsafeUtility.MemCpy(valuePtr, values.GetUnsafeReadOnlyPtr(), values.Length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next + oldLength;

            var bucket = key.GetHashCode() & data.bucketCapacityMask;

            for (var idx = 0; idx < values.Length; idx++)
            {
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength += values.Length;
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            this NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, NativeArray<TKey> keys, NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys.Length, values.Length);
            AddBatchUnsafe(hashMap, (TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, [NoAlias] NativeArray<TKey> keys,
            [NoAlias] NativeSlice<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.m_Writer.m_Buffer->AddBatchUnsafeParallel(keys, values);
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, [NoAlias] NativeSlice<TKey> keys,
            [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.m_Writer.m_Buffer->AddBatchUnsafeParallel(keys, values);
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, [NoAlias] TKey* keys, [NoAlias] TValue* values, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.m_Writer.m_Buffer->AddBatchUnsafeParallel(keys, values, length);
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, [NoAlias] NativeArray<TKey> keys)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            AddBatchUnsafe(hashMap, (TKey*)keys.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, [NoAlias] TKey* keys, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.m_Writer.m_Buffer->AddBatchUnsafeParallel(keys, length);
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, NativeArray<TKey> keys, TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.m_Writer.m_Buffer->AddBatchUnsafeParallel((TKey*)keys.GetUnsafeReadOnlyPtr(), value, keys.Length);
        }

        public static void RecalculateBuckets<TKey, TValue>([NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif
            var length = hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength;

            var data = hashMap.GetUnsafeBucketData();
            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next;
            var keys = (TKey*)data.keys;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }
        }

        public static void Add<TKey, TValue>([NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, TKey key, TValue item, int hash)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif
            var data = hashMap.m_MultiHashMapData.m_Buffer;

            if (!UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(data, key, out _, out _))
            {
                // Allocate an entry from the free list
                int idx;
                int* nextPtrs;

                if (data->allocatedIndexLength >= data->keyCapacity && data->firstFreeTLS[0] < 0)
                {
                    var maxThreadCount = JobsUtility.ThreadIndexCount;
                    for (var tls = 1; tls < maxThreadCount; ++tls)
                    {
                        if (data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine] >= 0)
                        {
                            idx = data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine];
                            nextPtrs = (int*)data->next;
                            data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine] = nextPtrs[idx];
                            nextPtrs[idx] = -1;
                            data->firstFreeTLS[0] = idx;
                            break;
                        }
                    }

                    if (data->firstFreeTLS[0] < 0)
                    {
                        var newCap = UnsafeParallelHashMapData.GrowCapacity(data->keyCapacity);
                        UnsafeParallelHashMapData.ReallocateHashMap<TKey, TValue>(data, newCap, UnsafeParallelHashMapData.GetBucketSize(newCap),
                            hashMap.m_MultiHashMapData.m_AllocatorLabel);
                    }
                }

                idx = data->firstFreeTLS[0];

                if (idx >= 0)
                {
                    data->firstFreeTLS[0] = ((int*)data->next)[idx];
                }
                else
                {
                    idx = data->allocatedIndexLength++;
                }

                CheckIndexOutOfBounds(data, idx);

                // Write the new value to the entry
                UnsafeUtility.WriteArrayElement(data->keys, idx, key);
                UnsafeUtility.WriteArrayElement(data->values, idx, item);

                var bucket = hash & data->bucketCapacityMask;
                // Add the index to the hash-map
                var buckets = (int*)data->buckets;
                nextPtrs = (int*)data->next;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckIndexOutOfBounds(UnsafeParallelHashMapData* data, int idx)
        {
            if (idx < 0 || idx >= data->keyCapacity)
            {
                throw new InvalidOperationException("Internal HashMap error");
            }
        }

        /// <summary>
        /// Recalculates buckets with hashes already temp cached in the next array
        /// </summary>
        /// <param name="hashMap"> </param>
        /// <typeparam name="TKey"> </typeparam>
        /// <typeparam name="TValue"> </typeparam>
        public static void RecalculateBucketsCached<TKey, TValue>([NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif
            var length = hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength;

            var data = hashMap.GetUnsafeBucketData();
            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = nextPtrs[idx] & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }
        }

        public static void SetAllocatedIndexLength<TKey, TValue>([NoAlias] this NativeParallelMultiHashMap<TKey, TValue> hashMap, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.m_MultiHashMapData.m_Buffer->allocatedIndexLength = length;
        }

        public static TKey FirstKey<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return map.m_MultiHashMapData.m_Buffer->FirstKey<TKey>();
        }

        public static bool TryGetFirstKeyValue<TKey, TValue>(
            this NativeParallelMultiHashMap<TKey, TValue> map, TKey key, out TKey storedKey, out TValue item, out NativeParallelMultiHashMapIterator<TKey> it)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckRead(map);
            return TryGetFirstKeyValueAtomic(map.m_MultiHashMapData.m_Buffer, key, out storedKey, out item, out it);
        }

        public static bool TryGetNextKeyValue<TKey, TValue>(
            this NativeParallelMultiHashMap<TKey, TValue> map, out TKey storedKey, out TValue item, ref NativeParallelMultiHashMapIterator<TKey> it)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckRead(map);
            return TryGetNextKeyValueAtomic(map.m_MultiHashMapData.m_Buffer, out storedKey, out item, ref it);
        }

        private static void GetUniqueArray<TKey, TValue>(UnsafeParallelMultiHashMap<TKey, TValue> container, NativeList<TKey> keys)
            where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
            where TValue : unmanaged
        {
            UnsafeParallelHashMapData.GetKeyArray(container.m_Buffer, keys.AsArray());

            keys.Sort();
            var uniques = keys.AsArray().Unique();
            keys.ResizeUninitialized(uniques);
        }

        private static bool TryGetFirstKeyValueAtomic<TKey, TValue>(
            UnsafeParallelHashMapData* data, TKey key, out TKey storedKey, out TValue item, out NativeParallelMultiHashMapIterator<TKey> it)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            it.key = key;

            if (data->allocatedIndexLength <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                storedKey = default;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            var buckets = (int*)data->buckets;
            var bucket = key.GetHashCode() & data->bucketCapacityMask;
            it.EntryIndex = it.NextEntryIndex = buckets[bucket];
            return TryGetNextKeyValueAtomic(data, out storedKey, out item, ref it);
        }

        private static bool TryGetNextKeyValueAtomic<TKey, TValue>(
            UnsafeParallelHashMapData* data, out TKey storedKey, out TValue item, ref NativeParallelMultiHashMapIterator<TKey> it)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;
            storedKey = default;
            item = default;
            if (entryIdx < 0 || entryIdx >= data->keyCapacity)
            {
                return false;
            }

            var nextPtrs = (int*)data->next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(it.key))
            {
                entryIdx = nextPtrs[entryIdx];
                if (entryIdx < 0 || entryIdx >= data->keyCapacity)
                {
                    return false;
                }
            }

            it.NextEntryIndex = nextPtrs[entryIdx];
            it.EntryIndex = entryIdx;

            // Read the value
            storedKey = UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx);
            item = UnsafeUtility.ReadArrayElement<TValue>(data->values, entryIdx);

            return true;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckLengthsMatch(int keys, int values)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (keys != values)
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckRead<TKey, TValue>(NativeParallelMultiHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(map.m_Safety);
#endif
        }
    }
}
