// <copyright file="DynamicHashSetData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    public unsafe struct DynamicHashSetData
    {
        internal int KeysOffset;
        internal int NextOffset;
        internal int BucketsOffset;
        internal int KeyCapacity;
        internal int BucketCapacityMask; // = bucket capacity - 1
        internal int AllocatedIndexLength;
        internal int FirstFreeIDX;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte* GetKeys(DynamicHashSetData* data) => (byte*)data + data->KeysOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte* GetBuckets(DynamicHashSetData* data) => (byte*)data + data->BucketsOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte* GetNexts(DynamicHashSetData* data) => (byte*)data + data->NextOffset;

        internal static int GetBucketSize(int capacity)
        {
            return capacity * 2;
        }

        internal static int GrowCapacity(int capacity)
        {
            if (capacity == 0)
            {
                return 1;
            }

            return capacity * 2;
        }

        internal static void AllocateHashSet<TKey>(DynamicBuffer<byte> buffer, int length, int bucketLength)
            where TKey : unmanaged, IEquatable<TKey>
        {
            Check.Assume(buffer.Length == 0, "Buffer already assigned");

            var hashMapDataSize = UnsafeUtility.SizeOf<DynamicHashMapData>();

            bucketLength = math.ceilpow2(bucketLength);

            var totalSize = CalculateDataSize<TKey>(length, bucketLength, out var nextOffset, out var bucketOffset);

            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            var data = buffer.AsSetData<TKey>();

            data->KeyCapacity = length;
            data->BucketCapacityMask = bucketLength - 1;

            data->KeysOffset = hashMapDataSize;
            data->NextOffset = hashMapDataSize + nextOffset;
            data->BucketsOffset = hashMapDataSize + bucketOffset;
        }

        internal static void ReallocateHashSet<TKey>(
            DynamicBuffer<byte> buffer,
            int newCapacity,
            int newBucketCapacity,
            out DynamicHashSetData* data)
            where TKey : unmanaged, IEquatable<TKey>
        {
            data = buffer.AsSetData<TKey>();

            newBucketCapacity = math.ceilpow2(newBucketCapacity);

            if (data->KeyCapacity == newCapacity && (data->BucketCapacityMask + 1) == newBucketCapacity)
            {
                return;
            }

            CheckHashMapReallocateDoesNotShrink(data, newCapacity);

            var hashMapDataSize = UnsafeUtility.SizeOf<DynamicHashMapData>();
            int totalSize = CalculateDataSize<TKey>(newCapacity, newBucketCapacity, out var nextOffset, out var bucketOffset);

            var oldKeys = new NativeArray<TKey>(data->KeyCapacity, Allocator.Temp);
            var oldNext = new NativeArray<int>(data->KeyCapacity, Allocator.Temp);
            var oldBuckets = new NativeArray<int>(data->BucketCapacityMask + 1, Allocator.Temp);

            UnsafeUtility.MemCpy(oldKeys.GetUnsafePtr(), GetKeys(data), data->KeyCapacity * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(oldNext.GetUnsafePtr(), GetNexts(data), data->KeyCapacity * UnsafeUtility.SizeOf<int>());
            UnsafeUtility.MemCpy(oldBuckets.GetUnsafePtr(), GetBuckets(data), (data->BucketCapacityMask + 1) * UnsafeUtility.SizeOf<int>());

            var oldAllocatedIndexLength = data->AllocatedIndexLength;

            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            data = buffer.AsSetData<TKey>();
            var ptr = (byte*)data;

            byte* newKeys = ptr + hashMapDataSize;
            byte* newNext = newKeys + nextOffset;
            byte* newBuckets = newKeys + bucketOffset;

            // The items are taken from a free-list and might not be tightly packed, copy all of the old capacity
            UnsafeUtility.MemCpy(newKeys, oldKeys.GetUnsafePtr(), oldKeys.Length * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(newNext, oldNext.GetUnsafePtr(), oldNext.Length * UnsafeUtility.SizeOf<int>());

            for (int emptyNext = oldNext.Length; emptyNext < newCapacity; ++emptyNext)
            {
                ((int*)newNext)[emptyNext] = -1;
            }

            // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
            for (int bucket = 0; bucket < newBucketCapacity; ++bucket)
            {
                ((int*)newBuckets)[bucket] = -1;
            }

            // TODO ?
            // UnsafeUtility.MemSet(newBuckets, 0xff, newBucketCapacity * 4);

            for (int bucket = 0; bucket <= oldBuckets.Length - 1; ++bucket)
            {
                int* nextPtrs = (int*)newNext;
                while (oldBuckets[bucket] >= 0)
                {
                    int curEntry = oldBuckets[bucket];
                    oldBuckets[bucket] = nextPtrs[curEntry];
                    int newBucket = oldKeys[curEntry].GetHashCode() & (newBucketCapacity - 1);
                    nextPtrs[curEntry] = ((int*)newBuckets)[newBucket];
                    ((int*)newBuckets)[newBucket] = curEntry;
                }
            }

            if (oldAllocatedIndexLength > oldKeys.Length)
            {
                data->AllocatedIndexLength = oldKeys.Length;
            }
            else
            {
                data->AllocatedIndexLength = oldAllocatedIndexLength;
            }

            data->KeysOffset = hashMapDataSize;
            data->NextOffset = hashMapDataSize + nextOffset;
            data->BucketsOffset = hashMapDataSize + bucketOffset;
            data->KeyCapacity = newCapacity;
            data->BucketCapacityMask = newBucketCapacity - 1;
        }

        private static int CalculateDataSize<TKey>(int length, int bucketLength, out int nextOffset, out int bucketOffset)
            where TKey : unmanaged, IEquatable<TKey>
        {
            var sizeOfTKey = UnsafeUtility.SizeOf<TKey>();
            var sizeOfInt = UnsafeUtility.SizeOf<int>();

            var keysSize = CollectionHelper.Align(sizeOfTKey * length, JobsUtility.CacheLineSize);
            var nextSize = CollectionHelper.Align(sizeOfInt * length, JobsUtility.CacheLineSize);
            var bucketSize = CollectionHelper.Align(sizeOfInt * bucketLength, JobsUtility.CacheLineSize);
            var totalSize = keysSize + nextSize + bucketSize;

            nextOffset = keysSize;
            bucketOffset = nextOffset + nextSize;

            return totalSize;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckHashMapReallocateDoesNotShrink(DynamicHashSetData* data, int newCapacity)
        {
            if (data->KeyCapacity > newCapacity)
            {
                throw new Exception("Shrinking a hash map is not supported");
            }
        }
    }
}
