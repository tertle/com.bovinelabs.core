// <copyright file="UnsafeParallelHashMapDataExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Unity.Burst;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class UnsafeParallelHashMapDataExtensions
    {
        internal static void AddBatchUnsafe<TKey, TValue>(
            [NoAlias] this ref UnsafeParallelHashMapData data,
            [NoAlias] TKey* keys,
            [NoAlias] TValue* values,
            int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var newLength = Interlocked.Add(ref data.allocatedIndexLength, length);
            var oldLength = newLength - length;

            var keyPtr = (TKey*)data.keys + oldLength;
            var valuePtr = (TValue*)data.values + oldLength;

            UnsafeUtility.MemCpy(keyPtr, keys, length * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(valuePtr, values, length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next + oldLength;

            for (var idx = 0; idx < length; idx++)
            {
                var hash = keys[idx].GetHashCode() & data.bucketCapacityMask;
                var index = oldLength + idx;
                var next = Interlocked.Exchange(ref UnsafeUtility.ArrayElementAsRef<int>(buckets, hash), index);
                nextPtrs[idx] = next;
            }
        }

        internal static void AddBatchUnsafe<TKey>(
            [NoAlias] this ref UnsafeParallelHashMapData data,
            [NoAlias] TKey* keys,
            int length)
            where TKey : unmanaged, IEquatable<TKey>
        {
            var newLength = Interlocked.Add(ref data.allocatedIndexLength, length);
            var oldLength = newLength - length;

            var keyPtr = (TKey*)data.keys + oldLength;

            UnsafeUtility.MemCpy(keyPtr, keys, length * UnsafeUtility.SizeOf<TKey>());

            var buckets = (int*)data.buckets;
            var nextPtrs = (int*)data.next + oldLength;

            for (var idx = 0; idx < length; idx++)
            {
                var hash = keys[idx].GetHashCode() & data.bucketCapacityMask;
                var index = oldLength + idx;
                var next = Interlocked.Exchange(ref UnsafeUtility.ArrayElementAsRef<int>(buckets, hash), index);
                nextPtrs[idx] = next;
            }
        }

        internal static ref TValue GetValueByRef<TKey, TValue>(this ref UnsafeParallelHashMapData data, TKey key)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (data.allocatedIndexLength <= 0)
            {
                throw new KeyNotFoundException();
            }
#endif

            // First find the slot based on the hash
            var buckets = (int*)data.buckets;
            var bucket = key.GetHashCode() & data.bucketCapacityMask;
            var entryIdx = buckets[bucket];

            var nextPtrs = (int*)data.next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(data.keys, entryIdx).Equals(key))
            {
                entryIdx = nextPtrs[entryIdx];
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((entryIdx < 0) || (entryIdx >= data.keyCapacity))
                {
                    throw new KeyNotFoundException();
                }
#endif
            }

            // Read the value
            return ref UnsafeUtility.ArrayElementAsRef<TValue>(data.values, entryIdx);
        }

        internal static TKey FirstKey<TKey>(this UnsafeParallelHashMapData data)
            where TKey : struct, IEquatable<TKey>
        {
            var length = data.bucketCapacityMask + 1;
            var buckets = (int*)data.buckets;

            for (var i = 0; i < length; i++)
            {
                var entryIndex = buckets[i];

                if (entryIndex != -1)
                {
                    return UnsafeUtility.ReadArrayElement<TKey>(data.keys, entryIndex);
                }
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            throw new InvalidOperationException("The source sequence is empty.");
#else
            return default;
#endif
        }

        internal static bool TryGetFirstKey<TKey>(this UnsafeParallelHashMapData data, out TKey key, ref int startIndex)
            where TKey : struct, IEquatable<TKey>
        {
            var length = data.bucketCapacityMask + 1;
            var buckets = (int*)data.buckets;

            for (; startIndex < length; startIndex++)
            {
                var entryIndex = buckets[startIndex];

                if (entryIndex != -1)
                {
                    key = UnsafeUtility.ReadArrayElement<TKey>(data.keys, entryIndex);
                    return true;
                }
            }

            key = default;
            return false;
        }

        internal static bool TryGetFirstKeyValue<TKey, TValue>(this UnsafeParallelHashMapData data, out TKey key, out TValue value, ref int startIndex)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var length = data.bucketCapacityMask + 1;
            var buckets = (int*)data.buckets;

            for (; startIndex < length; startIndex++)
            {
                var entryIndex = buckets[startIndex];

                if (entryIndex != -1)
                {
                    key = UnsafeUtility.ReadArrayElement<TKey>(data.keys, entryIndex);
                    value = UnsafeUtility.ReadArrayElement<TValue>(data.values, entryIndex);
                    return true;
                }
            }

            key = default;
            value = default;
            return false;
        }
    }
}
