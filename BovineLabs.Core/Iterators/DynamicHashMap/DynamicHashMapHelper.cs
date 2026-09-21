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

        internal int BucketCapacity => BucketCapacityMask + 1;

        internal readonly bool IsDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FirstFreeIdx == -1 && AllocatedIndex == Count;
        }

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
            get => Count == 0;
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

            var totalSize = CalculateDataSize(capacity, sizeOfValueT, out var layout);
            buffer.ResizeUninitialized(totalSize);

            var data = buffer.AsHelper<TKey>();
            // Clear the aligned header; Clear() below initializes the remaining layout.
            UnsafeUtility.MemClear(data, layout.ValuesOffset);

            data->Log2MinGrowth = log2MinGrowth;
            data->Capacity = capacity;
            data->BucketCapacityMask = layout.BucketCapacity - 1;
            data->SizeOfTValue = sizeOfValueT;

            data->ValuesOffset = layout.ValuesOffset;
            data->KeysOffset = layout.KeysOffset;
            data->NextOffset = layout.NextOffset;
            data->BucketsOffset = layout.BucketsOffset;

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
            var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, data->SizeOfTValue, out var layout);

            var oldCapacity = data->Capacity;
            var oldBucketCapacity = data->BucketCapacity;
            var oldCount = data->Count;
            var oldAllocatedIndex = data->AllocatedIndex;
            var oldFirstFreeIdx = data->FirstFreeIdx;
            var hasHoles = oldFirstFreeIdx != -1 || oldAllocatedIndex != oldCount;

            // Fast path: growing a holeless map can keep the existing dense indices.
            var canFastGrow = !hasHoles && newCapacity > oldCapacity;

            var oldValue = (byte*)UnsafeUtility.Malloc(oldAllocatedIndex * data->SizeOfTValue, UnsafeUtility.AlignOf<byte>(), Allocator.Temp);
            var oldKeys = (TKey*)UnsafeUtility.Malloc(oldAllocatedIndex * sizeof(TKey), UnsafeUtility.AlignOf<TKey>(), Allocator.Temp);

            int* oldNext = null;
            int* oldBuckets = null;

            if (!canFastGrow)
            {
                oldNext = (int*)UnsafeUtility.Malloc(oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
                oldBuckets = (int*)UnsafeUtility.Malloc(oldBucketCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
                UnsafeUtility.MemCpy(oldNext, data->Next, oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(oldBuckets, data->Buckets, oldBucketCapacity * sizeof(int));
            }

            UnsafeUtility.MemCpy(oldValue, data->Values, oldAllocatedIndex * data->SizeOfTValue);
            UnsafeUtility.MemCpy(oldKeys, data->Keys, oldAllocatedIndex * sizeof(TKey));

            var oldLog2MinGrowth = data->Log2MinGrowth;
            var sizeOfT = data->SizeOfTValue;

            buffer.ResizeUninitialized(totalSize);

            data = buffer.AsHelper<TKey>();
            // Old state is already backed up; never read the destination header here.
            UnsafeUtility.MemClear(data, layout.ValuesOffset);
            data->Capacity = newCapacity;
            data->BucketCapacityMask = newBucketCapacity - 1;
            data->Log2MinGrowth = oldLog2MinGrowth;
            data->SizeOfTValue = sizeOfT;

            data->ValuesOffset = layout.ValuesOffset;
            data->KeysOffset = layout.KeysOffset;
            data->NextOffset = layout.NextOffset;
            data->BucketsOffset = layout.BucketsOffset;

            data->Clear();

            if (canFastGrow)
            {
                // Keep dense indices and only rebuild bucket chains.
                data->Count = oldCount;
                data->AllocatedIndex = oldAllocatedIndex;

                UnsafeUtility.MemCpy(data->Values, oldValue, oldAllocatedIndex * sizeOfT);
                UnsafeUtility.MemCpy(data->Keys, oldKeys, oldAllocatedIndex * sizeof(TKey));

                var buckets = data->Buckets;
                var next = data->Next;

                UnsafeUtility.MemSet(next, 0xff, newCapacity * sizeof(int));
                UnsafeUtility.MemSet(buckets, 0xff, newBucketCapacity * sizeof(int));

                for (var idx = 0; idx < oldCount; ++idx)
                {
                    var bucket = data->GetBucket(oldKeys[idx]);
                    next[idx] = buckets[bucket];
                    buckets[bucket] = idx;
                }
            }
            else
            {
                // Match Unity.Collections behavior: rebuild the map on resize, which also removes holes/free-list state.
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int TryAdd(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, in TKey key)
        {
            if (data->Find(key) == -1)
            {
                return AddNewKey(buffer, ref data, key);
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int AddWithKnownAbsence(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, in TKey key)
        {
            return AddNewKey(buffer, ref data, key);
        }

        internal static int AddUnique(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, in TKey key)
        {
            data->CheckDoesNotExist(key);
            return AddNewKey(buffer, ref data, key);
        }

        internal static int AddMulti(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, in TKey key)
        {
            return AddNewKey(buffer, ref data, key);
        }

        internal static void AddBatchUnsafe(
            DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, [NoAlias] TKey* keys, [NoAlias] byte* values, int length)
        {
            var helper = buffer.AsHelper<TKey>();
            helper->CheckNoHolesForAddBatchUnsafe();

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
            helper->CheckNoHolesForAddBatchUnsafe();

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
            helper->CheckNoHolesForAddBatchUnsafe();

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

        internal static DynamicHashMapDenseWriteView<TKey> BeginDenseRebuild(
            void* buffer, int bufferLength, int count, int capacity, int sizeOfTValue, int log2MinGrowth)
        {
            Check.Assume(buffer != null, "Buffer must not be null.");
            Check.Assume(count >= 0, "Count must be non-negative.");
            Check.Assume(capacity >= count, "Capacity must be greater than or equal to count.");
            Check.Assume(sizeOfTValue >= 0, "Value size must be non-negative.");
            Check.Assume(log2MinGrowth >= 0 && log2MinGrowth < 31, "Log2MinGrowth is out of range.");

            var totalSize = CalculateDataSize(capacity, sizeOfTValue, out var layout);
            Check.Assume(bufferLength >= totalSize, "Buffer length is too small for the requested layout.");

            var data = (DynamicHashMapHelper<TKey>*)buffer;
            // The caller fills the active keys/values; completion initializes hash chains.
            UnsafeUtility.MemClear(data, layout.NextOffset);
            data->ValuesOffset = layout.ValuesOffset;
            data->KeysOffset = layout.KeysOffset;
            data->NextOffset = layout.NextOffset;
            data->BucketsOffset = layout.BucketsOffset;
            data->Count = 0;
            data->Capacity = capacity;
            data->BucketCapacityMask = layout.BucketCapacity - 1;
            data->Log2MinGrowth = log2MinGrowth;
            data->AllocatedIndex = 0;
            data->FirstFreeIdx = -1;
            data->SizeOfTValue = sizeOfTValue;

            return new DynamicHashMapDenseWriteView<TKey>
            {
                Data = data,
                Count = count,
                TotalSize = totalSize,
            };
        }

        internal static void CompleteDenseRebuild(
            ref DynamicHashMapDenseWriteView<TKey> view, DynamicHashMapRebuildOrder rebuildOrder = DynamicHashMapRebuildOrder.Default,
            DynamicHashMapDuplicatePolicy duplicatePolicy = DynamicHashMapDuplicatePolicy.RejectDuplicateKeys)
        {
            var data = view.Data;
            Check.Assume(data != null, "Dense write view is invalid.");
            Check.Assume(view.Count >= 0 && view.Count <= data->Capacity, "Dense write view count is out of range.");

            // Count is mutable while building. Discard any entries outside the final prefix.
            UnsafeUtility.MemClear(data->Values + ((long)view.Count * data->SizeOfTValue),
                (long)(data->Capacity - view.Count) * data->SizeOfTValue);
            UnsafeUtility.MemClear(data->Keys + view.Count, (long)(data->Capacity - view.Count) * sizeof(TKey));

            UnsafeUtility.MemSet(data->Next, 0xff, data->Capacity * sizeof(int));
            UnsafeUtility.MemSet(data->Buckets, 0xff, data->BucketCapacity * sizeof(int));

            var buckets = data->Buckets;
            var next = data->Next;
            var keys = data->Keys;

            if (rebuildOrder == DynamicHashMapRebuildOrder.PreservePackedChainOrder)
            {
                for (var idx = view.Count - 1; idx >= 0; --idx)
                {
                    var bucket = data->GetBucket(keys[idx]);
                    CheckNoDuplicateKey(keys, next, buckets[bucket], keys[idx], duplicatePolicy);
                    next[idx] = buckets[bucket];
                    buckets[bucket] = idx;
                }
            }
            else
            {
                for (var idx = 0; idx < view.Count; ++idx)
                {
                    var bucket = data->GetBucket(keys[idx]);
                    CheckNoDuplicateKey(keys, next, buckets[bucket], keys[idx], duplicatePolicy);
                    next[idx] = buckets[bucket];
                    buckets[bucket] = idx;
                }
            }

            data->Count = view.Count;
            data->AllocatedIndex = view.Count;
            data->FirstFreeIdx = -1;
        }

        internal void Clear()
        {
            // Values, keys and the gaps between them are part of DynamicBuffer.Length.
            UnsafeUtility.MemClear(Values, NextOffset - ValuesOffset);
            UnsafeUtility.MemSet(Buckets, 0xff, BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet(Next, 0xff, Capacity * sizeof(int));

            Count = 0;
            FirstFreeIdx = -1;
            AllocatedIndex = 0;
        }

        internal void ClearDense()
        {
            if (!IsDense)
            {
                Clear();
                return;
            }

            var keys = Keys;
            var buckets = Buckets;
            var count = Count;

            for (var i = 0; i < count; i++)
            {
                buckets[GetBucket(keys[i])] = -1;
            }

            UnsafeUtility.MemClear(Values, (long)count * SizeOfTValue);
            UnsafeUtility.MemClear(keys, (long)count * sizeof(TKey));
            UnsafeUtility.MemSet(Next, 0xff, (long)count * sizeof(int));

            Count = 0;
            FirstFreeIdx = -1;
            AllocatedIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetBucket(in TKey key)
        {
            return (int)((uint)key.GetHashCode() & BucketCapacityMask);
        }

        internal int CopyActiveEntriesTo(byte* keysDestination, int keyStride, byte* valuesDestination, int valueStride)
        {
            return CopyActiveEntriesTo(keysDestination, keyStride, valuesDestination, valueStride, DynamicHashMapTraversalOrder.DenseIndex);
        }

        internal int CopyActiveEntriesTo(
            byte* keysDestination, int keyStride, byte* valuesDestination, int valueStride, DynamicHashMapTraversalOrder traversalOrder)
        {
            Check.Assume(keysDestination != null || Count == 0, "Keys destination must not be null.");
            Check.Assume(valuesDestination != null || Count == 0 || SizeOfTValue == 0, "Values destination must not be null.");
            Check.Assume(keyStride >= sizeof(TKey), "Key stride is too small.");
            Check.Assume(valueStride >= SizeOfTValue, "Value stride is too small.");

            if (Count == 0)
            {
                return 0;
            }

            if (traversalOrder == DynamicHashMapTraversalOrder.DenseIndex && IsDense)
            {
                UnsafeUtility.MemCpyStride(keysDestination, keyStride, Keys, sizeof(TKey), sizeof(TKey), Count);

                if (SizeOfTValue > 0)
                {
                    UnsafeUtility.MemCpyStride(valuesDestination, valueStride, Values, SizeOfTValue, SizeOfTValue, Count);
                }

                return Count;
            }

            var keys = Keys;
            var values = Values;
            var buckets = Buckets;
            var next = Next;
            var packedIndex = 0;

            for (int i = 0, capacity = BucketCapacity; i < capacity && packedIndex < Count; ++i)
            {
                var entryIndex = buckets[i];

                while (entryIndex != -1)
                {
                    UnsafeUtility.MemCpy(keysDestination + (packedIndex * keyStride), keys + entryIndex, sizeof(TKey));

                    if (SizeOfTValue > 0)
                    {
                        UnsafeUtility.MemCpy(valuesDestination + (packedIndex * valueStride), values + (entryIndex * SizeOfTValue), SizeOfTValue);
                    }

                    packedIndex++;
                    entryIndex = next[entryIndex];
                }
            }

            Check.Assume(packedIndex == Count, "Active entry traversal did not visit Count entries.");
            return packedIndex;
        }

        internal int Find(TKey key)
        {
            if (AllocatedIndex > 0)
            {
                // First find the slot based on the hash
                var bucket = GetBucket(key);
                var entryIdx = Buckets[bucket];

                if ((uint)entryIdx < (uint)Capacity)
                {
                    var keys = Keys;
                    var next = Next;

                    while (!UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(key))
                    {
                        entryIdx = next[entryIdx];
                        if ((uint)entryIdx >= (uint)Capacity)
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
            if (Capacity != 0)
            {
                var removed = 0;

                // First find the slot based on the hash
                var bucket = GetBucket(key);

                var keys = Keys;
                var next = Next;
                var buckets = Buckets;

                var prevEntry = -1;
                var entryIdx = buckets[bucket];

                while (entryIdx >= 0 && entryIdx < Capacity)
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
                        ClearEntryData(entryIdx);
                        next[entryIdx] = FirstFreeIdx;
                        FirstFreeIdx = entryIdx;
                        break;
                    }

                    prevEntry = entryIdx;
                    entryIdx = next[entryIdx];
                }

                Count -= removed;
                return removed != 0 ? removed : -1;
            }

            return -1;
        }

        internal int Remove(TKey key)
        {
            if (Capacity == 0)
            {
                return 0;
            }

            var removed = 0;

            // First find the slot based on the hash
            var bucket = GetBucket(key);

            var prevEntry = -1;
            var entryIdx = Buckets[bucket];

            while (entryIdx >= 0 && entryIdx < Capacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(Keys, entryIdx).Equals(key))
                {
                    ++removed;

                    // Found matching element, remove it
                    if (prevEntry < 0)
                    {
                        Buckets[bucket] = Next[entryIdx];
                    }
                    else
                    {
                        Next[prevEntry] = Next[entryIdx];
                    }

                    // And free the index
                    var nextIdx = Next[entryIdx];
                    ClearEntryData(entryIdx);
                    Next[entryIdx] = FirstFreeIdx;
                    FirstFreeIdx = entryIdx;
                    entryIdx = nextIdx;
                }
                else
                {
                    prevEntry = entryIdx;
                    entryIdx = Next[entryIdx];
                }
            }

            Count -= removed;
            return removed;
        }

        internal void Remove(HashMapIterator<TKey> it)
        {
            if ((uint)it.EntryIndex >= (uint)Capacity)
            {
                ThrowInvalidIterator();
                return;
            }

            CheckIteratorKey(it);

            var bucket = GetBucket(it.Key);
            var entryIdx = Buckets[bucket];

            if (entryIdx == it.EntryIndex)
            {
                Buckets[bucket] = Next[entryIdx];
            }
            else
            {
                while (entryIdx >= 0 && entryIdx < Capacity && Next[entryIdx] != it.EntryIndex)
                {
                    entryIdx = Next[entryIdx];
                }

                if ((uint)entryIdx >= (uint)Capacity)
                {
                    ThrowInvalidIterator();
                    return;
                }

                Next[entryIdx] = Next[it.EntryIndex];
            }

            ClearEntryData(it.EntryIndex);
            Next[it.EntryIndex] = FirstFreeIdx;
            FirstFreeIdx = it.EntryIndex;
            Count--;
        }

        internal bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            var idx = Find(key);

            if (idx != -1)
            {
                item = UnsafeUtility.ReadArrayElement<TValue>(Values, idx);
                return true;
            }

            item = default;
            return false;
        }

        internal bool TryGetFirstValue<TValue>(TKey key, out TValue item, out HashMapIterator<TKey> it)
            where TValue : unmanaged
        {
            it.Key = key;

            if (AllocatedIndex <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            var bucket = GetBucket(it.Key);
            it.EntryIndex = it.NextEntryIndex = Buckets[bucket];

            return TryGetNextValue(out item, ref it);
        }

        internal bool TryGetNextValue<TValue>(out TValue item, ref HashMapIterator<TKey> it)
            where TValue : unmanaged
        {
            var entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;

            if (entryIdx < 0 || entryIdx >= Capacity)
            {
                item = default;
                return false;
            }

            var next = Next;
            var keys = Keys;

            while (!UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(it.Key))
            {
                entryIdx = next[entryIdx];
                if ((uint)entryIdx >= (uint)Capacity)
                {
                    item = default;
                    return false;
                }
            }

            it.NextEntryIndex = next[entryIdx];
            it.EntryIndex = entryIdx;
            item = UnsafeUtility.ReadArrayElement<TValue>(Values, entryIdx);
            return true;
        }

        internal NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TKey>(Count, allocator, NativeArrayOptions.UninitializedMemory);

            var keys = Keys;
            var buckets = Buckets;
            var next = Next;

            for (int i = 0, count = 0, max = result.Length, capacity = BucketCapacity; i < capacity && count < max; i++)
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
            var result = CollectionHelper.CreateNativeArray<TValue>(Count, allocator, NativeArrayOptions.UninitializedMemory);

            var values = Values;
            var buckets = Buckets;
            var next = Next;

            for (int i = 0, count = 0, max = result.Length, capacity = BucketCapacity; i < capacity && count < max; ++i)
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
            var result = new NativeKeyValueArrays<TKey, TValue>(Count, allocator, NativeArrayOptions.UninitializedMemory);

            var keys = Keys;
            var values = Values;
            var buckets = Buckets;
            var next = Next;

            for (int i = 0, count = 0, max = result.Length, capacity = BucketCapacity; i < capacity && count < max; ++i)
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

            Check.Assume(FirstFreeIdx == -1, "Trying to RemoveRangeShiftDown on map with holes. Call Flatten() first.");
            Check.Assume(start >= 0 && start < Count);
            Check.Assume(length >= 0 && start + length <= Count);

            var keys = Keys;
            var values = Values;

            var shift = Count - length - start;

            // var shift = count - le
            UnsafeUtility.MemMove(keys + start, keys + start + length, UnsafeUtility.SizeOf<TKey>() * shift);
            UnsafeUtility.MemMove(values + (start * SizeOfTValue), values + ((start + length) * SizeOfTValue), shift * SizeOfTValue);

            UnsafeUtility.MemSet(Buckets, 0xff, BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet((Next + Count) - length, 0xff, length * sizeof(int)); // only need to clear replaced elements

            AllocatedIndex -= length;
            Count -= length;

            UnsafeUtility.MemClear(keys + Count, (long)length * sizeof(TKey));
            UnsafeUtility.MemClear(values + ((long)Count * SizeOfTValue), (long)length * SizeOfTValue);

            var buckets = Buckets;
            var next = Next;

            for (var idx = 0; idx < Count; idx++)
            {
                var bucket = keys[idx].GetHashCode() & BucketCapacityMask;
                next[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearEntryData(int index)
        {
            // Only this removed slot is dead. Other live slots can be above Count.
            UnsafeUtility.MemClear(Keys + index, sizeof(TKey));
            UnsafeUtility.MemClear(Values + ((long)index * SizeOfTValue), SizeOfTValue);
        }

        private int AddNoCollideNoAlloc(in TKey key)
        {
            Check.Assume(AllocatedIndex < Capacity || FirstFreeIdx >= 0);

            var idx = FirstFreeIdx;

            if (idx >= 0)
            {
                FirstFreeIdx = Next[idx];
            }
            else
            {
                idx = AllocatedIndex++;
            }

            CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(Keys, idx, key);
            var bucket = GetBucket(key);

            var next = Next;
            next[idx] = Buckets[bucket];
            Buckets[bucket] = idx;
            Count++;

            return idx;
        }

        private static int AddNewKey(DynamicBuffer<byte> buffer, ref DynamicHashMapHelper<TKey>* data, in TKey key)
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

        internal static int CalculateDataSize(int capacity, int sizeOfTValue, out DynamicHashMapLayout layout)
        {
            Check.Assume(TryCalculateDataSize(capacity, sizeOfTValue, out layout), "Invalid DynamicHashMap layout.");
            return layout.TotalSize;
        }

        internal static bool TryCalculateDataSize(int capacity, int sizeOfTValue, out DynamicHashMapLayout layout)
        {
            layout = default;

            if (capacity <= 0 || sizeOfTValue < 0 || capacity > 0x20000000)
            {
                return false;
            }

            var bucketCapacity = math.ceilpow2(GetBucketSize(capacity));
            return TryCalculateDataSize(capacity, bucketCapacity, sizeOfTValue, out layout);
        }

        private static int CalculateDataSize(int capacity, int bucketCapacity, int sizeOfTValue, out DynamicHashMapLayout layout)
        {
            Check.Assume(TryCalculateDataSize(capacity, bucketCapacity, sizeOfTValue, out layout), "Invalid DynamicHashMap layout.");
            return layout.TotalSize;
        }

        private static bool TryCalculateDataSize(int capacity, int bucketCapacity, int sizeOfTValue, out DynamicHashMapLayout layout)
        {
            layout = default;

            if (capacity <= 0 || bucketCapacity <= 0 || sizeOfTValue < 0)
            {
                return false;
            }

            var sizeOfTKey = sizeof(TKey);
            var sizeOfInt = sizeof(int);

            var valuesOffset = Align(sizeof(DynamicHashMapHelper<TKey>), 16);
            var valuesSize = (long)sizeOfTValue * capacity;

            var keysOffset = Align(valuesOffset + valuesSize, UnsafeUtility.AlignOf<TKey>());
            var keysSize = (long)sizeOfTKey * capacity;

            var nextOffset = Align(keysOffset + keysSize, UnsafeUtility.AlignOf<int>());
            var nextSize = (long)sizeOfInt * capacity;

            var bucketOffset = Align(nextOffset + nextSize, UnsafeUtility.AlignOf<int>());
            var bucketSize = (long)sizeOfInt * bucketCapacity;

            var totalSize = bucketOffset + bucketSize;

            if (valuesOffset > int.MaxValue || keysOffset > int.MaxValue || nextOffset > int.MaxValue || bucketOffset > int.MaxValue ||
                totalSize <= 0 || totalSize > int.MaxValue)
            {
                return false;
            }

            layout = new DynamicHashMapLayout
            {
                ValuesOffset = (int)valuesOffset,
                KeysOffset = (int)keysOffset,
                NextOffset = (int)nextOffset,
                BucketsOffset = (int)bucketOffset,
                Capacity = capacity,
                BucketCapacity = bucketCapacity,
                SizeOfTValue = sizeOfTValue,
                TotalSize = (int)totalSize,
            };

            return true;
        }

        private static long Align(long size, int alignment)
        {
            var mask = alignment - 1L;
            return (size + mask) & ~mask;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void CheckNoHolesForAddBatchUnsafe()
        {
            if (FirstFreeIdx != -1 || AllocatedIndex != Count)
            {
                throw new InvalidOperationException("AddBatchUnsafe requires a map with no holes. Call Flatten() first.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void CheckIteratorKey(HashMapIterator<TKey> it)
        {
            if (!UnsafeUtility.ReadArrayElement<TKey>(Keys, it.EntryIndex).Equals(it.Key))
            {
                ThrowInvalidIterator();
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowInvalidIterator()
        {
            throw new ArgumentException("Iterator is invalid.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDoesNotExist(TKey key)
        {
            if (Find(key) != -1)
            {
                throw new ArgumentException($"An item with the same key has already been added: {key}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckNoDuplicateKey(TKey* keys, int* next, int bucketHead, TKey key, DynamicHashMapDuplicatePolicy duplicatePolicy)
        {
            if (duplicatePolicy == DynamicHashMapDuplicatePolicy.AllowDuplicateKeys)
            {
                return;
            }

            for (var entryIdx = bucketHead; entryIdx != -1; entryIdx = next[entryIdx])
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(key))
                {
                    throw new ArgumentException($"An item with the same key has already been added: {key}");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIndexOutOfBounds(int idx)
        {
            if ((uint)idx >= (uint)Capacity)
            {
                throw new InvalidOperationException($"Internal HashMap error. idx {idx}");
            }
        }

        internal struct Enumerator
        {
            private const int DenseTraversal = -1;

            [NativeDisableUnsafePtrRestriction]
            internal DynamicHashMapHelper<TKey>* Data;
            internal int Index;
            internal int BucketIndex;
            internal int NextIndex;

            internal Enumerator(DynamicHashMapHelper<TKey>* data)
            {
                Data = data;
                Index = -1;
                BucketIndex = data->IsDense ? DenseTraversal : 0;
                NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool MoveNext()
            {
                if (BucketIndex == DenseTraversal)
                {
                    var index = Index + 1;

                    if (index < Data->Count)
                    {
                        Index = index;
                        return true;
                    }

                    Index = -1;
                    BucketIndex = Data->BucketCapacity;
                    return false;
                }

                var next = Data->Next;

                if (NextIndex != -1)
                {
                    Index = NextIndex;
                    NextIndex = next[NextIndex];
                    return true;
                }

                var buckets = Data->Buckets;

                for (int i = BucketIndex, num = Data->BucketCapacity; i < num; ++i)
                {
                    var idx = buckets[i];

                    if (idx != -1)
                    {
                        Index = idx;
                        BucketIndex = i + 1;
                        NextIndex = next[idx];

                        return true;
                    }
                }

                Index = -1;
                BucketIndex = Data->BucketCapacity;
                NextIndex = -1;
                return false;
            }

            internal void Reset()
            {
                Index = -1;
                BucketIndex = Data->IsDense ? DenseTraversal : 0;
                NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal KVPair<TKey, TValue> GetCurrent<TValue>()
                where TValue : unmanaged
            {
                return new KVPair<TKey, TValue>
                {
                    Data = Data,
                    Index = Index,
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal TKey GetCurrentKey()
            {
                if (Index != -1)
                {
                    return Data->Keys[Index];
                }

                return default;
            }
        }
    }
}
