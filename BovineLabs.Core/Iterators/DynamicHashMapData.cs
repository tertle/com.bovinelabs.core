// <copyright file="DynamicHashMapData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;
    using Debug = UnityEngine.Debug;

    [StructLayout(LayoutKind.Explicit)]
    [BurstCompatible]
    internal unsafe struct DynamicHashMapData
    {
        [FieldOffset(0)]
        internal byte* Values;

        // 4-byte padding on 32-bit architectures here
        [FieldOffset(8)]
        internal byte* Keys;

        // 4-byte padding on 32-bit architectures here
        [FieldOffset(16)]
        internal byte* Next;

        // 4-byte padding on 32-bit architectures here
        [FieldOffset(24)]
        internal byte* Buckets;

        // 4-byte padding on 32-bit architectures here
        [FieldOffset(32)]
        internal int KeyCapacity;

        [FieldOffset(36)]
        internal int BucketCapacityMask; // = bucket capacity - 1

        [FieldOffset(40)]
        internal int AllocatedIndexLength;

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

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int), typeof(int) })]
        internal static void AllocateHashMap<TBuffer, TKey, TValue>(
            DynamicBuffer<TBuffer> buffer,
            int length,
            int bucketLength,
            out DynamicHashMapData* outBuf)
            where TBuffer : struct, IDynamicHashMap<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            Debug.Assert(buffer.Length == 0, "Buffer already assigned");

            IsBlittableAndThrow<TKey, TValue>();

            var hashMapDataSize = UnsafeUtility.SizeOf<DynamicHashMapData>();

            bucketLength = math.ceilpow2(bucketLength);

            int totalSize = CalculateDataSize<TKey, TValue>(length, bucketLength, out var keyOffset, out var nextOffset, out var bucketOffset);

            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            var data = buffer.AsData<TBuffer, TKey, TValue>();
            var ptr = (byte*)data;

            data->KeyCapacity = length;
            data->BucketCapacityMask = bucketLength - 1;

            data->Values = ptr + hashMapDataSize;
            data->Keys = data->Values + keyOffset;
            data->Next = data->Values + nextOffset;
            data->Buckets = data->Values + bucketOffset;

            outBuf = data;
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int), typeof(int) })]
        internal static void ReallocateHashMap<TBuffer, TKey, TValue>(
            DynamicBuffer<TBuffer> buffer,
            int newCapacity,
            int newBucketCapacity,
            out DynamicHashMapData* data)
            where TBuffer : struct, IDynamicHashMap<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            data = buffer.AsData<TBuffer, TKey, TValue>();

            newBucketCapacity = math.ceilpow2(newBucketCapacity);

            if (data->KeyCapacity == newCapacity && (data->BucketCapacityMask + 1) == newBucketCapacity)
            {
                return;
            }

            CheckHashMapReallocateDoesNotShrink(data, newCapacity);

            var hashMapDataSize = UnsafeUtility.SizeOf<DynamicHashMapData>();
            int totalSize = CalculateDataSize<TKey, TValue>(newCapacity, newBucketCapacity, out var keyOffset, out var nextOffset, out var bucketOffset);

            var oldValue = new NativeArray<TValue>(data->KeyCapacity, Allocator.Temp);
            var oldKeys = new NativeArray<TKey>(data->KeyCapacity, Allocator.Temp);
            var oldNext = new NativeArray<int>(data->KeyCapacity, Allocator.Temp);
            var oldBuckets = new NativeArray<int>(data->BucketCapacityMask + 1, Allocator.Temp);

            UnsafeUtility.MemCpy(oldValue.GetUnsafePtr(), data->Values, data->KeyCapacity * UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpy(oldKeys.GetUnsafePtr(), data->Keys, data->KeyCapacity * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(oldNext.GetUnsafePtr(), data->Next, data->KeyCapacity * UnsafeUtility.SizeOf<int>());
            UnsafeUtility.MemCpy(oldBuckets.GetUnsafePtr(), data->Buckets, (data->BucketCapacityMask + 1) * UnsafeUtility.SizeOf<int>());

            var oldAllocatedIndexLength = data->AllocatedIndexLength;

            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            data = buffer.AsData<TBuffer, TKey, TValue>();
            var ptr = (byte*)data;

            byte* newValues = ptr + hashMapDataSize;
            byte* newKeys = newValues + keyOffset;
            byte* newNext = newValues + nextOffset;
            byte* newBuckets = newValues + bucketOffset;

            // The items are taken from a free-list and might not be tightly packed, copy all of the old capacity
            UnsafeUtility.MemCpy(newValues, oldValue.GetUnsafePtr(), oldValue.Length * UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpy(newKeys, oldKeys.GetUnsafePtr(), oldKeys.Length * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(newNext, oldNext.GetUnsafePtr(), oldNext.Length * UnsafeUtility.SizeOf<int>());

            for (int emptyNext = oldValue.Length; emptyNext < newCapacity; ++emptyNext)
            {
                ((int*)newNext)[emptyNext] = -1;
            }

            // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
            for (int bucket = 0; bucket < newBucketCapacity; ++bucket)
            {
                ((int*)newBuckets)[bucket] = -1;
            }

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

            data->Values = newValues;
            data->Keys = newKeys;
            data->Next = newNext;
            data->Buckets = newBuckets;
            data->KeyCapacity = newCapacity;
            data->BucketCapacityMask = newBucketCapacity - 1;
        }

        internal static bool IsEmpty(DynamicHashMapData* data)
        {
            if (data->AllocatedIndexLength <= 0)
            {
                return true;
            }

            var bucketArray = (int*)data->Buckets;
            var capacityMask = data->BucketCapacityMask;

            for (int i = 0; i <= capacityMask; ++i)
            {
                int bucket = bucketArray[i];

                if (bucket != -1)
                {
                    return false;
                }
            }

            return true;
        }

        internal static int GetCount(DynamicHashMapData* data)
        {
            if (data->AllocatedIndexLength <= 0)
            {
                return 0;
            }

            return math.min(data->KeyCapacity, data->AllocatedIndexLength);
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        internal static void GetKeyArray<TKey>(DynamicHashMapData* data, NativeArray<TKey> result)
            where TKey : struct
        {
            var bucketArray = (int*)data->Buckets;
            var bucketNext = (int*)data->Next;

            for (int i = 0, count = 0, max = result.Length; i <= data->BucketCapacityMask && count < max; ++i)
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TKey>(data->Keys, bucket);
                    bucket = bucketNext[bucket];
                }
            }
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        internal static void GetValueArray<TValue>(DynamicHashMapData* data, NativeArray<TValue> result)
            where TValue : struct
        {
            var bucketArray = (int*)data->Buckets;
            var bucketNext = (int*)data->Next;

            for (int i = 0, count = 0, max = result.Length, capacityMask = data->BucketCapacityMask; i <= capacityMask && count < max; ++i)
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TValue>(data->Values, bucket);
                    bucket = bucketNext[bucket];
                }
            }
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        internal static void GetKeyValueArrays<TKey, TValue>(DynamicHashMapData* data, NativeKeyValueArrays<TKey, TValue> result)
            where TKey : struct
            where TValue : struct
        {
            var bucketArray = (int*)data->Buckets;
            var bucketNext = (int*)data->Next;

            for (int i = 0, count = 0, max = result.Length, capacityMask = data->BucketCapacityMask; i <= capacityMask && count < max; ++i)
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    result.Keys[count] = UnsafeUtility.ReadArrayElement<TKey>(data->Keys, bucket);
                    result.Values[count] = UnsafeUtility.ReadArrayElement<TValue>(data->Values, bucket);
                    count++;
                    bucket = bucketNext[bucket];
                }
            }
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        private static int CalculateDataSize<TKey, TValue>(int length, int bucketLength, out int keyOffset, out int nextOffset, out int bucketOffset)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var sizeOfTValue = UnsafeUtility.SizeOf<TValue>();
            var sizeOfTKey = UnsafeUtility.SizeOf<TKey>();
            var sizeOfInt = UnsafeUtility.SizeOf<int>();

            var valuesSize = CollectionHelper.Align(sizeOfTValue * length, JobsUtility.CacheLineSize);
            var keysSize = CollectionHelper.Align(sizeOfTKey * length, JobsUtility.CacheLineSize);
            var nextSize = CollectionHelper.Align(sizeOfInt * length, JobsUtility.CacheLineSize);
            var bucketSize = CollectionHelper.Align(sizeOfInt * bucketLength, JobsUtility.CacheLineSize);
            var totalSize = valuesSize + keysSize + nextSize + bucketSize;

            keyOffset = 0 + valuesSize;
            nextOffset = keyOffset + keysSize;
            bucketOffset = nextOffset + nextSize;

            return totalSize;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [NotBurstCompatible]
        private static void IsBlittableAndThrow<TKey, TValue>()
        {
            CollectionHelper.CheckIsUnmanaged<TKey>();
            CollectionHelper.CheckIsUnmanaged<TValue>();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckHashMapReallocateDoesNotShrink(DynamicHashMapData* data, int newCapacity)
        {
            if (data->KeyCapacity > newCapacity)
            {
                throw new Exception("Shrinking a hash map is not supported");
            }
        }
    }
}
