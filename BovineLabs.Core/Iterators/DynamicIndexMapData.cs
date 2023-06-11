// <copyright file="DynamicIndexMapData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;

    internal unsafe struct DynamicIndexMapData
    {
        internal int ValuesOffset;
        internal int NextOffset;
        internal int BucketsOffset;
        internal int ValueCapacity;
        internal int BucketLength;
        internal int AllocatedIndexLength;
        internal int FirstFreeIDX;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte* GetValues(DynamicIndexMapData* data) => (byte*)data + data->ValuesOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int* GetBuckets(DynamicIndexMapData* data) => (int*)(data + data->BucketsOffset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int* GetNexts(DynamicIndexMapData* data) => (int*)(data + data->NextOffset);

        internal static int GrowCapacity(int capacity)
        {
            if (capacity == 0)
            {
                return 1;
            }

            return capacity * 2;
        }

        internal static void AllocateIndexMap<TValue>(
            DynamicBuffer<byte> buffer,
            int length,
            int bucketLength)
            where TValue : unmanaged
        {
            var hashMapDataSize = UnsafeUtility.SizeOf<DynamicHashMapData>();

            var totalSize = CalculateDataSize<TValue>(length, bucketLength, out var nextOffset, out var bucketOffset);

            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            var data = buffer.AsIndexData<TValue>();

            data->ValueCapacity = length;
            data->BucketLength = bucketLength;

            data->ValuesOffset = hashMapDataSize;
            data->NextOffset = hashMapDataSize + nextOffset;
            data->BucketsOffset = hashMapDataSize + bucketOffset;
        }

        internal static void ReallocateHashMap<TValue>(DynamicBuffer<byte> buffer, int newCapacity, out DynamicIndexMapData* data)
            where TValue : unmanaged
        {
            data = buffer.AsIndexData<TValue>();

            if (data->ValueCapacity == newCapacity)
            {
                return;
            }

            CheckHashMapReallocateDoesNotShrink(data, newCapacity);

            var hashMapDataSize = UnsafeUtility.SizeOf<DynamicHashMapData>();
            int totalSize = CalculateDataSize<TValue>(newCapacity, data->BucketLength, out var nextOffset, out var bucketOffset);

            var oldValue = new NativeArray<TValue>(data->ValueCapacity, Allocator.Temp);
            var oldNext = new NativeArray<int>(data->ValueCapacity, Allocator.Temp);
            var oldBuckets = new NativeArray<int>(data->BucketLength, Allocator.Temp);

            UnsafeUtility.MemCpy(oldValue.GetUnsafePtr(), GetValues(data), data->ValueCapacity * UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpy(oldNext.GetUnsafePtr(), GetNexts(data), data->ValueCapacity * UnsafeUtility.SizeOf<int>());
            UnsafeUtility.MemCpy(oldBuckets.GetUnsafePtr(), GetBuckets(data), data->BucketLength * UnsafeUtility.SizeOf<int>());

            var oldAllocatedIndexLength = data->AllocatedIndexLength;
            var oldBucketLength = data->BucketLength;

            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            data = buffer.AsIndexData<TValue>();
            var ptr = (byte*)data;

            byte* newValues = ptr + hashMapDataSize;
            byte* newNext = newValues + nextOffset;
            byte* newBuckets = newValues + bucketOffset;

            UnsafeUtility.MemCpy(newValues, oldValue.GetUnsafePtr(), oldValue.Length * UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpy(newNext, oldNext.GetUnsafePtr(), oldNext.Length * UnsafeUtility.SizeOf<int>());
            UnsafeUtility.MemCpy(newBuckets, oldBuckets.GetUnsafePtr(), oldBuckets.Length * UnsafeUtility.SizeOf<int>());

            // TODO
            // UnsafeUtility.MemSet(newNext, 0xff, data->ValueCapacity * 4);
            for (int emptyNext = oldValue.Length; emptyNext < newCapacity; ++emptyNext)
            {
                ((int*)newNext)[emptyNext] = -1;
            }

            if (oldAllocatedIndexLength > oldValue.Length)
            {
                data->AllocatedIndexLength = oldValue.Length;
            }
            else
            {
                data->AllocatedIndexLength = oldAllocatedIndexLength;
            }

            data->ValuesOffset = hashMapDataSize;
            data->NextOffset = hashMapDataSize + nextOffset;
            data->BucketsOffset = hashMapDataSize + bucketOffset;
            data->ValueCapacity = newCapacity;
            data->BucketLength = oldBucketLength;
        }

        private static int CalculateDataSize<TValue>(int length, int bucketLength, out int nextOffset, out int bucketOffset)
            where TValue : unmanaged
        {
            var sizeOfTValue = UnsafeUtility.SizeOf<TValue>();
            var sizeOfInt = UnsafeUtility.SizeOf<int>();

            var valuesSize = CollectionHelper.Align(sizeOfTValue * length, JobsUtility.CacheLineSize);
            var nextSize = CollectionHelper.Align(sizeOfInt * length, JobsUtility.CacheLineSize);
            var bucketSize = CollectionHelper.Align(sizeOfInt * bucketLength, JobsUtility.CacheLineSize);
            var totalSize = valuesSize + nextSize + bucketSize;

            nextOffset = 0 + valuesSize;
            bucketOffset = nextOffset + nextSize;

            return totalSize;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckHashMapReallocateDoesNotShrink(DynamicIndexMapData* data, int newCapacity)
        {
            if (data->ValueCapacity > newCapacity)
            {
                throw new Exception("Shrinking a index is not supported");
            }
        }
    }
}
