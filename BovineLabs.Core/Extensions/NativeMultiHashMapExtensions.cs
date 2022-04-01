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
        public static unsafe void ClearAndAddBatch<TKey, TValue>([NoAlias] this NativeMultiHashMap<TKey, TValue> hashMap, [NoAlias] NativeArray<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
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
    }
}
