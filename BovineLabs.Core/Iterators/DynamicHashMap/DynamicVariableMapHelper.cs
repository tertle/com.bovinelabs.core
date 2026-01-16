// <copyright file="DynamicVariableMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Iterators.Columns;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DynamicVariableMapHelper<TKey, TValue, T, TC>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where T : unmanaged, IEquatable<T>
        where TC : unmanaged, IColumn<T>
    {
        internal HashHelper<TKey> KeyHash;
        internal TC Column;

        internal int ValuesOffset;
        internal int Count;
        internal int Capacity;
        internal int BucketCapacityMask; // = bucket capacity - 1
        internal int Log2MinGrowth;
        internal int AllocatedIndex;
        internal int FirstFreeIdx;

        internal int BucketCapacity => this.BucketCapacityMask + 1;

        internal TValue* Values => (TValue*)((byte*)UnsafeUtility.AddressOf(ref this) + this.ValuesOffset);

        internal static void Init(DynamicBuffer<byte> buffer, int capacity, int minGrowth)
        {
            Check.Assume(buffer.Length == 0, "Buffer already assigned");

            var log2MinGrowth = (byte)(32 - math.lzcnt(math.max(1, minGrowth) - 1));
            capacity = CalcCapacityCeilPow2(0, capacity, log2MinGrowth);

            var column = default(TC);
            var bucketCapacity = GetBucketSize(capacity);

            var indexSize = column.CalculateDataSize(capacity);
            var totalSize = CalculateDataSize(
                capacity, bucketCapacity, indexSize, out var dataOffset, out var keyOffset, out var nextOffset, out var bucketOffset, out var indexOffset);

            buffer.ResizeUninitialized(dataOffset + totalSize);

            var data = buffer.AsVariableHelper<TKey, TValue, T, TC>();

            data->Log2MinGrowth = log2MinGrowth;
            data->Capacity = capacity;
            data->BucketCapacityMask = bucketCapacity - 1;

            data->ValuesOffset = dataOffset;
            data->KeyHash = new HashHelper<TKey>((byte*)data, &data->KeyHash, dataOffset, keyOffset, nextOffset, bucketOffset);

            var offset = (int)((byte*)&data->Column - (byte*)data);
            Check.Assume(offset < int.MaxValue);
            offset = dataOffset + indexOffset - offset;

            column.Initialize(offset, capacity);
            data->Column = column;

            data->Clear(); // also sets FirstFreeIdx, Count, AllocatedIndex
        }

        internal static void Resize(DynamicBuffer<byte> buffer, ref DynamicVariableMapHelper<TKey, TValue, T, TC>* data, int newCapacity)
        {
            newCapacity = math.max(newCapacity, data->Count);
            var newBucketCapacity = math.ceilpow2(GetBucketSize(newCapacity));

            if (data->Capacity == newCapacity && data->BucketCapacity == newBucketCapacity)
            {
                return;
            }

            ResizeExact(buffer, ref data, newCapacity, newBucketCapacity);
        }

        internal static int TryAdd(
            DynamicBuffer<byte> buffer, ref DynamicVariableMapHelper<TKey, TValue, T, TC>* data, in TKey key, in TValue value, in T column)
        {
            if (data->Find(key) == -1)
            {
                return AddInternal(buffer, ref data, key, value, column);
            }

            return -1;
        }

        internal static int AddUnique(
            DynamicBuffer<byte> buffer, ref DynamicVariableMapHelper<TKey, TValue, T, TC>* data, in TKey key, in TValue value, in T column)
        {
            data->CheckDoesNotExist(key);
            return AddInternal(buffer, ref data, key, value, column);
        }

        private static void ResizeExact(
            DynamicBuffer<byte> buffer, ref DynamicVariableMapHelper<TKey, TValue, T, TC>* data, int newCapacity, int newBucketCapacity)
        {
            var indexSize = data->Column.CalculateDataSize(newCapacity);
            var totalSize = CalculateDataSize(
                newCapacity, newBucketCapacity,  indexSize, out var dataOffset, out var keyOffset, out var nextOffset, out var bucketOffset, out var indexOffset);

            var oldValue = (TValue*)UnsafeUtility.Malloc(data->Capacity * sizeof(TValue), UnsafeUtility.AlignOf<TValue>(), Allocator.Temp);
            UnsafeUtility.MemCpy(oldValue, data->Values, data->Capacity * sizeof(TValue));

            var kHelper = new HashHelper<TKey>.Resize(ref data->KeyHash, data->Capacity, data->BucketCapacity);
            var cHelper = data->Column.StartResize();

            var oldCapacity = data->Capacity;
            var oldBucketCapacity = data->BucketCapacity;
            var oldCount = data->Count;
            var oldFirstFreeIdx = data->FirstFreeIdx;
            var oldAllocatedIndex = data->AllocatedIndex;

            var oldLog2MinGrowth = data->Log2MinGrowth;

            buffer.ResizeUninitialized(dataOffset + totalSize);

            data = buffer.AsVariableHelper<TKey, TValue, T, TC>();
            data->Capacity = newCapacity;
            data->BucketCapacityMask = newBucketCapacity - 1;
            data->Log2MinGrowth = oldLog2MinGrowth;

            data->ValuesOffset = dataOffset;
            data->KeyHash = new HashHelper<TKey>((byte*)data, &data->KeyHash, dataOffset, keyOffset, nextOffset, bucketOffset);

            data->Column = default;
            var offset = (int)((byte*)&data->Column - (byte*)data);
            Check.Assume(offset < int.MaxValue);
            offset = dataOffset + indexOffset - offset;

            data->Column.Initialize(offset, newCapacity);

            if (newCapacity > oldCapacity)
            {
                data->Count = oldCount;
                data->FirstFreeIdx = oldFirstFreeIdx;
                data->AllocatedIndex = oldAllocatedIndex;

                UnsafeUtility.MemCpy(data->Values, oldValue, oldCapacity * sizeof(TValue));

                kHelper.Increase(ref data->KeyHash, newCapacity, newBucketCapacity);
                data->Column.ApplyResize(cHelper);

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
                        AddNoCollideNoAlloc(data, kHelper.OldKeys[idx], oldValue[idx], data->Column.GetValueOld(cHelper, idx));
                    }
                }
            }

            UnsafeUtility.Free(oldValue, Allocator.Temp);
        }

        private static int AddInternal(
            DynamicBuffer<byte> buffer, ref DynamicVariableMapHelper<TKey, TValue, T, TC>* data, TKey key, in TValue value, in T column)
        {
            // Allocate an entry from the free list
            if (data->AllocatedIndex >= data->Capacity && data->FirstFreeIdx < 0)
            {
                var newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                Resize(buffer, ref data, newCap);
            }

            return AddNoCollideNoAlloc(data, key, value, column);
        }

        private static int AddNoCollideNoAlloc(DynamicVariableMapHelper<TKey, TValue, T, TC>* data, in TKey key, in TValue value, in T column)
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
            UnsafeUtility.WriteArrayElement(data->Values, idx, value);

            // Add the key to the hash-map
            var keyBucket = HashHelper<TKey>.GetBucket(key, data->BucketCapacityMask);
            var keyNext = data->KeyHash.Next;
            keyNext[idx] = data->KeyHash.Buckets[keyBucket];
            data->KeyHash.Buckets[keyBucket] = idx;

            data->Column.Add(column, idx);

            data->Count++;
            return idx;
        }

        internal void Clear()
        {
            this.KeyHash.Clear(this.Capacity, this.BucketCapacity);
            this.Column.Clear();

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

                    this.Column.Remove(entryIdx);

                    this.Count--;
                    return true;
                }

                prevEntry = entryIdx;
                entryIdx = this.KeyHash.Next[entryIdx];
            }

            return false;
        }

        internal void RemoveAt(int entryIdx)
        {
            this.CheckIndexOutOfBounds(entryIdx);

            // Get the key at this index
            var key = UnsafeUtility.ReadArrayElement<TKey>(this.KeyHash.Keys, entryIdx);

            this.ValidateKey(key, entryIdx);

            // Find which bucket this key belongs to
            var bucket = HashHelper<TKey>.GetBucket(key, this.BucketCapacityMask);

            // Walk the chain to find the previous element
            var prevEntry = -1;
            var currentIdx = this.KeyHash.Buckets[bucket];

            while (currentIdx >= 0 && currentIdx < this.Capacity)
            {
                if (currentIdx == entryIdx)
                {
                    // Found the element to remove
                    if (prevEntry < 0)
                    {
                        this.KeyHash.Buckets[bucket] = this.KeyHash.Next[entryIdx];
                    }
                    else
                    {
                        this.KeyHash.Next[prevEntry] = this.KeyHash.Next[entryIdx];
                    }

                    // Add to free list
                    this.KeyHash.Next[entryIdx] = this.FirstFreeIdx;
                    this.FirstFreeIdx = entryIdx;

                    this.Column.Remove(entryIdx);
                    this.Count--;
                    return;
                }

                prevEntry = currentIdx;
                currentIdx = this.KeyHash.Next[currentIdx];
            }
        }

        internal bool TryGetValue(TKey key, out TValue item, out T column)
        {
            var idx = this.Find(key);

            if (idx != -1)
            {
                item = UnsafeUtility.ReadArrayElement<TValue>(this.Values, idx);
                column = this.Column.GetValue(idx);
                return true;
            }

            item = default;
            column = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TKey GetKeyAtIndex(int index)
        {
            return UnsafeUtility.ReadArrayElement<TKey>(this.KeyHash.Keys, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref TValue GetValueAtIndex(int index)
        {
            return ref UnsafeUtility.ArrayElementAsRef<TValue>(this.Values, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T GetColumnAtIndex(int index)
        {
            return this.Column.GetValue(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void GetAtIndex(int index, out TKey key, out TValue item, out T column)
        {
            key = this.GetKeyAtIndex(index);
            item = this.GetValueAtIndex(index);
            column = this.GetColumnAtIndex(index);
        }

        private static int CalculateDataSize(
            int capacity, int bucketCapacity, int indexSize,
            out int outDataOffset, out int outKeyOffset, out int outNextOffset, out int outBucketOffset, out int outIndexOffset)
        {
            var sizeOfTKey = sizeof(TKey);
            var sizeOfTValue = sizeof(TValue);
            var sizeOfInt = sizeof(int);

            var hashMapDataSize = sizeof(DynamicVariableMapHelper<TKey, TValue, T, TC>);
            var alignOfTValue = UnsafeUtility.AlignOf<TValue>();
            outDataOffset = CollectionHelper.Align(hashMapDataSize, math.max(16, alignOfTValue));

            var valuesSize = sizeOfTValue * capacity;

            var keysOffset = CollectionHelper.Align(valuesSize, UnsafeUtility.AlignOf<TKey>());
            var keysSize = sizeOfTKey * capacity;

            var nextOffset = CollectionHelper.Align(keysOffset + keysSize, UnsafeUtility.AlignOf<int>());
            var nextSize = sizeOfInt * capacity;

            var bucketOffset = CollectionHelper.Align(nextOffset + nextSize, UnsafeUtility.AlignOf<int>());
            var bucketSize = sizeOfInt * bucketCapacity;

            var indexOffset = CollectionHelper.Align(bucketOffset + bucketSize, math.max(16, UnsafeUtility.AlignOf<T>()));

            outKeyOffset = keysOffset;
            outNextOffset = nextOffset;
            outBucketOffset = bucketOffset;
            outIndexOffset = indexOffset;

            return indexOffset + indexSize;
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
        private void ValidateKey(TKey key, int idx)
        {
            var actual = this.Find(key);

            if (actual != idx)
            {
                throw new ArgumentException($"Index does not exist: {idx}");
            }

        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIndexOutOfBounds(int idx)
        {
            if ((uint)idx >= (uint)this.Capacity)
            {
                throw new InvalidOperationException($"Index out of bounds,idx {idx}");
            }
        }

        internal struct Enumerator
        {
            [NativeDisableUnsafePtrRestriction]
            internal readonly DynamicVariableMapHelper<TKey, TValue, T, TC>* Data;
            internal int Index;
            internal int BucketIndex;
            internal int NextIndex;

            internal Enumerator(DynamicVariableMapHelper<TKey, TValue, T, TC>* data)
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
            internal DynamicVariableMap<TKey, TValue, T, TC>.KVC GetCurrent()
            {
                return new DynamicVariableMap<TKey, TValue, T, TC>.KVC
                {
                    Data = this.Data,
                    Index = this.Index,
                };
            }
        }
    }
}
