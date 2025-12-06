// <copyright file="NativeHashMapExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    public static unsafe class NativeHashMapExtensions
    {
        public static ref TValue GetOrAddRef<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, TKey key, TValue defaultValue = default)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckWrite(hashMap);

            var idx = hashMap.m_Data->Find(key);

            if (idx == -1)
            {
                idx = hashMap.m_Data->AddNoFind(key);
                UnsafeUtility.WriteArrayElement(hashMap.m_Data->Ptr, idx, defaultValue);
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(hashMap.m_Data->Ptr, idx);
        }

        public static ref TValue GetRef<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, TKey key)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckWrite(hashMap);

            var idx = hashMap.m_Data->Find(key);
            Check.Assume(idx != -1);
            return ref UnsafeUtility.ArrayElementAsRef<TValue>(hashMap.m_Data->Ptr, idx);
        }

        public static ref TValue GetRefNoSafety<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, TKey key)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var idx = hashMap.m_Data->Find(key);
            Check.Assume(idx != -1);
            return ref UnsafeUtility.ArrayElementAsRef<TValue>(hashMap.m_Data->Ptr, idx);
        }

        public static bool Remove<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, TKey key, out TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckWrite(hashMap);
            var data = hashMap.m_Data;

            if (hashMap.Capacity == 0)
            {
                value = default;
                return false;
            }

            // First find the slot based on the hash
            var bucket = data->GetBucket(key);

            var prevEntry = -1;
            var entryIdx = data->Buckets[bucket];

            while (entryIdx >= 0 && entryIdx < data->Capacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(data->Keys, entryIdx).Equals(key))
                {
                    // Found matching element, remove it
                    if (prevEntry < 0)
                    {
                        data->Buckets[bucket] = data->Next[entryIdx];
                    }
                    else
                    {
                        data->Next[prevEntry] = data->Next[entryIdx];
                    }

                    value = UnsafeUtility.ReadArrayElement<TValue>(hashMap.m_Data->Ptr, entryIdx);

                    // And free the index
                    data->Next[entryIdx] = data->FirstFreeIdx;
                    data->FirstFreeIdx = entryIdx;
                    data->Count -= 1;

                    return true;
                }

                prevEntry = entryIdx;
                entryIdx = data->Next[entryIdx];
            }

            value = default;
            return false;
        }

        public static bool TryGetIndex<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, TKey key, out int index)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            index = hashMap.m_Data->Find(key);
            return index != -1;
        }

        public static TValue ReadIndexUnsafe<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, int index)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return UnsafeUtility.ReadArrayElement<TValue>(hashMap.m_Data->Ptr, index);
        }

        public static bool TryGetIndex<TKey, TValue>(this NativeHashMap<TKey, TValue>.ReadOnly hashMap, TKey key, out int index)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            index = hashMap.m_Data->Find(key);
            return index != -1;
        }

        public static TValue ReadIndexUnsafe<TKey, TValue>(this NativeHashMap<TKey, TValue>.ReadOnly hashMap, int index)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return UnsafeUtility.ReadArrayElement<TValue>(hashMap.m_Data->Ptr, index);
        }

        internal static int AddNoFind<TKey>(this ref HashMapHelper<TKey> hashMapHelper, in TKey key)
            where TKey : unmanaged, IEquatable<TKey>
        {
            // Allocate an entry from the free list
            if (hashMapHelper.AllocatedIndex >= hashMapHelper.Capacity && hashMapHelper.FirstFreeIdx < 0)
            {
                var newCap = hashMapHelper.CalcCapacityCeilPow2(hashMapHelper.Capacity + (1 << hashMapHelper.Log2MinGrowth));
                hashMapHelper.ResizeMulti(newCap);
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

        internal static int AddNoFindNoResize<TKey>(this ref HashMapHelper<TKey> hashMapHelper, in TKey key)
            where TKey : unmanaged, IEquatable<TKey>
        {
            CheckHasCapacity(ref hashMapHelper);

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

        internal static int AddLinearNoResize<TKey>(this ref HashMapHelper<TKey> hashMapHelper, in TKey key)
            where TKey : unmanaged, IEquatable<TKey>
        {
            CheckHasCapacity(ref hashMapHelper);
            CheckNoFreeIDX(ref hashMapHelper);

            var idx = hashMapHelper.AllocatedIndex++;

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

        private static void ResizeMulti<TKey>(this ref HashMapHelper<TKey> hashMapHelper, int newCapacity)
            where TKey : unmanaged, IEquatable<TKey>
        {
            newCapacity = math.max(newCapacity, hashMapHelper.Count);
            var newBucketCapacity = math.ceilpow2(HashMapHelper<TKey>.GetBucketSize(newCapacity));

            if (hashMapHelper.Capacity == newCapacity && hashMapHelper.BucketCapacity == newBucketCapacity)
            {
                return;
            }

            hashMapHelper.ResizeExactMulti(newCapacity, newBucketCapacity);
        }

        private static void ResizeExactMulti<TKey>(this ref HashMapHelper<TKey> hashMapHelper, int newCapacity, int newBucketCapacity)
            where TKey : unmanaged, IEquatable<TKey>
        {
            var totalSize = HashMapHelper<TKey>.CalculateDataSize(newCapacity, newBucketCapacity, hashMapHelper.SizeOfTValue, out var keyOffset,
                out var nextOffset, out var bucketOffset);

            var oldPtr = hashMapHelper.Ptr;
            var oldKeys = hashMapHelper.Keys;
            var oldNext = hashMapHelper.Next;
            var oldBuckets = hashMapHelper.Buckets;
            var oldBucketCapacity = hashMapHelper.BucketCapacity;

            hashMapHelper.Ptr = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, hashMapHelper.Allocator);
            hashMapHelper.Keys = (TKey*)(hashMapHelper.Ptr + keyOffset);
            hashMapHelper.Next = (int*)(hashMapHelper.Ptr + nextOffset);
            hashMapHelper.Buckets = (int*)(hashMapHelper.Ptr + bucketOffset);
            hashMapHelper.Capacity = newCapacity;
            hashMapHelper.BucketCapacity = newBucketCapacity;

            hashMapHelper.Clear();

            for (int i = 0, num = oldBucketCapacity; i < num; ++i)
            {
                for (var idx = oldBuckets[i]; idx != -1; idx = oldNext[idx])
                {
                    var newIdx = AddNoFindNoResize(ref hashMapHelper, oldKeys[idx]);
                    UnsafeUtility.MemCpy(hashMapHelper.Ptr + (hashMapHelper.SizeOfTValue * newIdx), oldPtr + (hashMapHelper.SizeOfTValue * idx),
                        hashMapHelper.SizeOfTValue);
                }
            }

            Memory.Unmanaged.Free(oldPtr, hashMapHelper.Allocator);
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckDoesNotExist<TKey>(this ref HashMapHelper<TKey> hashMapHelper, in TKey key)
            where TKey : unmanaged, IEquatable<TKey>
        {
            if (hashMapHelper.Find(key) != -1)
            {
                throw new InvalidOperationException($"Key already exists {key}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckHasCapacity<TKey>(this ref HashMapHelper<TKey> hashMapHelper)
            where TKey : unmanaged, IEquatable<TKey>
        {
            // Allocate an entry from the free list
            if (hashMapHelper.AllocatedIndex >= hashMapHelper.Capacity && hashMapHelper.FirstFreeIdx < 0)
            {
                throw new InvalidOperationException("Capacity reached");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckNoFreeIDX<TKey>(this ref HashMapHelper<TKey> hashMapHelper)
            where TKey : unmanaged, IEquatable<TKey>
        {
            if (hashMapHelper.FirstFreeIdx >= 0)
            {
                throw new InvalidOperationException("No free idx allowed");
            }
        }
    }
}
