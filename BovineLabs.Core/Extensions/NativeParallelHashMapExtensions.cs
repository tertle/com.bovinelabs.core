// <copyright file="NativeParallelHashMapExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeParallelHashMapExtensions
    {
        public static int Reserve<TKey, TValue>([NoAlias] this NativeParallelHashMap<TKey, TValue>.ParallelWriter hashMap, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.m_Writer.m_Buffer->ReserveParallel(length);
        }

        public static UnsafeParallelHashMapBucketData GetUnsafeBucketData<TKey, TValue>(
            [NoAlias] this NativeParallelHashMap<TKey, TValue>.ParallelWriter hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.m_Writer.m_Buffer->GetBucketData();
        }

        public static ref TValue GetOrAddRef<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> hashMap, TKey key, TValue defaultValue = default)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(hashMap.m_Safety);
#endif

            return ref hashMap.m_HashMapData.GetOrAddRef(key, defaultValue);
        }

        public static ref TValue GetOrAddRef<TKey, TValue>(
            this NativeParallelHashMap<TKey, TValue>.ParallelWriter hashMap, TKey key, TValue defaultValue = default)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(hashMap.m_Safety);
#endif

            return ref hashMap.m_Writer.GetOrAddRef(key, defaultValue);
        }

        public static ref TValue GetRef<TKey, TValue>(this NativeParallelHashMap<TKey, TValue>.ParallelWriter hashMap, TKey key)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(hashMap.m_Safety);
