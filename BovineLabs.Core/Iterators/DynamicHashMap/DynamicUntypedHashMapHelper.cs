// <copyright file="DynamicUntypedHashMapHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Assertions;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DynamicUntypedHashMapHelper<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        internal int ValuesOffset;
        internal int KeysOffset;
        internal int NextOffset;
        internal int BucketsOffset;
        internal int DataOffset;
        internal int Count;
        internal int Capacity;
        internal int DataCapacity;
        internal int BucketCapacityMask; // = bucket capacity - 1
        internal int Log2MinGrowth;
        internal int DataAllocatedIndex;

        internal int BucketCapacity => this.BucketCapacityMask + 1;

        internal byte* Values
        {
            get
            {
                fixed (DynamicUntypedHashMapHelper<TKey>* data = &this)
                {
                    return (byte*)data + data->ValuesOffset;
                }
            }
        }

        internal TKey* Keys
        {
            get
            {
                fixed (DynamicUntypedHashMapHelper<TKey>* data = &this)
                {
                    return (TKey*)((byte*)data + data->KeysOffset);
                }
            }
        }

        internal int* Next
        {
            get
            {
                fixed (DynamicUntypedHashMapHelper<TKey>* data = &this)
                {
                    return (int*)((byte*)data + data->NextOffset);
                }
            }
        }

        internal int* Buckets
        {
            get
            {
                fixed (DynamicUntypedHashMapHelper<TKey>* data = &this)
                {
                    return (int*)((byte*)data + data->BucketsOffset);
                }
            }
        }

        internal int* Data
        {
            get
            {
                fixed (DynamicUntypedHashMapHelper<TKey>* data = &this)
                {
                    return (int*)((byte*)data + data->DataOffset);
                }
            }
        }

        internal readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Count == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalcCapacityCeilPow2(int count, int capacity, int log2MinGrowth)
        {
            capacity = math.max(math.max(1, count), capacity);
            var newCapacity = math.max(capacity, 1 << log2MinGrowth);
            var result = math.ceilpow2(newCapacity);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalcCapacityCeilPow2(int capacity, int log2MinGrowth)
        {
            var newCapacity = math.max(capacity, 1 << log2MinGrowth);
            var result = math.ceilpow2(newCapacity);

            return result;
        }

        private static int GetBucketSize(int capacity)
        {
            return capacity * 2;
        }

        internal static void Init(DynamicBuffer<byte> buffer, int capacity, int dataCapacity, int minGrowth)
        {
            Check.Assume(buffer.Length == 0, "Buffer already assigned");

            var log2MinGrowth = (byte)(32 - math.lzcnt(math.max(1, minGrowth) - 1));
            capacity = CalcCapacityCeilPow2(0, capacity, log2MinGrowth);
            dataCapacity = CalcCapacityCeilPow2(0, dataCapacity, log2MinGrowth);

            var bucketCapacity = GetBucketSize(capacity);
            var totalSize = CalculateDataSize(
                capacity, bucketCapacity, dataCapacity, out var keyOffset, out var nextOffset, out var bucketOffset, out var dataOffset);

            var hashMapDataSize = sizeof(DynamicUntypedHashMapHelper<TKey>);
            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            var data = buffer.AsUntypedHelper<TKey>();

            data->Count = 0;
            data->Log2MinGrowth = log2MinGrowth;
            data->Capacity = capacity;
            data->DataCapacity = dataCapacity;
            data->BucketCapacityMask = bucketCapacity - 1;
            data->DataAllocatedIndex = 0;

            data->ValuesOffset = hashMapDataSize;
            data->KeysOffset = hashMapDataSize + keyOffset;
            data->NextOffset = hashMapDataSize + nextOffset;
            data->BucketsOffset = hashMapDataSize + bucketOffset;
            data->DataOffset = hashMapDataSize + dataOffset;

            UnsafeUtility.MemSet(data->Buckets, 0xff, data->BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet(data->Next, 0xff, data->Capacity * sizeof(int));
        }

        internal static void Resize(DynamicBuffer<byte> buffer, ref DynamicUntypedHashMapHelper<TKey>* data, int newCapacity)
        {
            // This hashmap doesn't allow shrinking
            if (newCapacity < data->Capacity)
            {
                return;
            }

            var newBucketCapacity = math.ceilpow2(GetBucketSize(newCapacity));

            Resize(buffer, ref data, newCapacity, newBucketCapacity);
        }

        internal static void Resize(DynamicBuffer<byte> buffer, ref DynamicUntypedHashMapHelper<TKey>* data, int newCapacity, int newBucketCapacity)
        {
            Assert.IsTrue(newCapacity > data->Capacity);

            var totalSize = CalculateDataSize(
                newCapacity, newBucketCapacity, data->DataCapacity, out var keyOffset, out var nextOffset, out var bucketOffset, out var newDataOffset);

            var oldValue = (byte*)UnsafeUtility.Malloc(data->Capacity * sizeof(int), UnsafeUtility.AlignOf<byte>(), Allocator.Temp);
            var oldKeys = (TKey*)UnsafeUtility.Malloc(data->Capacity * sizeof(TKey), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var oldNext = (int*)UnsafeUtility.Malloc(data->Capacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var oldBuckets = (int*)UnsafeUtility.Malloc(data->BucketCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var oldData = (int*)UnsafeUtility.Malloc(data->DataCapacity, UnsafeUtility.AlignOf<int>(), Allocator.Temp);

            UnsafeUtility.MemCpy(oldValue, data->Values, data->Capacity * sizeof(int));
            UnsafeUtility.MemCpy(oldKeys, data->Keys, data->Capacity * sizeof(TKey));
            UnsafeUtility.MemCpy(oldNext, data->Next, data->Capacity * sizeof(int));
            UnsafeUtility.MemCpy(oldBuckets, data->Buckets, data->BucketCapacity * sizeof(int));
            UnsafeUtility.MemCpy(oldData, data->Data, data->DataCapacity * sizeof(int));

            var oldCapacity = data->Capacity;
            var oldBucketCapacity = data->BucketCapacity;
            var oldDataCapacity = data->DataCapacity;
            var oldCount = data->Count;
            var oldDataAllocatedIndex = data->DataAllocatedIndex;

            var oldLog2MinGrowth = data->Log2MinGrowth;
            var hashMapDataSize = sizeof(DynamicUntypedHashMapHelper<TKey>);

            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            data = buffer.AsUntypedHelper<TKey>();
            data->Capacity = newCapacity;
            data->DataCapacity = oldDataCapacity;
            data->BucketCapacityMask = newBucketCapacity - 1;
            data->Log2MinGrowth = oldLog2MinGrowth;

            data->ValuesOffset = hashMapDataSize;
            data->KeysOffset = hashMapDataSize + keyOffset;
            data->NextOffset = hashMapDataSize + nextOffset;
            data->BucketsOffset = hashMapDataSize + bucketOffset;
            data->DataOffset = hashMapDataSize + newDataOffset;

            data->Count = oldCount;
            data->DataAllocatedIndex = oldDataAllocatedIndex;

            // var keys = data->Keys;
            var next = data->Next;
            var buckets = data->Buckets;

            UnsafeUtility.MemCpy(data->Values, oldValue, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(data->Keys, oldKeys, oldCapacity * sizeof(TKey));

            UnsafeUtility.MemCpy(data->Data, oldData, oldDataCapacity * sizeof(int));

            UnsafeUtility.MemCpy(next, oldNext, oldCapacity * sizeof(int));
            UnsafeUtility.MemSet(next + oldCapacity, 0xff, (newCapacity - oldCapacity) * sizeof(int));

            // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
            UnsafeUtility.MemSet(buckets, 0xff, newBucketCapacity * 4);

            for (int bucket = 0; bucket < oldBucketCapacity; ++bucket)
            {
                while (oldBuckets[bucket] >= 0)
                {
                    var curEntry = oldBuckets[bucket];
                    oldBuckets[bucket] = next[curEntry];
                    var newBucket = data->GetBucket(oldKeys[curEntry]);
                    next[curEntry] = buckets[newBucket];
                    buckets[newBucket] = curEntry;
                }
            }
        }

        internal static void ResizeData(DynamicBuffer<byte> buffer, ref DynamicUntypedHashMapHelper<TKey>* data, int newCapacity)
        {
            // This hashmap doesn't allow shrinking
            if (newCapacity < data->DataCapacity)
            {
                return;
            }

            var toAllocate = (newCapacity - data->Capacity) * sizeof(int);

            // As data is stored at end of buffer, we just need to increase buffer capacity size
            var newBufferCapacity = buffer.Length + toAllocate;

            buffer.ResizeUninitialized(newBufferCapacity);
            data = buffer.AsUntypedHelper<TKey>();

            data->DataCapacity = newCapacity;
        }

        internal static void AddOrSet<TValue>(DynamicBuffer<byte> buffer, ref DynamicUntypedHashMapHelper<TKey>* data, in TKey key, TValue value)
            where TValue : unmanaged
        {
            var idx = data->Find(key);

            if (data->Find(key) == -1)
            {
                // Allocate an entry from the free list
                if (data->Count == data->Capacity)
                {
                    var newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                    Resize(buffer, ref data, newCap);
                }

                idx = data->Count++;

                data->CheckIndexOutOfBounds(idx);

                UnsafeUtility.WriteArrayElement(data->Keys, idx, key);

                var bucket = data->GetBucket(key);

                // Add the index to the hash-map
                var next = data->Next;
                next[idx] = data->Buckets[bucket];
                data->Buckets[bucket] = idx;
            }

            var isLarge = sizeof(TValue) > sizeof(int);
            if (isLarge)
            {
                if ((data->DataAllocatedIndex * sizeof(int)) + sizeof(TValue) > data->DataCapacity * sizeof(int))
                {
                    var newCap = CalcCapacityCeilPow2(data->DataCapacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                    ResizeData(buffer, ref data, newCap);
                }

                int* ptr = data->Data + data->DataAllocatedIndex;

                UnsafeUtility.MemCpy(ptr, &value, sizeof(TValue));

                Check.Assume(sizeof(TValue) % sizeof(int) == 0);

                UnsafeUtility.WriteArrayElement(data->Values, idx, data->DataAllocatedIndex);
                data->DataAllocatedIndex += sizeof(TValue) / sizeof(int);
            }
            else
            {
                UnsafeUtility.WriteArrayElement(data->Values, idx, value);
            }
        }

        internal static void AddUnique<TValue>(DynamicBuffer<byte> buffer, ref DynamicUntypedHashMapHelper<TKey>* data, in TKey key, TValue value)
            where TValue : unmanaged
        {
            data->CheckDoesNotExist(key);

            // Allocate an entry from the free list
            if (data->Count == data->Capacity)
            {
                int newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                Resize(buffer, ref data, newCap);
            }

            var idx = data->Count++;

            data->CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(data->Keys, idx, key);

            var bucket = data->GetBucket(key);

            // Add the index to the hash-map
            var next = data->Next;
            next[idx] = data->Buckets[bucket];
            data->Buckets[bucket] = idx;

            var isLarge = sizeof(TValue) > sizeof(int);
            if (isLarge)
            {
                if ((data->DataAllocatedIndex * sizeof(int)) + sizeof(TValue) > data->DataCapacity * sizeof(int))
                {
                    var newCap = CalcCapacityCeilPow2(data->DataCapacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                    ResizeData(buffer, ref data, newCap);
                }

                int* ptr = data->Data + data->DataAllocatedIndex;
                UnsafeUtility.MemCpy(ptr, &value, sizeof(TValue));

                Check.Assume(sizeof(TValue) % sizeof(int) == 0);

                UnsafeUtility.WriteArrayElement(data->Values, idx, data->DataAllocatedIndex);
                data->DataAllocatedIndex += sizeof(TValue) / sizeof(int);
            }
            else
            {
                UnsafeUtility.WriteArrayElement(data->Values, idx, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetBucket(in TKey key)
        {
            return (int)((uint)key.GetHashCode() & this.BucketCapacityMask);
        }

        internal int Find(TKey key)
        {
            if (this.Count > 0)
            {
                // First find the slot based on the hash
                var bucket = this.GetBucket(key);
                var entryIdx = this.Buckets[bucket];

                if ((uint)entryIdx < (uint)this.Capacity)
                {
                    var keys = this.Keys;
                    var next = this.Next;

                    while (!UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(key))
                    {
                        entryIdx = next[entryIdx];
                        if ((uint)entryIdx >= (uint)this.Capacity)
                        {
                            return -1;
                        }
                    }

                    return entryIdx;
                }
            }

            return -1;
        }

        internal bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            var idx = this.Find(key);

            if (idx != -1)
            {
                if (UnsafeUtility.SizeOf<TValue>() > UnsafeUtility.SizeOf<int>())
                {
                    idx = UnsafeUtility.ReadArrayElement<int>(this.Values, idx);

                    // TODO range check
                    item = *(TValue*)(this.Data + idx);
                }
                else
                {
                    item = UnsafeUtility.ReadArrayElement<TValue>(this.Values, idx);
                }

                return true;
            }

            item = default;
            return false;
        }

        private static int CalculateDataSize(
            int capacity,
            int bucketCapacity,
            int dataCapacity,
            out int outKeyOffset,
            out int outNextOffset,
            out int outBucketOffset,
            out int outDataOffset)
        {
            var sizeOfTKey = sizeof(TKey);
            var sizeOfInt = sizeof(int);

            var valuesSize = sizeOfInt * capacity;
            var keysSize = sizeOfTKey * capacity;
            var nextSize = sizeOfInt * capacity;
            var bucketSize = sizeOfInt * bucketCapacity;
            var dataSize = sizeOfInt * dataCapacity;
            var totalSize = valuesSize + keysSize + nextSize + bucketSize + dataSize;

            outKeyOffset = valuesSize;
            outNextOffset = outKeyOffset + keysSize;
            outBucketOffset = outNextOffset + nextSize;
            outDataOffset = outBucketOffset + bucketSize;

            return totalSize;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDoesNotExist(TKey key)
        {
            if (this.Find(key) != -1)
            {
                throw new ArgumentException($"An item with the same key has already been added: {key}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIndexOutOfBounds(int idx)
        {
            if ((uint)idx >= (uint)this.Capacity)
            {
                throw new InvalidOperationException($"Internal HashMap error. idx {idx}");
            }
        }
    }
}
