// <copyright file="DynamicIndexedMapHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe ref struct DynamicIndexedMapHelper<TKey, TIndex, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex : unmanaged, IEquatable<TIndex>
        where TValue : unmanaged
    {
        internal int ValuesOffset;

        internal HashHelper<TKey> KeyHash;
        internal HashHelper<TIndex> IndexHash;

        internal int Count;
        internal int Capacity;
        internal int BucketCapacityMask; // = bucket capacity - 1
        internal int Log2MinGrowth;
        internal int AllocatedIndex;
        internal int FirstFreeIdx;

        internal int BucketCapacity => this.BucketCapacityMask + 1;

        internal TValue* Values
        {
            get
            {
                fixed (DynamicIndexedMapHelper<TKey, TIndex, TValue>* data = &this)
                {
                    return (TValue*)((byte*)data + data->ValuesOffset);
                }
            }
        }

        internal readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Count == 0;
        }

        internal static void Init(DynamicBuffer<byte> buffer, int capacity, int minGrowth)
        {
            Check.Assume(buffer.Length == 0, "Buffer already assigned");

            var log2MinGrowth = (byte)(32 - math.lzcnt(math.max(1, minGrowth) - 1));
            capacity = CalcCapacityCeilPow2(0, capacity, log2MinGrowth);

            var bucketCapacity = GetBucketSize(capacity);
            var totalSize = CalculateDataSize(capacity, bucketCapacity, out var keyOffset, out var nextOffset, out var bucketOffset, out var indexOffset,
                out var indexNextOffset, out var indexBucketOffset);

            var hashMapDataSize = sizeof(DynamicIndexedMapHelper<TKey, TIndex, TValue>);
            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            var data = buffer.AsIndexedHelper<TKey, TIndex, TValue>();

            data->Log2MinGrowth = log2MinGrowth;
            data->Capacity = capacity;
            data->BucketCapacityMask = bucketCapacity - 1;

            data->ValuesOffset = hashMapDataSize;
            data->KeyHash = new HashHelper<TKey>((byte*)data, &data->KeyHash, hashMapDataSize, keyOffset, nextOffset, bucketOffset);
            data->IndexHash = new HashHelper<TIndex>((byte*)data, &data->IndexHash, hashMapDataSize, indexOffset, indexNextOffset, indexBucketOffset);

            data->Clear(); // also sets FirstFreeIdx, Count, AllocatedIndex
        }

        internal static void Resize(DynamicBuffer<byte> buffer, ref DynamicIndexedMapHelper<TKey, TIndex, TValue>* data, int newCapacity)
        {
            newCapacity = math.max(newCapacity, data->Count);
            var newBucketCapacity = math.ceilpow2(GetBucketSize(newCapacity));

            if (data->Capacity == newCapacity && data->BucketCapacity == newBucketCapacity)
            {
                return;
            }

            ResizeExact(buffer, ref data, newCapacity, newBucketCapacity);
        }

        internal static void ResizeExact(
            DynamicBuffer<byte> buffer, ref DynamicIndexedMapHelper<TKey, TIndex, TValue>* data, int newCapacity, int newBucketCapacity)
        {
            var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, out var keyOffset, out var nextOffset, out var bucketOffset, out var indexOffset,
                out var indexNextOffset, out var indexBucketOffset);

            var oldValue = (TValue*)UnsafeUtility.Malloc(data->Capacity * sizeof(TValue), UnsafeUtility.AlignOf<TValue>(), Allocator.Temp);
            UnsafeUtility.MemCpy(oldValue, data->Values, data->Capacity * sizeof(TValue));

            var kHelper = new HashHelper<TKey>.Resize(ref data->KeyHash, data->Capacity, data->BucketCapacity);
            var iHelper = new HashHelper<TIndex>.Resize(ref data->IndexHash, data->Capacity, data->BucketCapacity);

            var oldCapacity = data->Capacity;
            var oldBucketCapacity = data->BucketCapacity;
            var oldCount = data->Count;
            var oldFirstFreeIdx = data->FirstFreeIdx;
            var oldAllocatedIndex = data->AllocatedIndex;

            var oldLog2MinGrowth = data->Log2MinGrowth;
            var hashMapDataSize = sizeof(DynamicIndexedMapHelper<TKey, TIndex, TValue>);

            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            data = buffer.AsIndexedHelper<TKey, TIndex, TValue>();
            data->Capacity = newCapacity;
            data->BucketCapacityMask = newBucketCapacity - 1;
            data->Log2MinGrowth = oldLog2MinGrowth;

            data->ValuesOffset = hashMapDataSize;
            data->KeyHash = new HashHelper<TKey>((byte*)data, &data->KeyHash, hashMapDataSize, keyOffset, nextOffset, bucketOffset);
            data->IndexHash = new HashHelper<TIndex>((byte*)data, &data->IndexHash, hashMapDataSize, indexOffset, indexNextOffset, indexBucketOffset);

            if (newCapacity > oldCapacity)
            {
                data->Count = oldCount;
                data->FirstFreeIdx = oldFirstFreeIdx;
                data->AllocatedIndex = oldAllocatedIndex;

                UnsafeUtility.MemCpy(data->Values, oldValue, oldCapacity * sizeof(TValue));

                kHelper.Increase(ref data->KeyHash, newCapacity, newBucketCapacity);
                iHelper.Increase(ref data->IndexHash, newCapacity, newBucketCapacity);

                if (data->AllocatedIndex > data->Capacity)
                {
                    data->AllocatedIndex = data->Capacity;
                }
            }
            else
            {
                data->Clear();

                for (var i = 0; i < oldBucketCapacity; ++i)
                {
                    for (var idx = kHelper.OldBuckets[i]; idx != -1; idx = kHelper.OldNext[idx])
                    {
                        AddNoCollideNoAlloc(data, kHelper.OldKeys[idx], iHelper.OldKeys[idx], oldValue[idx]);
                    }
                }
            }
        }

        internal static int TryAdd(
            DynamicBuffer<byte> buffer, ref DynamicIndexedMapHelper<TKey, TIndex, TValue>* data, in TKey key, in TIndex index, in TValue value)
        {
            if (data->Find(key) == -1)
            {
                return AddInternal(buffer, ref data, key, index, value);
            }

            return -1;
        }

        internal static int AddUnique(
            DynamicBuffer<byte> buffer, ref DynamicIndexedMapHelper<TKey, TIndex, TValue>* data, in TKey key, in TIndex index, in TValue value)
        {
            data->CheckDoesNotExist(key);
            return AddInternal(buffer, ref data, key, index, value);
        }

        internal static void Flatten(DynamicBuffer<byte> buffer, ref DynamicIndexedMapHelper<TKey, TIndex, TValue>* data)
        {
            var capacity = CalcCapacityCeilPow2(data->Count, data->Count, data->Log2MinGrowth);
            ResizeExact(buffer, ref data, capacity, GetBucketSize(capacity));
        }

        private static int AddInternal(
            DynamicBuffer<byte> buffer, ref DynamicIndexedMapHelper<TKey, TIndex, TValue>* data, TKey key, TIndex index, TValue value)
        {
            // Allocate an entry from the free list
            if (data->AllocatedIndex >= data->Capacity && data->FirstFreeIdx < 0)
            {
                var newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                Resize(buffer, ref data, newCap);
            }

            return AddNoCollideNoAlloc(data, key, index, value);
        }

        private static int AddNoCollideNoAlloc(DynamicIndexedMapHelper<TKey, TIndex, TValue>* data, in TKey key, in TIndex index, in TValue value)
        {
            Check.Assume(data->AllocatedIndex < data->Capacity || data->FirstFreeIdx >= 0);

            var idx = data->FirstFreeIdx;

            if (idx >= 0)
            {
                data->FirstFreeIdx = data->KeyHash.Next[idx];
            }
            else
            {
                idx = data->AllocatedIndex++;
            }

            data->CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(data->KeyHash.Keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->IndexHash.Keys, idx, index);
            UnsafeUtility.WriteArrayElement(data->Values, idx, value);

            // Add the key to the hash-map
            var keyBucket = HashHelper<TKey>.GetBucket(key, data->BucketCapacityMask);
            var keyNext = data->KeyHash.Next;
            keyNext[idx] = data->KeyHash.Buckets[keyBucket];
            data->KeyHash.Buckets[keyBucket] = idx;

            // Add the index to the hash-map
            var indexBucket = HashHelper<TIndex>.GetBucket(index, data->BucketCapacityMask);
            var indexNext = data->IndexHash.Next;
            indexNext[idx] = data->IndexHash.Buckets[indexBucket];
            data->IndexHash.Buckets[indexBucket] = idx;

            data->Count++;
            return idx;
        }

        internal void Clear()
        {
            this.KeyHash.Clear(this.Capacity, this.BucketCapacity);
            this.IndexHash.Clear(this.Capacity, this.BucketCapacity);

            this.Count = 0;
            this.FirstFreeIdx = -1;
            this.AllocatedIndex = 0;
        }

        internal int Find(TKey key)
        {
            if (this.AllocatedIndex > 0)
            {
                return this.KeyHash.Find(key, this.Capacity, this.BucketCapacityMask);
            }

            return -1;
        }

        internal bool Remove(TKey key)
        {
            if (this.Capacity == 0)
            {
                return false;
            }

            // First find the slot based on the hash
            var bucket = HashHelper<TKey>.GetBucket(key, this.BucketCapacityMask);

            var prevEntry = -1;
            var entryIdx = this.KeyHash.Buckets[bucket];

            while (entryIdx >= 0 && entryIdx < this.Capacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(this.KeyHash.Keys, entryIdx).Equals(key))
                {
                    // Found matching element, remove it
                    if (prevEntry < 0)
                    {
                        this.KeyHash.Buckets[bucket] = this.KeyHash.Next[entryIdx];
                    }
                    else
                    {
                        this.KeyHash.Next[prevEntry] = this.KeyHash.Next[entryIdx];
                    }

                    // And free the index
                    this.KeyHash.Next[entryIdx] = this.FirstFreeIdx;
                    this.FirstFreeIdx = entryIdx;

                    // We need to iterate until we find the same index
                    var index = UnsafeUtility.ReadArrayElement<TIndex>(this.IndexHash.Keys, entryIdx);

                    var indexBucket = HashHelper<TIndex>.GetBucket(index, this.BucketCapacityMask);
                    var indexPrevEntry = -1;
                    var indexEntryIdx = this.IndexHash.Buckets[indexBucket];

                    while (entryIdx != indexEntryIdx)
                    {
                        indexPrevEntry = indexEntryIdx;
                        indexEntryIdx = this.IndexHash.Next[indexEntryIdx];
                    }

                    // Found matching element, remove it
                    if (indexPrevEntry < 0)
                    {
                        this.IndexHash.Buckets[indexBucket] = this.IndexHash.Next[indexEntryIdx];
                    }
                    else
                    {
                        this.IndexHash.Next[indexPrevEntry] = this.IndexHash.Next[indexEntryIdx];
                    }

                    // And free the index
                    this.KeyHash.Next[indexEntryIdx] = this.KeyHash.Next[entryIdx]; // TODO we don't add this way so it shouldn't matter

                    this.Count--;
                    return true;
                }

                prevEntry = entryIdx;
                entryIdx = this.KeyHash.Next[entryIdx];
            }

            return false;
        }

        internal bool TryGetValue(TKey key, out TIndex index, out TValue item)
        {
            var idx = this.Find(key);

            if (idx != -1)
            {
                index = UnsafeUtility.ReadArrayElement<TIndex>(this.IndexHash.Keys, idx);
                item = UnsafeUtility.ReadArrayElement<TValue>(this.Values, idx);
                return true;
            }

            index = default;
            item = default;
            return false;
        }

        internal bool TryGetFirstValue(TIndex index, out TKey key, out TValue item, out HashMapIterator<TIndex> it)
        {
            it.Key = index;

            if (this.AllocatedIndex <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                key = default;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            var bucket = HashHelper<TIndex>.GetBucket(it.Key, this.BucketCapacityMask);
            it.EntryIndex = it.NextEntryIndex = this.IndexHash.Buckets[bucket];

            return this.TryGetNextValue(out key, out item, ref it);
        }

        internal bool TryGetNextValue(out TKey key, out TValue item, ref HashMapIterator<TIndex> it)
        {
            var entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;

            if (entryIdx < 0 || entryIdx >= this.Capacity)
            {
                key = default;
                item = default;
                return false;
            }

            var next = this.IndexHash.Next;
            var keys = this.IndexHash.Keys;

            while (!UnsafeUtility.ReadArrayElement<TIndex>(keys, entryIdx).Equals(it.Key))
            {
                entryIdx = next[entryIdx];
                if ((uint)entryIdx >= (uint)this.Capacity)
                {
                    key = default;
                    item = default;
                    return false;
                }
            }

            it.NextEntryIndex = next[entryIdx];
            it.EntryIndex = entryIdx;
            key = UnsafeUtility.ReadArrayElement<TKey>(this.KeyHash.Keys, entryIdx);
            item = UnsafeUtility.ReadArrayElement<TValue>(this.Values, entryIdx);
            return true;
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

            var keys = this.KeyHash.Keys;
            var indices = this.IndexHash.Keys;
            var values = this.Values;

            var shift = this.Count - length - start;

            // var shift = count - le
            UnsafeUtility.MemMove(keys + start, keys + start + length, sizeof(TKey) * shift);
            UnsafeUtility.MemMove(indices + start, indices + start + length, sizeof(TIndex) * shift);
            UnsafeUtility.MemMove(values + start, values + (start + length), shift * sizeof(TValue));

            UnsafeUtility.MemSet(this.KeyHash.Buckets, 0xff, this.BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet((this.KeyHash.Next + this.Count) - length, 0xff, length * sizeof(int)); // only need to clear replaced elements

            UnsafeUtility.MemSet(this.IndexHash.Buckets, 0xff, this.BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet((this.IndexHash.Next + this.Count) - length, 0xff, length * sizeof(int)); // only need to clear replaced elements

            this.AllocatedIndex -= length;
            this.Count -= length;

            var keyBuckets = this.KeyHash.Buckets;
            var keyNext = this.KeyHash.Next;

            for (var idx = 0; idx < this.Count; idx++)
            {
                var bucket = keys[idx].GetHashCode() & this.BucketCapacityMask;
                keyNext[idx] = keyBuckets[bucket];
                keyBuckets[bucket] = idx;
            }

            var indexBuckets = this.IndexHash.Buckets;
            var indexNext = this.IndexHash.Next;

            for (var idx = 0; idx < this.Count; idx++)
            {
                var bucket = indices[idx].GetHashCode() & this.BucketCapacityMask;
                indexNext[idx] = indexBuckets[bucket];
                indexBuckets[bucket] = idx;
            }
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
        private static int GetBucketSize(int capacity)
        {
            return capacity * 2;
        }

        private static int CalculateDataSize(
            int capacity, int bucketCapacity, out int outKeyOffset, out int outNextOffset, out int outBucketOffset, out int outIndexOffset,
            out int outIndexNextOffset, out int outIndexBucketOffset)
        {
            var sizeOfTKey = sizeof(TKey);
            var sizeOfTIndex = sizeof(TIndex);
            var sizeOfTValue = sizeof(TValue);
            var sizeOfInt = sizeof(int);

            var valuesSize = sizeOfTValue * capacity;

            var keysSize = sizeOfTKey * capacity;
            var nextSize = sizeOfInt * capacity;
            var bucketSize = sizeOfInt * bucketCapacity;

            var indexSize = sizeOfTIndex * capacity;
            var indexNextSize = sizeOfInt * capacity;
            var indexBucketSize = sizeOfInt * bucketCapacity;

            var totalSize = valuesSize + keysSize + nextSize + bucketSize + indexSize + indexNextSize + indexBucketSize;

            outKeyOffset = valuesSize;
            outNextOffset = outKeyOffset + keysSize;
            outBucketOffset = outNextOffset + nextSize;
            outIndexOffset = outBucketOffset + bucketSize;
            outIndexNextOffset = outIndexOffset + indexSize;
            outIndexBucketOffset = outIndexNextOffset + indexNextSize;

            return totalSize;
        }

        internal (NativeArray<TKey> Keys, NativeArray<TIndex> Indices, NativeArray<TValue> Values) GetArrays(AllocatorManager.AllocatorHandle allocator)
        {
            var keyOutput = CollectionHelper.CreateNativeArray<TKey>(this.Count, allocator);
            var indexOutput = CollectionHelper.CreateNativeArray<TIndex>(this.Count, allocator);
            var valueOutput = CollectionHelper.CreateNativeArray<TValue>(this.Count, allocator);

            var values = this.Values;
            var keys = this.KeyHash.Keys;
            var buckets = this.KeyHash.Buckets;
            var next = this.KeyHash.Next;

            var indices = this.IndexHash.Keys;

            for (int i = 0, count = 0, max = this.Count, capacity = this.BucketCapacity; i < capacity && count < max; ++i)
            {
                var bucket = buckets[i];

                while (bucket != -1)
                {
                    keyOutput[count] = UnsafeUtility.ReadArrayElement<TKey>(keys, bucket);
                    indexOutput[count] = UnsafeUtility.ReadArrayElement<TIndex>(indices, bucket);
                    valueOutput[count] = UnsafeUtility.ReadArrayElement<TValue>(values, bucket);
                    count++;
                    bucket = next[bucket];
                }
            }

            return (keyOutput, indexOutput, valueOutput);
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
            internal DynamicIndexedMapHelper<TKey, TIndex, TValue>* Data;
            internal int Index;
            internal int BucketIndex;
            internal int NextIndex;

            internal Enumerator(DynamicIndexedMapHelper<TKey, TIndex, TValue>* data)
            {
                this.Data = data;
                this.Index = -1;
                this.BucketIndex = 0;
                this.NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool MoveNext()
            {
                var next = this.Data->KeyHash.Next;

                if (this.NextIndex != -1)
                {
                    this.Index = this.NextIndex;
                    this.NextIndex = next[this.NextIndex];
                    return true;
                }

                var buckets = this.Data->KeyHash.Buckets;

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
            internal KIV<TKey, TIndex, TValue> GetCurrent()
            {
                return new KIV<TKey, TIndex, TValue>
                {
                    Data = this.Data,
                    Index = this.Index,
                };
            }
        }
    }
}