#endif

            return ref hashMap.m_Writer.GetRef(key);
        }

        public static void ClearLengthBuckets<TKey, TValue>([NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var data = hashMap.m_HashMapData.m_Buffer;

            UnsafeUtility.MemSet(data->buckets, 0xff, (data->bucketCapacityMask + 1) * 4);
            // UnsafeUtility.MemSet(data->next, 0xff, data->keyCapacity * 4);
            data->allocatedIndexLength = 0;
        }

        /// <summary>
        /// Clear a <see cref="NativeParallelHashMap{TKey,TValue}" /> then efficiently add a collection of keys and values to it.
        /// This is much faster than iterating and using Add.
        /// NOTE: this is not safe. It does not check for duplicates and must only be used when keys are gauranteed to be unique.
        /// </summary>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void ClearAndAddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, [NoAlias] NativeArray<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys.Length, values.Length);
            ClearAndAddBatchUnsafe(hashMap, (TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public static void ClearAndAddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, TKey[] keys, TValue[] values, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMin(keys.Length, values.Length, length);

            var handle1 = GCHandle.Alloc(keys, GCHandleType.Pinned);
            var handle2 = GCHandle.Alloc(values, GCHandleType.Pinned);

            ClearAndAddBatchUnsafe(hashMap, (TKey*)handle1.AddrOfPinnedObject(), (TValue*)handle2.AddrOfPinnedObject(), length);

            handle1.Free();
            handle2.Free();
        }

        /// <summary>
        /// Clear a <see cref="NativeParallelHashMap{TKey,TValue}" /> then efficiently add a collection of keys and values to it.
        /// This is much faster than iterating and using Add.
        /// NOTE: this is not safe. It does not check for duplicates and must only be used when keys are gauranteed to be unique.
        /// </summary>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <param name="length"> The length of the buffers. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void ClearAndAddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, [NoAlias] TKey* keys, [NoAlias] TValue* values, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            hashMap.m_HashMapData.ClearAndAddBatchUnsafe(keys, values, length);
        }

        /// <summary>
        /// Clear a <see cref="NativeParallelHashMap{TKey,TValue}" /> then efficiently add a collection of keys and values to it.
        /// This is much faster than iterating and using Add.
        /// NOTE: this is not safe. It does not check for duplicates and must only be used when keys are gauranteed to be unique.
        /// </summary>
        /// <param name="hashMap"> The hashmap to clear and add to. </param>
        /// <param name="keys"> Collection of keys, the length should match the length of values. </param>
        /// <param name="values"> Collection of values, the length should match the length of keys. </param>
        /// <param name="length"> The length of the buffers. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public static void ClearAndAddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, [NoAlias] NativeSlice<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif
            hashMap.m_HashMapData.ClearAndAddBatchUnsafe(keys, values);
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, [NoAlias] NativeArray<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys.Length, values.Length);
            AddBatchUnsafe(hashMap, (TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, [NoAlias] TKey* keys, [NoAlias] TValue* values, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var oldLength = hashMap.m_HashMapData.m_Buffer->allocatedIndexLength;
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

            hashMap.m_HashMapData.m_Buffer->allocatedIndexLength += length;
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, [NoAlias] NativeSlice<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys.Length, values.Length);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var length = keys.Length;
            var oldLength = hashMap.m_HashMapData.m_Buffer->allocatedIndexLength;
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
                var bucket = keys[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_HashMapData.m_Buffer->allocatedIndexLength += length;
        }

        public static void AddBatchUnsafe<TKey, TValue>([NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, [NoAlias] NativeArray<TKey> keys)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            AddBatchUnsafe(hashMap, (TKey*)keys.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public static void AddBatchUnsafe<TKey, TValue>([NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, [NoAlias] TKey* keys, int length)
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

            var keyPtr = (TKey*)data.keys + oldLength;

            UnsafeUtility.MemCpy(keyPtr, keys, length * UnsafeUtility.SizeOf<TKey>());

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next + oldLength;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            hashMap.m_HashMapData.m_Buffer->allocatedIndexLength += length;
        }

        public static void ClearAndAddKeyBatchUnsafe<TKey, TValue>([NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, [NoAlias] NativeArray<TKey> keys)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            ClearAndAddKeyBatchUnsafe(hashMap, (TKey*)keys.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public static void ClearAndAddKeyBatchUnsafe<TKey, TValue>([NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, [NoAlias] TKey* keys, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            hashMap.Clear();

            if (hashMap.Capacity < length)
            {
                hashMap.Capacity = length;
            }

            var data = hashMap.GetUnsafeBucketData();
            UnsafeUtility.MemCpy(data.keys, keys, length * UnsafeUtility.SizeOf<TKey>());

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & data.bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }

            hashMap.m_HashMapData.m_Buffer->allocatedIndexLength = length;
        }

        /// <summary>
        /// Add a collection of keys and values to a hashmap in parallel.
        /// All keys added this way must be UNIQUE as it is not checked. Hashmap must not have had any elements removed.
        /// </summary>
        /// <param name="hashMap"> </param>
        /// <param name="keys"> </param>
        /// <param name="values"> </param>
        /// <typeparam name="TKey"> </typeparam>
        /// <typeparam name="TValue"> </typeparam>
        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelHashMap<TKey, TValue>.ParallelWriter hashMap, [NoAlias] NativeArray<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys.Length, values.Length);
            AddBatchUnsafe(hashMap, (TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this NativeParallelHashMap<TKey, TValue>.ParallelWriter hashMap, [NoAlias] TKey* keys, [NoAlias] TValue* values, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.m_Writer.m_Buffer->AddBatchUnsafeParallel(keys, values, length);
        }

        public static void RecalculateBuckets<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(hashMap.m_Safety);
#endif

            var length = hashMap.m_HashMapData.m_Buffer->allocatedIndexLength;

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

        public static void SetLengthUnsafe<TKey, TValue>([NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.m_HashMapData.m_Buffer->allocatedIndexLength = length;
        }

        public static int GetLengthUnsafe<TKey, TValue>([NoAlias] this NativeParallelHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.m_HashMapData.m_Buffer->allocatedIndexLength;
        }

        public static ref TValue GetValueByRef<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map, TKey key)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref map.m_HashMapData.m_Buffer->GetValueByRef<TKey, TValue>(key);
        }

        public static TKey FirstKey<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return map.m_HashMapData.m_Buffer->FirstKey<TKey>();
        }

        public static bool TryGetFirstKeyValue<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map, out TKey key, out TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var startIndex = 0;
            return map.m_HashMapData.m_Buffer->TryGetFirstKeyValue<TKey, TValue>(out key, out value, ref startIndex);
        }

        public static bool TryGetFirstKeyValue<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map, out TKey key, out TValue value, ref int index)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return map.m_HashMapData.TryGetFirstKeyValue(out key, out value, ref index);
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
        private static void CheckLengthsMin(int keys, int values, int min)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (keys < min || values < min)
            {
                throw new ArgumentException("Key or value array isn't large enough");
            }
#endif
        }
    }
}
