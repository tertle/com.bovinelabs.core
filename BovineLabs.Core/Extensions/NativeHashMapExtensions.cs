// <copyright file="NativeHashMapExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeHashMapExtensions
    {
        public static ref TValue GetOrAddRef<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, TKey key)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckWrite(hashMap);

            var idx = hashMap.m_Data->Find(key);

            if (idx == -1)
            {
                idx = hashMap.m_Data->AddNoFind(key);
                UnsafeUtility.WriteArrayElement(hashMap.m_Data->Ptr, idx, default(TValue));
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(hashMap.m_Data->Ptr, idx);
        }

        internal static int AddNoFind<TKey>(this ref HashMapHelper<TKey> hashMapHelper, in TKey key)
            where TKey : unmanaged, IEquatable<TKey>
        {
            // Allocate an entry from the free list
            if (hashMapHelper.AllocatedIndex >= hashMapHelper.Capacity && hashMapHelper.FirstFreeIdx < 0)
            {
                int newCap = hashMapHelper.CalcCapacityCeilPow2(hashMapHelper.Capacity + (1 << hashMapHelper.Log2MinGrowth));
                hashMapHelper.Resize(newCap);
            }

            var idx = hashMapHelper.FirstFreeIdx;

            if (idx >= 0)
            {
                hashMapHelper.FirstFreeIdx = hashMapHelper.Next[idx];
            }
            else
            {
                idx = hashMapHelper.AllocatedIndex++;
            }

            CheckIndexOutOfBounds(idx, hashMapHelper.Capacity);

            UnsafeUtility.WriteArrayElement(hashMapHelper.Keys, idx, key);
            var bucket = hashMapHelper.GetBucket(key);

            // Add the index to the hash-map
            var next = hashMapHelper.Next;
            next[idx] = hashMapHelper.Buckets[bucket];
            hashMapHelper.Buckets[bucket] = idx;
            hashMapHelper.Count++;

            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetBucket<TKey>(this in HashMapHelper<TKey> hashMapHelper, in TKey key)
            where TKey : unmanaged, IEquatable<TKey>
        {
            return (int)((uint)key.GetHashCode() & (hashMapHelper.BucketCapacity - 1));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckWrite<TKey, TValue>(NativeHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(hashMap.m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckIndexOutOfBounds(int idx, int capacity)
        {
            if ((uint)idx >= (uint)capacity)
            {
                throw new InvalidOperationException($"Internal HashMap error. idx {idx}");
            }
        }
    }
}
