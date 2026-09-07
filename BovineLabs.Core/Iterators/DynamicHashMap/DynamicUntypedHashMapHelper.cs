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
        internal int SizesOffset;
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

        internal ushort* Sizes
        {
            get
            {
                fixed (DynamicUntypedHashMapHelper<TKey>* data = &this)
                {
                    return (ushort*)((byte*)data + data->SizesOffset);
                }
            }
        }

        internal readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Count == 0;
        }

        internal static void Init(DynamicBuffer<byte> buffer, int capacity, int dataCapacity, int minGrowth)
        {
            Check.Assume(buffer.Length == 0, "Buffer already assigned");

            var log2MinGrowth = (byte)(32 - math.lzcnt(math.max(1, minGrowth) - 1));
            capacity = CalcCapacityCeilPow2(0, capacity, log2MinGrowth);
            dataCapacity = CalcCapacityCeilPow2(0, dataCapacity, log2MinGrowth);

            var bucketCapacity = GetBucketSize(capacity);
            var totalSize = CalculateDataSize(capacity, bucketCapacity, dataCapacity, out var keyOffset, out var nextOffset, out var bucketOffset,
                out var sizesOffset, out var dataOffset);

            var hashMapDataSize = sizeof(DynamicUntypedHashMapHelper<TKey>);
            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            var data = buffer.AsUntypedHelper<TKey>();
            UnsafeUtility.MemClear(data, hashMapDataSize + totalSize);

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
            data->SizesOffset = hashMapDataSize + sizesOffset;
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

            var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, data->DataCapacity, out var keyOffset, out var nextOffset, out var bucketOffset,
                out var sizesOffset, out var dataOffset);

            var oldValue = (byte*)UnsafeUtility.Malloc(data->Capacity * sizeof(int), UnsafeUtility.AlignOf<byte>(), Allocator.Temp);
            var oldKeys = (TKey*)UnsafeUtility.Malloc(data->Capacity * sizeof(TKey), UnsafeUtility.AlignOf<TKey>(), Allocator.Temp);
            var oldNext = (int*)UnsafeUtility.Malloc(data->Capacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var oldBuckets = (int*)UnsafeUtility.Malloc(data->BucketCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var oldSizes = (ushort*)UnsafeUtility.Malloc(data->Capacity * sizeof(ushort), UnsafeUtility.AlignOf<ushort>(), Allocator.Temp);
            var oldData = (int*)UnsafeUtility.Malloc(data->DataCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);

            UnsafeUtility.MemCpy(oldValue, data->Values, data->Capacity * sizeof(int));
            UnsafeUtility.MemCpy(oldKeys, data->Keys, data->Capacity * sizeof(TKey));
            UnsafeUtility.MemCpy(oldNext, data->Next, data->Capacity * sizeof(int));
            UnsafeUtility.MemCpy(oldBuckets, data->Buckets, data->BucketCapacity * sizeof(int));
            UnsafeUtility.MemCpy(oldSizes, data->Sizes, data->Capacity * sizeof(ushort));
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
            UnsafeUtility.MemClear(data, hashMapDataSize + totalSize);
            data->Capacity = newCapacity;
            data->DataCapacity = oldDataCapacity;
            data->BucketCapacityMask = newBucketCapacity - 1;
            data->Log2MinGrowth = oldLog2MinGrowth;

            data->ValuesOffset = hashMapDataSize;
            data->KeysOffset = hashMapDataSize + keyOffset;
            data->NextOffset = hashMapDataSize + nextOffset;
            data->BucketsOffset = hashMapDataSize + bucketOffset;
            data->SizesOffset = hashMapDataSize + sizesOffset;
            data->DataOffset = hashMapDataSize + dataOffset;

            data->Count = oldCount;
            data->DataAllocatedIndex = oldDataAllocatedIndex;

            // Slots are dense, but the variable-data arena can have holes. Restore only
            // live payload bytes; copying old capacity would reintroduce dirty reserve.
            UnsafeUtility.MemCpy(data->Keys, oldKeys, (long)oldCount * sizeof(TKey));
            UnsafeUtility.MemCpy(data->Sizes, oldSizes, (long)oldCount * sizeof(ushort));
            for (var i = 0; i < oldCount; ++i)
            {
                var size = oldSizes[i];
                if (size > sizeof(int))
                {
                    var offset = ((int*)oldValue)[i];
                    ((int*)data->Values)[i] = offset;
                    UnsafeUtility.MemCpy(data->Data + offset, oldData + offset, size);
                }
                else
                {
                    UnsafeUtility.MemCpy(data->Values + (i * sizeof(int)), oldValue + (i * sizeof(int)), size);
                }
            }

            UnsafeUtility.MemCpy(data->Next, oldNext, oldCapacity * sizeof(int));
            UnsafeUtility.MemSet(data->Next + oldCapacity, 0xff, (newCapacity - oldCapacity) * sizeof(int));

            // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
            UnsafeUtility.MemSet(data->Buckets, 0xff, newBucketCapacity * 4);

            var next = data->Next;
            var buckets = data->Buckets;

            for (var bucket = 0; bucket < oldBucketCapacity; ++bucket)
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
            if (newCapacity <= data->DataCapacity)
            {
                return;
            }

            var toAllocate = (newCapacity - data->DataCapacity) * sizeof(int);

            // As data is stored at end of buffer, we just need to increase buffer capacity size
            var oldLength = buffer.Length;
            var newBufferCapacity = oldLength + toAllocate;

            buffer.ResizeUninitialized(newBufferCapacity);
            data = buffer.AsUntypedHelper<TKey>();
            UnsafeUtility.MemClear((byte*)data + oldLength, toAllocate);

            data->DataCapacity = newCapacity;
        }

        internal static void AddOrSet<TValue>(DynamicBuffer<byte> buffer, ref DynamicUntypedHashMapHelper<TKey>* data, in TKey key, TValue value)
            where TValue : unmanaged
        {
            var idx = data->Find(key);
            var isLarge = sizeof(TValue) > sizeof(int);
            var add = idx == -1;

            if (add)
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

                var size = UnsafeUtility.SizeOf<TValue>();
                Check.Assume(size <= ushort.MaxValue, "Size exceeds max allowed size of ushort.MaxValue");
                UnsafeUtility.WriteArrayElement(data->Sizes, idx, (ushort)size);

                var bucket = data->GetBucket(key);

                // Add the index to the hash-map
                var next = data->Next;
                next[idx] = data->Buckets[bucket];
                data->Buckets[bucket] = idx;
            }
            else
            {
                data->CheckSize<TValue>(idx);
            }

            if (isLarge)
            {
                int dataAllocatedIndex;

                // Sets don't need to allocate, element should already exist
                if (add)
                {
                    Check.Assume(sizeof(TValue) % sizeof(int) == 0);

                    var previousDataAllocatedIndex = data->DataAllocatedIndex;
                    data->DataAllocatedIndex = AlignDataAllocatedIndex<TValue>(previousDataAllocatedIndex);

                    var minNewCapacity = data->DataAllocatedIndex + (sizeof(TValue) / sizeof(int));
                    if (minNewCapacity > data->DataCapacity)
                    {
                        var newCap = data->DataCapacity;
                        do
                        {
                            newCap = CalcCapacityCeilPow2(newCap + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                        }
                        while (newCap < minNewCapacity);

                        ResizeData(buffer, ref data, newCap);
                    }

                    UnsafeUtility.MemClear(data->Data + previousDataAllocatedIndex,
                        (long)(data->DataAllocatedIndex - previousDataAllocatedIndex) * sizeof(int));
                    dataAllocatedIndex = data->DataAllocatedIndex;

                    var dst = (int*)data->Values + idx;
                    *dst = data->DataAllocatedIndex;

                    data->DataAllocatedIndex += sizeof(TValue) / sizeof(int);
                }
                else
                {
                    // Set, just read the stored address
                    dataAllocatedIndex = *((int*)data->Values + idx);
                }

                var ptr = data->Data + dataAllocatedIndex;
                UnsafeUtility.MemCpy(ptr, &value, sizeof(TValue));
            }
            else
            {
                var dst = (TValue*)(data->Values + (idx * sizeof(int)));
                *dst = value;
                UnsafeUtility.MemClear((byte*)dst + sizeof(TValue), sizeof(int) - sizeof(TValue));
            }
        }

        internal static void AddOrSetRaw(DynamicBuffer<byte> buffer, ref DynamicUntypedHashMapHelper<TKey>* data, in TKey key, void* value, int length)
        {
            Check.Assume(length >= 0, "Size must be non-negative");
            Check.Assume(length <= ushort.MaxValue, "Size exceeds max allowed size of ushort.MaxValue");

            var idx = data->Find(key);
            var isLarge = length > sizeof(int);
            var add = idx == -1;

            if (add)
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
                UnsafeUtility.WriteArrayElement(data->Sizes, idx, (ushort)length);

                var bucket = data->GetBucket(key);

                // Add the index to the hash-map
                var next = data->Next;
                next[idx] = data->Buckets[bucket];
                data->Buckets[bucket] = idx;
            }
            else
            {
                data->CheckSize(idx, length);
            }

            if (isLarge)
            {
                int dataAllocatedIndex;

                // Sets don't need to allocate, element should already exist
                if (add)
                {
                    var alignedSize = CollectionHelper.Align(length, sizeof(int));
                    var intsRequired = alignedSize / sizeof(int);

                    var minNewCapacity = data->DataAllocatedIndex + intsRequired;
                    if (minNewCapacity > data->DataCapacity)
                    {
                        var newCap = data->DataCapacity;
                        do
                        {
                            newCap = CalcCapacityCeilPow2(newCap + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                        }
                        while (newCap < minNewCapacity);

                        ResizeData(buffer, ref data, newCap);
                    }

                    dataAllocatedIndex = data->DataAllocatedIndex;

                    var dst = (int*)data->Values + idx;
                    *dst = data->DataAllocatedIndex;

                    data->DataAllocatedIndex += intsRequired;
                }
                else
                {
                    // Set, just read the stored address
                    dataAllocatedIndex = *((int*)data->Values + idx);
                }

                var ptr = (byte*)(data->Data + dataAllocatedIndex);
                WriteRawSlot(ptr, value, length, CollectionHelper.Align(length, sizeof(int)));
            }
            else
            {
                var dst = data->Values + (idx * sizeof(int));
                WriteRawSlot(dst, value, length, sizeof(int));
            }
        }

        internal static ref TValue GetValue<TValue>(DynamicUntypedHashMapHelper<TKey>* data, int idx)
            where TValue : unmanaged
        {
            data->CheckSize<TValue>(idx);

            var isLarge = sizeof(TValue) > sizeof(int);
            if (isLarge)
            {
                var dst = (int*)data->Values + idx;
                var dataAllocatedIndex = *dst;

                return ref UnsafeUtility.AsRef<TValue>(data->Data + dataAllocatedIndex);
            }

            return ref UnsafeUtility.AsRef<TValue>(data->Values + (idx * sizeof(int)));
        }

        internal static byte* GetValueRaw(DynamicUntypedHashMapHelper<TKey>* data, int idx, out int length)
        {
            length = UnsafeUtility.ReadArrayElement<ushort>(data->Sizes, idx);
            if (length > sizeof(int))
            {
                var dataAllocatedIndex = *((int*)data->Values + idx);
                return (byte*)(data->Data + dataAllocatedIndex);
            }

            return data->Values + (idx * sizeof(int));
        }

        internal static int AddUnique<TValue>(DynamicBuffer<byte> buffer, ref DynamicUntypedHashMapHelper<TKey>* data, in TKey key, TValue value)
            where TValue : unmanaged
        {
            data->CheckDoesNotExist(key);

            // Allocate an entry from the free list
            if (data->Count == data->Capacity)
            {
                var newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                Resize(buffer, ref data, newCap);
            }

            var idx = data->Count++;

            data->CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(data->Keys, idx, key);

            var size = UnsafeUtility.SizeOf<TValue>();
            Check.Assume(size <= ushort.MaxValue, "Size exceeds max allowed size of ushort.MaxValue");
            UnsafeUtility.WriteArrayElement(data->Sizes, idx, (ushort)size);

            var bucket = data->GetBucket(key);

            // Add the index to the hash-map
            var next = data->Next;
            next[idx] = data->Buckets[bucket];
            data->Buckets[bucket] = idx;

            var isLarge = sizeof(TValue) > sizeof(int);
            if (isLarge)
            {
                Check.Assume(sizeof(TValue) % sizeof(int) == 0);

                var previousDataAllocatedIndex = data->DataAllocatedIndex;
                data->DataAllocatedIndex = AlignDataAllocatedIndex<TValue>(previousDataAllocatedIndex);

                var minNewCapacity = data->DataAllocatedIndex + (sizeof(TValue) / sizeof(int));
                if (minNewCapacity > data->DataCapacity)
                {
                    var newCap = data->DataCapacity;
                    do
                    {
                        newCap = CalcCapacityCeilPow2(newCap + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                    }
                    while (newCap < minNewCapacity);

                    ResizeData(buffer, ref data, newCap);
                }

                UnsafeUtility.MemClear(data->Data + previousDataAllocatedIndex,
                    (long)(data->DataAllocatedIndex - previousDataAllocatedIndex) * sizeof(int));
                var ptr = data->Data + data->DataAllocatedIndex;
                UnsafeUtility.MemCpy(ptr, &value, sizeof(TValue));

                var dst = (int*)data->Values + idx;
                *dst = data->DataAllocatedIndex;

                data->DataAllocatedIndex += sizeof(TValue) / sizeof(int);
            }
            else
            {
                var dst = (TValue*)(data->Values + (idx * sizeof(int)));
                *dst = value;
                UnsafeUtility.MemClear((byte*)dst + sizeof(TValue), sizeof(int) - sizeof(TValue));
            }

            return idx;
        }

        internal static int AddUniqueRaw(DynamicBuffer<byte> buffer, ref DynamicUntypedHashMapHelper<TKey>* data, in TKey key, void* value, int length)
        {
            Check.Assume(length >= 0, "Size must be non-negative");
            Check.Assume(length <= ushort.MaxValue, "Size exceeds max allowed size of ushort.MaxValue");
            data->CheckDoesNotExist(key);

            // Allocate an entry from the free list
            if (data->Count == data->Capacity)
            {
                var newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                Resize(buffer, ref data, newCap);
            }

            var idx = data->Count++;

            data->CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(data->Keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->Sizes, idx, (ushort)length);

            var bucket = data->GetBucket(key);

            // Add the index to the hash-map
            var next = data->Next;
            next[idx] = data->Buckets[bucket];
            data->Buckets[bucket] = idx;

            if (length > sizeof(int))
            {
                var alignedSize = CollectionHelper.Align(length, sizeof(int));
                var intsRequired = alignedSize / sizeof(int);

                var minNewCapacity = data->DataAllocatedIndex + intsRequired;
                if (minNewCapacity > data->DataCapacity)
                {
                    var newCap = data->DataCapacity;
                    do
                    {
                        newCap = CalcCapacityCeilPow2(newCap + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                    }
                    while (newCap < minNewCapacity);

                    ResizeData(buffer, ref data, newCap);
                }

                var ptr = (byte*)(data->Data + data->DataAllocatedIndex);
                WriteRawSlot(ptr, value, length, alignedSize);

                var dst = (int*)data->Values + idx;
                *dst = data->DataAllocatedIndex;

                data->DataAllocatedIndex += intsRequired;
            }
            else
            {
                var dst = data->Values + (idx * sizeof(int));
                WriteRawSlot(dst, value, length, sizeof(int));
            }

            return idx;
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
                fixed (DynamicUntypedHashMapHelper<TKey>* data = &this)
                {
                    item = GetValue<TValue>(data, idx);
                }

                return true;
            }

            item = default;
            return false;
        }

        internal bool TryGetValueRaw(TKey key, out byte* value, out int length)
        {
            var idx = this.Find(key);

            if (idx != -1)
            {
                fixed (DynamicUntypedHashMapHelper<TKey>* data = &this)
                {
                    value = GetValueRaw(data, idx, out length);
                }

                return true;
            }

            value = null;
            length = 0;
            return false;
        }

        internal int TryRemove(TKey key)
        {
            if (this.Count == 0)
            {
                return -1;
            }

            var bucket = this.GetBucket(key);

            var prevEntry = -1;
            var entryIdx = this.Buckets[bucket];

            while ((uint)entryIdx < (uint)this.Capacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(this.Keys, entryIdx).Equals(key))
                {
                    this.RemoveAt(bucket, prevEntry, entryIdx);
                    return 1;
                }

                prevEntry = entryIdx;
                entryIdx = this.Next[entryIdx];
            }

            return -1;
        }

        internal NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TKey>(this.Count, allocator, NativeArrayOptions.UninitializedMemory);

            var keys = this.Keys;
            var buckets = this.Buckets;
            var next = this.Next;

            for (int i = 0, count = 0, max = result.Length, capacity = this.BucketCapacity; i < capacity && count < max; i++)
            {
                var bucket = buckets[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TKey>(keys, bucket);
                    bucket = next[bucket];
                }
            }

            return result;
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

        private static void WriteRawSlot(byte* destination, void* source, int length, int storageSize)
        {
            // Copy before clearing the tail, so an in-place update retains its source.
            if (length > 0)
            {
                UnsafeUtility.MemMove(destination, source, length);
            }

            UnsafeUtility.MemClear(destination + length, storageSize - length);
        }

        private void RemoveAt(int bucket, int prevEntry, int entryIdx)
        {
            var next = this.Next;
            var buckets = this.Buckets;
            var nextEntry = next[entryIdx];

            if (prevEntry < 0)
            {
                buckets[bucket] = nextEntry;
            }
            else
            {
                next[prevEntry] = nextEntry;
            }

            // Release the removed payload before the last slot replaces its metadata.
            var removedSize = this.Sizes[entryIdx];
            if (removedSize > sizeof(int))
            {
                var removedOffset = ((int*)this.Values)[entryIdx];
                UnsafeUtility.MemClear(this.Data + removedOffset, CollectionHelper.Align(removedSize, sizeof(int)));
            }

            var lastIndex = this.Count - 1;
            if (entryIdx != lastIndex)
            {
                var lastKey = UnsafeUtility.ReadArrayElement<TKey>(this.Keys, lastIndex);
                var lastBucket = this.GetBucket(lastKey);
                var lastPrevEntry = -1;
                var lastEntryIdx = buckets[lastBucket];

                while (lastEntryIdx != lastIndex)
                {
                    lastPrevEntry = lastEntryIdx;
                    lastEntryIdx = next[lastEntryIdx];
                }

                if (lastPrevEntry < 0)
                {
                    buckets[lastBucket] = entryIdx;
                }
                else
                {
                    next[lastPrevEntry] = entryIdx;
                }

                UnsafeUtility.WriteArrayElement(this.Keys, entryIdx, lastKey);
                UnsafeUtility.WriteArrayElement(this.Sizes, entryIdx, UnsafeUtility.ReadArrayElement<ushort>(this.Sizes, lastIndex));
                UnsafeUtility.MemCpy(this.Values + (entryIdx * sizeof(int)), this.Values + (lastIndex * sizeof(int)), sizeof(int));
                next[entryIdx] = next[lastIndex];
            }

            UnsafeUtility.MemClear(this.Keys + lastIndex, sizeof(TKey));
            UnsafeUtility.MemClear(this.Values + (lastIndex * sizeof(int)), sizeof(int));
            this.Sizes[lastIndex] = 0;
            next[lastIndex] = -1;
            this.Count--;

            if (this.Count == 0)
            {
                UnsafeUtility.MemClear(this.Data, (long)this.DataCapacity * sizeof(int));
                this.DataAllocatedIndex = 0;
            }
        }

        private static int CalculateDataSize(
            int capacity, int bucketCapacity, int dataCapacity, out int outKeyOffset, out int outNextOffset, out int outBucketOffset, out int outSizeOffset,
            out int outDataOffset)
        {
            var sizeOfTKey = sizeof(TKey);
            var sizeOfInt = sizeof(int);
            var sizeOfUShort = sizeof(ushort);
            var alignOfTKey = UnsafeUtility.AlignOf<TKey>();

            var valuesSize = sizeOfInt * capacity;
            var keysSize = sizeOfTKey * capacity;
            var nextSize = sizeOfInt * capacity;
            var bucketSize = sizeOfInt * bucketCapacity;
            var sizeSize = sizeOfUShort * capacity;
            var dataSize = sizeOfInt * dataCapacity;

            // Layout is:
            // Values (int[capacity]) -> Keys (TKey[capacity]) -> Next (int[capacity]) -> Buckets (int[bucketCapacity]) -> Sizes (ushort[capacity]) -> Data (int[dataCapacity])
            // Explicitly align each segment to avoid misaligned reads/writes on strict platforms.
            outKeyOffset = CollectionHelper.Align(valuesSize, alignOfTKey);
            outNextOffset = CollectionHelper.Align(outKeyOffset + keysSize, sizeOfInt);
            outBucketOffset = CollectionHelper.Align(outNextOffset + nextSize, sizeOfInt);
            outSizeOffset = CollectionHelper.Align(outBucketOffset + bucketSize, sizeOfUShort);

            // Large values are stored in the Data segment; align the segment so values with higher alignment (e.g. 16) can be stored correctly.
            outDataOffset = CollectionHelper.Align(outSizeOffset + sizeSize, 16);

            return outDataOffset + dataSize;
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckSize<TValue>(int idx)
            where TValue : unmanaged
        {
            var expected = UnsafeUtility.SizeOf<TValue>();
            this.CheckSize(idx, expected);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckSize(int idx, int expected)
        {
            var actual = UnsafeUtility.ReadArrayElement<ushort>(this.Sizes, idx);
            if (expected != actual)
            {
                throw new InvalidOperationException($"Size of type {expected} does not match stored {actual}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AlignDataAllocatedIndex<TValue>(int dataAllocatedIndex)
            where TValue : unmanaged
        {
            var align = UnsafeUtility.AlignOf<TValue>();

            // Data is stored in int units; align the int index so (Data + index) satisfies the TValue alignment.
            // If align < sizeof(int), the base alignment is already sufficient.
            var alignInts = align / sizeof(int);
            if (alignInts <= 1)
            {
                return dataAllocatedIndex;
            }

            return CollectionHelper.Align(dataAllocatedIndex, alignInts);
        }
    }
}
