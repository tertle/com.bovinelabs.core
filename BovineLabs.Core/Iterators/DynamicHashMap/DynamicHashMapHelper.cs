// <copyright file="DynamicHashMapHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DynamicHashMapHelper<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        internal int ValuesOffset;
        internal int KeysOffset;
        internal int NextOffset;
        internal int BucketsOffset;
        internal int Count;
        internal int Capacity;
        internal int BucketCapacityMask; // = bucket capacity - 1
        internal int Log2MinGrowth;
        internal int AllocatedIndex;
        internal int FirstFreeIdx;
        internal int SizeOfTValue;

        internal int BucketCapacity => this.BucketCapacityMask + 1;

        internal byte* Values
        {
            get
            {
                fixed (DynamicHashMapHelper<TKey>* data = &this)
                {
                    return (byte*)data + data->ValuesOffset;
                }
            }
        }

        internal TKey* Keys
        {
            get
            {
                fixed (DynamicHashMapHelper<TKey>* data = &this)
                {
                    return (TKey*)((byte*)data + data->KeysOffset);
                }
            }
        }

        internal int* Next
        {
            get
            {
                fixed (DynamicHashMapHelper<TKey>* data = &this)
                {
                    return (int*)((byte*)data + data->NextOffset);
                }
            }
        }

        internal int* Buckets
        {
            get
            {
                fixed (DynamicHashMapHelper<TKey>* data = &this)
                {
                    return (int*)((byte*)data + data->BucketsOffset);
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

        private static int GetBucketSize(int capacity)
        {
            return capacity * 2;
        }

        internal static void Init(DynamicBuffer<byte> buffer, int capacity, int sizeOfValueT, int minGrowth)
        {
            Check.Assume(buffer.Length == 0, "Buffer already assigned");

            var log2MinGrowth = (byte)(32 - math.lzcnt(math.max(1, minGrowth) - 1));
            capacity = CalcCapacityCeilPow2(0, capacity, log2MinGrowth);

            var bucketCapacity = GetBucketSize(capacity);
            var totalSize = CalculateDataSize(capacity, bucketCapacity, sizeOfValueT, out var keyOffset, out var nextOffset, out var bucketOffset);

            var hashMapDataSize = sizeof(DynamicHashMapHelper<TKey>);
            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            var data = buffer.AsHelper<TKey>();

            data->Log2MinGrowth = log2MinGrowth;
            data->Capacity = capacity;
            data->BucketCapacityMask = bucketCapacity - 1;
            data->SizeOfTValue = sizeOfValueT;

            data->ValuesOffset = hashMapDataSize;
            data->KeysOffset = hashMapDataSize + keyOffset;
            data->NextOffset = hashMapDataSize + nextOffset;
            data->BucketsOffset = hashMapDataSize + bucketOffset;

            data->Clear(); // sets FirstFreeIdx, Count, AllocatedIndex
        }

        internal static void Resize(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, int newCapacity)
        {
            newCapacity = math.max(newCapacity, data->Count);
            var newBucketCapacity = math.ceilpow2(GetBucketSize(newCapacity));

            if (data->Capacity == newCapacity && data->BucketCapacity == newBucketCapacity)
            {
                return;
            }

            ResizeExact(buffer, ref data, newCapacity, newBucketCapacity);
        }

        internal static void ResizeExact(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, int newCapacity, int newBucketCapacity)
        {
            var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, data->SizeOfTValue, out var keyOffset, out var nextOffset, out var bucketOffset);

            var oldValue = (byte*)UnsafeUtility.Malloc(data->Capacity * data->SizeOfTValue, UnsafeUtility.AlignOf<byte>(), Allocator.Temp);
            var oldKeys = (TKey*)UnsafeUtility.Malloc(data->Capacity * sizeof(TKey), UnsafeUtility.AlignOf<TKey>(), Allocator.Temp);
            var oldNext = (int*)UnsafeUtility.Malloc(data->Capacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var oldBuckets = (int*)UnsafeUtility.Malloc(data->BucketCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);

            UnsafeUtility.MemCpy(oldValue, data->Values, data->Capacity * data->SizeOfTValue);
            UnsafeUtility.MemCpy(oldKeys, data->Keys, data->Capacity * sizeof(TKey));
            UnsafeUtility.MemCpy(oldNext, data->Next, data->Capacity * sizeof(int));
            UnsafeUtility.MemCpy(oldBuckets, data->Buckets, data->BucketCapacity * sizeof(int));

            var oldCapacity = data->Capacity;
            var oldBucketCapacity = data->BucketCapacity;
            var oldCount = data->Count;
            var oldFirstFreeIdx = data->FirstFreeIdx;
            var oldAllocatedIndex = data->AllocatedIndex;

            var oldLog2MinGrowth = data->Log2MinGrowth;
            var sizeOfT = data->SizeOfTValue;
            var hashMapDataSize = sizeof(DynamicHashMapHelper<TKey>);

            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            data = buffer.AsHelper<TKey>();
            data->Capacity = newCapacity;
            data->BucketCapacityMask = newBucketCapacity - 1;
            data->Log2MinGrowth = oldLog2MinGrowth;
            data->SizeOfTValue = sizeOfT;

            data->ValuesOffset = hashMapDataSize;
            data->KeysOffset = hashMapDataSize + keyOffset;
            data->NextOffset = hashMapDataSize + nextOffset;
            data->BucketsOffset = hashMapDataSize + bucketOffset;

            if (newCapacity > oldCapacity)
            {
                data->Count = oldCount;
                data->FirstFreeIdx = oldFirstFreeIdx;
                data->AllocatedIndex = oldAllocatedIndex;

                // var keys = data->Keys;
                var next = data->Next;
                var buckets = data->Buckets;

                UnsafeUtility.MemCpy(data->Values, oldValue, oldCapacity * sizeOfT);
                UnsafeUtility.MemCpy(data->Keys, oldKeys, oldCapacity * sizeof(TKey));

                UnsafeUtility.MemCpy(next, oldNext, oldCapacity * sizeof(int));
                UnsafeUtility.MemSet(next + oldCapacity, 0xff, (newCapacity - oldCapacity) * sizeof(int));

                // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
                UnsafeUtility.MemSet(buckets, 0xff, newBucketCapacity * 4);

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

                if (data->AllocatedIndex > data->Capacity)
                {
                    data->AllocatedIndex = data->Capacity;
                }
            }
            else
            {
                data->Clear();

                // TODO can this be made faster?
                for (var i = 0; i < oldBucketCapacity; ++i)
                {
                    for (var idx = oldBuckets[i]; idx != -1; idx = oldNext[idx])
                    {
                        var newIdx = data->AddNoCollideNoAlloc(oldKeys[idx]);
                        UnsafeUtility.MemCpy(data->Values + (sizeOfT * newIdx), oldValue + (sizeOfT * idx), sizeOfT);
                    }
                }
            }
        }

        internal static int TryAdd(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, in TKey key)
        {
            if (data->Find(key) == -1)
            {
                // Allocate an entry from the free list
                if (data->AllocatedIndex >= data->Capacity && data->FirstFreeIdx < 0)
                {
                    var newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                    Resize(buffer, ref data, newCap);
                }

                var idx = data->FirstFreeIdx;

                if (idx >= 0)
                {
                    data->FirstFreeIdx = data->Next[idx];
                }
                else
                {
                    idx = data->AllocatedIndex++;
                }

                data->CheckIndexOutOfBounds(idx);

                UnsafeUtility.WriteArrayElement(data->Keys, idx, key);
                var bucket = data->GetBucket(key);

                // Add the index to the hash-map
                var next = data->Next;
                next[idx] = data->Buckets[bucket];
                data->Buckets[bucket] = idx;
                data->Count++;

                return idx;
            }

            return -1;
        }

        internal static int AddUnique(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, in TKey key)
        {
            data->CheckDoesNotExist(key);

            // Allocate an entry from the free list
            if (data->AllocatedIndex >= data->Capacity && data->FirstFreeIdx < 0)
            {
                var newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                Resize(buffer, ref data, newCap);
            }

            var idx = data->FirstFreeIdx;

            if (idx >= 0)
            {
                data->FirstFreeIdx = data->Next[idx];
            }
            else
            {
                idx = data->AllocatedIndex++;
            }

            data->CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(data->Keys, idx, key);
            var bucket = data->GetBucket(key);

            // Add the index to the hash-map
            var next = data->Next;
            next[idx] = data->Buckets[bucket];
            data->Buckets[bucket] = idx;
            data->Count++;

            return idx;
        }

        internal static int AddMulti(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, in TKey key)
        {
            // Allocate an entry from the free list
            if (data->AllocatedIndex >= data->Capacity && data->FirstFreeIdx < 0)
            {
                var newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                Resize(buffer, ref data, newCap);
            }

            var idx = data->FirstFreeIdx;

            if (idx >= 0)
            {
                data->FirstFreeIdx = data->Next[idx];
            }
            else
            {
                idx = data->AllocatedIndex++;
            }

            data->CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(data->Keys, idx, key);
            var bucket = data->GetBucket(key);

            // Add the index to the hash-map
            var next = data->Next;
            next[idx] = data->Buckets[bucket];
            data->Buckets[bucket] = idx;
            data->Count++;

            return idx;
        }

        internal static void AddBatchUnsafe(
            DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, [NoAlias] TKey* keys, [NoAlias] byte* values, int length)
        {
            var helper = buffer.AsHelper<TKey>();

            var oldLength = helper->Count;
            var newLength = oldLength + length;

            if (helper->Capacity < newLength)
            {
                Resize(buffer, ref data, newLength);
                helper = buffer.AsHelper<TKey>();
            }

            var keyPtr = helper->Keys + oldLength;
            var valuePtr = helper->Values + (oldLength * helper->SizeOfTValue);

            UnsafeUtility.MemCpy(keyPtr, keys, length * sizeof(TKey));
            UnsafeUtility.MemCpy(valuePtr, values, length * helper->SizeOfTValue);

            var buckets = helper->Buckets;
            var nextPtrs = helper->Next + oldLength;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & helper->BucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            helper->AllocatedIndex += length;
            helper->Count += length;
        }

        internal static void AddBatchUnsafe<TValue>(
            DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, NativeSlice<TKey> keys, [NoAlias] NativeSlice<TValue> values)
            where TValue : unmanaged
        {
            var helper = buffer.AsHelper<TKey>();

            Check.Assume(keys.Length == values.Length, "keys.Length != values.Length");

            var length = keys.Length;

            var oldLength = helper->Count;
            var newLength = oldLength + length;

            if (helper->Capacity < newLength)
            {
                Resize(buffer, ref data, newLength);
                helper = buffer.AsHelper<TKey>();
            }

            var keyPtr = helper->Keys + oldLength;
            var valuePtr = helper->Values + (oldLength * helper->SizeOfTValue);

            Check.Assume(helper->SizeOfTValue == UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpyStride(keyPtr, UnsafeUtility.SizeOf<TKey>(), keys.GetUnsafeReadOnlyPtr(), keys.Stride, UnsafeUtility.SizeOf<TKey>(), length);
            UnsafeUtility.MemCpyStride(valuePtr, helper->SizeOfTValue, values.GetUnsafeReadOnlyPtr(), values.Stride, helper->SizeOfTValue, length);

            var buckets = helper->Buckets;
            var nextPtrs = helper->Next + oldLength;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & helper->BucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            helper->AllocatedIndex += length;
            helper->Count += length;
        }

        internal static void AddBatchUnsafe<TValue>(
            DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, NativeSlice<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TValue : unmanaged
        {
            var helper = buffer.AsHelper<TKey>();

            Check.Assume(keys.Length == values.Length, "keys.Length != values.Length");

            var length = keys.Length;

            var oldLength = helper->Count;
            var newLength = oldLength + length;

            if (helper->Capacity < newLength)
            {
                Resize(buffer, ref data, newLength);
                helper = buffer.AsHelper<TKey>();
            }

            var keyPtr = helper->Keys + oldLength;
            var valuePtr = helper->Values + (oldLength * helper->SizeOfTValue);

            Check.Assume(helper->SizeOfTValue == UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpyStride(keyPtr, UnsafeUtility.SizeOf<TKey>(), keys.GetUnsafeReadOnlyPtr(), keys.Stride, UnsafeUtility.SizeOf<TKey>(), length);
            UnsafeUtility.MemCpy(valuePtr, values.GetUnsafeReadOnlyPtr(), helper->SizeOfTValue * length);

            var buckets = helper->Buckets;
            var nextPtrs = helper->Next + oldLength;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & helper->BucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            helper->AllocatedIndex += length;
            helper->Count += length;
        }

        internal static void Flatten(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data)
        {
            var capacity = CalcCapacityCeilPow2(data->Count, data->Count, data->Log2MinGrowth);
            ResizeExact(buffer, ref data, capacity, GetBucketSize(capacity));
        }

        internal void Clear()
        {
            UnsafeUtility.MemSet(this.Buckets, 0xff, this.BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet(this.Next, 0xff, this.Capacity * sizeof(int));

            this.Count = 0;
            this.FirstFreeIdx = -1;
            this.AllocatedIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetBucket(in TKey key)
        {
            return (int)((uint)key.GetHashCode() & this.BucketCapacityMask);
        }

        internal int Find(TKey key)
        {
            if (this.AllocatedIndex > 0)
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

        internal int TryRemove(TKey key)
        {
            if (this.Capacity != 0)
            {
                var removed = 0;

                // First find the slot based on the hash
                var bucket = this.GetBucket(key);

                var keys = this.Keys;
                var next = this.Next;
                var buckets = this.Buckets;

                var prevEntry = -1;
                var entryIdx = buckets[bucket];

                while (entryIdx >= 0 && entryIdx < this.Capacity)
                {
                    if (UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(key))
                    {
                        ++removed;

                        // Found matching element, remove it
                        if (prevEntry < 0)
                        {
                            buckets[bucket] = next[entryIdx];
                        }
                        else
                        {
                            next[prevEntry] = next[entryIdx];
                        }

                        // And free the index
                        next[entryIdx] = this.FirstFreeIdx;
                        this.FirstFreeIdx = entryIdx;
                        break;
                    }

                    prevEntry = entryIdx;
                    entryIdx = next[entryIdx];
                }

                this.Count -= removed;
                return removed != 0 ? removed : -1;
            }

            return -1;
        }

        internal int Remove(TKey key)
        {
            if (this.Capacity == 0)
            {
                return 0;
            }

            var removed = 0;

            // First find the slot based on the hash
            var bucket = this.GetBucket(key);

            var prevEntry = -1;
            var entryIdx = this.Buckets[bucket];

            while (entryIdx >= 0 && entryIdx < this.Capacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(this.Keys, entryIdx).Equals(key))
                {
                    ++removed;

                    // Found matching element, remove it
                    if (prevEntry < 0)
                    {
                        this.Buckets[bucket] = this.Next[entryIdx];
                    }
                    else
                    {
                        this.Next[prevEntry] = this.Next[entryIdx];
                    }

                    // And free the index
                    var nextIdx = this.Next[entryIdx];
                    this.Next[entryIdx] = this.FirstFreeIdx;
                    this.FirstFreeIdx = entryIdx;
                    entryIdx = nextIdx;
                }
                else
                {
                    prevEntry = entryIdx;
                    entryIdx = this.Next[entryIdx];
                }
            }

            this.Count -= removed;
            return removed;
        }

        internal bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            var idx = this.Find(key);

            if (idx != -1)
            {
                item = UnsafeUtility.ReadArrayElement<TValue>(this.Values, idx);
                return true;
            }

            item = default;
            return false;
        }

        internal bool TryGetFirstValue<TValue>(TKey key, out TValue item, out HashMapIterator<TKey> it)
            where TValue : unmanaged
        {
            it.Key = key;

            if (this.AllocatedIndex <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            var bucket = this.GetBucket(it.Key);
            it.EntryIndex = it.NextEntryIndex = this.Buckets[bucket];

            return this.TryGetNextValue(out item, ref it);
        }

        internal bool TryGetNextValue<TValue>(out TValue item, ref HashMapIterator<TKey> it)
            where TValue : unmanaged
        {
            var entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;

            if (entryIdx < 0 || entryIdx >= this.Capacity)
            {
                item = default;
                return false;
            }

            var next = this.Next;
            var keys = this.Keys;

            while (!UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(it.Key))
            {
                entryIdx = next[entryIdx];
                if ((uint)entryIdx >= (uint)this.Capacity)
                {
                    item = default;
                    return false;
                }
            }

            it.NextEntryIndex = next[entryIdx];
            it.EntryIndex = entryIdx;
            item = UnsafeUtility.ReadArrayElement<TValue>(this.Values, entryIdx);
            return true;
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

        internal NativeArray<TValue> GetValueArray<TValue>(AllocatorManager.AllocatorHandle allocator)
            where TValue : unmanaged
        {
            var result = CollectionHelper.CreateNativeArray<TValue>(this.Count, allocator, NativeArrayOptions.UninitializedMemory);

            var values = this.Values;
            var buckets = this.Buckets;
            var next = this.Next;

            for (int i = 0, count = 0, max = result.Length, capacity = this.BucketCapacity; i < capacity && count < max; ++i)
            {
                var bucket = buckets[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TValue>(values, bucket);
                    bucket = next[bucket];
                }
            }

            return result;
        }

        internal NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays<TValue>(AllocatorManager.AllocatorHandle allocator)
            where TValue : unmanaged
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(this.Count, allocator, NativeArrayOptions.UninitializedMemory);

            var keys = this.Keys;
            var values = this.Values;
            var buckets = this.Buckets;
            var next = this.Next;

            for (int i = 0, count = 0, max = result.Length, capacity = this.BucketCapacity; i < capacity && count < max; ++i)
            {
                var bucket = buckets[i];

                while (bucket != -1)
                {
                    result.Keys[count] = UnsafeUtility.ReadArrayElement<TKey>(keys, bucket);
                    result.Values[count] = UnsafeUtility.ReadArrayElement<TValue>(values, bucket);
                    count++;
                    bucket = next[bucket];
                }
            }

            return result;
        }

        internal void RemoveRangeShiftDown(int start, int length)
        {
            if (length == 0)
            {
                return;
            }

            Check.Assume(this.FirstFreeIdx == -1, "Trying to RemoveRangeShiftDown on map with holes. Call Flatten() first.");
            Check.Assume(start >= 0 && start < this.Count);
            Check.Assume(length >= 0 && start + length <= this.Count);

            var keys = this.Keys;
            var values = this.Values;

            var shift = this.Count - length - start;

            // var shift = count - le
            UnsafeUtility.MemMove(keys + start, keys + start + length, UnsafeUtility.SizeOf<TKey>() * shift);
            UnsafeUtility.MemMove(values + (start * this.SizeOfTValue), values + ((start + length) * this.SizeOfTValue), shift * this.SizeOfTValue);

            UnsafeUtility.MemSet(this.Buckets, 0xff, this.BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet((this.Next + this.Count) - length, 0xff, length * sizeof(int)); // only need to clear replaced elements

            this.AllocatedIndex -= length;
            this.Count -= length;

            var buckets = this.Buckets;
            var next = this.Next;

            for (var idx = 0; idx < this.Count; idx++)
            {
                var bucket = keys[idx].GetHashCode() & this.BucketCapacityMask;
                next[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }
        }

        private int AddNoCollideNoAlloc(in TKey key)
        {
            Check.Assume(this.AllocatedIndex < this.Capacity || this.FirstFreeIdx >= 0);

            var idx = this.FirstFreeIdx;

            if (idx >= 0)
            {
                this.FirstFreeIdx = this.Next[idx];
            }
            else
            {
                idx = this.AllocatedIndex++;
            }

            this.CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(this.Keys, idx, key);
            var bucket = this.GetBucket(key);

            var next = this.Next;
            next[idx] = this.Buckets[bucket];
            this.Buckets[bucket] = idx;
            this.Count++;

            return idx;
        }

        private static int CalculateDataSize(
            int capacity, int bucketCapacity, int sizeOfTValue, out int outKeyOffset, out int outNextOffset, out int outBucketOffset)
        {
            var sizeOfTKey = sizeof(TKey);
            var sizeOfInt = sizeof(int);

            var valuesSize = sizeOfTValue * capacity;
            var keysSize = sizeOfTKey * capacity;
            var nextSize = sizeOfInt * capacity;
            var bucketSize = sizeOfInt * bucketCapacity;
            var totalSize = valuesSize + keysSize + nextSize + bucketSize;

            outKeyOffset = valuesSize;
            outNextOffset = outKeyOffset + keysSize;
            outBucketOffset = outNextOffset + nextSize;

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

        internal struct Enumerator
        {
            [NativeDisableUnsafePtrRestriction]
            internal DynamicHashMapHelper<TKey>* Data;
            internal int Index;
            internal int BucketIndex;
            internal int NextIndex;

            internal Enumerator(DynamicHashMapHelper<TKey>* data)
            {
                this.Data = data;
                this.Index = -1;
                this.BucketIndex = 0;
                this.NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool MoveNext()
            {
                var next = this.Data->Next;

                if (this.NextIndex != -1)
                {
                    this.Index = this.NextIndex;
                    this.NextIndex = next[this.NextIndex];
                    return true;
                }

                var buckets = this.Data->Buckets;

                for (int i = this.BucketIndex, num = this.Data->BucketCapacity; i < num; ++i)
                {
                    var idx = buckets[i];

                    if (idx != -1)
                    {
                        this.Index = idx;
                        this.BucketIndex = i + 1;
                        this.NextIndex = next[idx];

                        return true;
                    }
                }

                this.Index = -1;
                this.BucketIndex = this.Data->BucketCapacity;
                this.NextIndex = -1;
                return false;
            }

            internal void Reset()
            {
                this.Index = -1;
                this.BucketIndex = 0;
                this.NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal KVPair<TKey, TValue> GetCurrent<TValue>()
                where TValue : unmanaged
            {
                return new KVPair<TKey, TValue>
                {
                    Data = this.Data,
                    Index = this.Index,
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal TKey GetCurrentKey()
            {
                if (this.Index != -1)
                {
                    return this.Data->Keys[this.Index];
                }

                return default;
            }
        }
    }
}
