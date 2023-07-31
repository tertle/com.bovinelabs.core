// <copyright file="HashMapHelperExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Unity.Collections.LowLevel.Unsafe;

    internal static unsafe class HashMapHelperInternals
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearLengthBuckets<TKey>(ref this HashMapHelper<TKey> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
        {
            UnsafeUtility.MemSet(hashMap.Buckets, 0xff, hashMap.BucketCapacity * sizeof(int));

            hashMap.Count = 0;
            hashMap.FirstFreeIdx = -1;
            hashMap.AllocatedIndex = 0;
        }

        public static void RecalculateBuckets<TKey>(this HashMapHelper<TKey> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
        {
            var length = hashMap.AllocatedIndex;

            var buckets = hashMap.Buckets;
            var nextPtrs = hashMap.Next;
            var keys = hashMap.Keys;

            var bucketCapacityMask = hashMap.BucketCapacity - 1;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = (int)((uint)keys[idx].GetHashCode() & bucketCapacityMask);
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReserveAtomicNoResize<TKey>(this ref HashMapHelper<TKey> hashMap, int length)
            where TKey : unmanaged, IEquatable<TKey>
        {
            var newLength = Interlocked.Add(ref hashMap.AllocatedIndex, length);
            return newLength - length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCount<TKey>(this ref HashMapHelper<TKey> hashMap, int count)
            where TKey : unmanaged, IEquatable<TKey>
        {
            hashMap.Count = count;
        }
    }
}
