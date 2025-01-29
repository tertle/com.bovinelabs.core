// <copyright file="UnsafeKeyedMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary>
    /// An iterator over all values associated with an individual key in a multi hash map.
    /// </summary>
    /// <remarks> The iteration order over the values associated with a key is an implementation detail. Do not rely upon any particular ordering. </remarks>
    public struct UnsafeKeyedMapIterator
    {
        internal int NextEntryIndex;

        public int EntryIndex { get; internal set; }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct KeyedMapData
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
        internal int BucketCapacity;

        [FieldOffset(40)]
        internal int Length;

        internal static void AllocateHashMap<TValue>(int length, int bucketLength, AllocatorManager.AllocatorHandle label, out KeyedMapData* data)
            where TValue : unmanaged
        {
            CollectionHelper.CheckIsUnmanaged<TValue>();

            data = (KeyedMapData*)Memory.Unmanaged.Allocate(sizeof(KeyedMapData), UnsafeUtility.AlignOf<KeyedMapData>(), label);

            data->Length = 0;
            data->KeyCapacity = length;
            data->BucketCapacity = bucketLength;

            var totalSize = CalculateDataSize<TValue>(length, bucketLength, out var keyOffset, out var nextOffset, out var bucketOffset);

            data->Values = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, label);
            data->Keys = data->Values + keyOffset;
            data->Next = data->Values + nextOffset;
            data->Buckets = data->Values + bucketOffset;
        }

        internal static void ReallocateHashMap<TValue>(KeyedMapData* data, int newCapacity, AllocatorManager.AllocatorHandle label)
            where TValue : unmanaged
        {
            if (data->KeyCapacity == newCapacity)
            {
                return;
            }

            CheckHashMapReallocateDoesNotShrink(data, newCapacity);

            var totalSize = CalculateDataSize<TValue>(newCapacity, data->BucketCapacity, out var keyOffset, out var nextOffset, out var bucketOffset);

            var newData = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, label);
            var newKeys = newData + keyOffset;
            var newNext = newData + nextOffset;
            var newBuckets = newData + bucketOffset;

            // The items are taken from a free-list and might not be tightly packed, copy all of the old capcity
            UnsafeUtility.MemCpy(newData, data->Values, data->KeyCapacity * UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpy(newKeys, data->Keys, data->KeyCapacity * UnsafeUtility.SizeOf<int>());
            UnsafeUtility.MemCpy(newNext, data->Next, data->KeyCapacity * UnsafeUtility.SizeOf<int>());
            UnsafeUtility.MemCpy(newBuckets, data->Buckets, data->BucketCapacity * UnsafeUtility.SizeOf<int>());

            for (var emptyNext = data->KeyCapacity; emptyNext < newCapacity; ++emptyNext)
            {
                ((int*)newNext)[emptyNext] = -1;
            }

            Memory.Unmanaged.Free(data->Values, label);

            data->Values = newData;
            data->Keys = newKeys;
            data->Next = newNext;
            data->Buckets = newBuckets;
            data->KeyCapacity = newCapacity;
        }

        internal static int GrowCapacity(int capacity)
        {
            if (capacity == 0)
            {
                return 1;
            }

            return capacity * 2;
        }

        internal static void DeallocateHashMap(KeyedMapData* data, AllocatorManager.AllocatorHandle allocator)
        {
            Memory.Unmanaged.Free(data->Values, allocator);
            Memory.Unmanaged.Free(data, allocator);
        }

        private static int CalculateDataSize<TValue>(int length, int bucketLength, out int keyOffset, out int nextOffset, out int bucketOffset)
            where TValue : unmanaged
        {
            var sizeOfTValue = UnsafeUtility.SizeOf<TValue>();
            var sizeOfInt = UnsafeUtility.SizeOf<int>();

            var valuesSize = CollectionHelper.Align(sizeOfTValue * length, JobsUtility.CacheLineSize);
            var keysSize = CollectionHelper.Align(sizeOfInt * length, JobsUtility.CacheLineSize);
            var nextSize = CollectionHelper.Align(sizeOfInt * length, JobsUtility.CacheLineSize);
            var bucketSize = CollectionHelper.Align(sizeOfInt * bucketLength, JobsUtility.CacheLineSize);
            var totalSize = valuesSize + keysSize + nextSize + bucketSize;

            keyOffset = 0 + valuesSize;
            nextOffset = keyOffset + keysSize;
            bucketOffset = nextOffset + nextSize;

            return totalSize;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckHashMapReallocateDoesNotShrink(KeyedMapData* data, int newCapacity)
        {
            if (data->KeyCapacity > newCapacity)
            {
                throw new Exception("Shrinking a native keyed map is not supported");
            }
        }
    }

    [BurstCompile]
    internal struct UnsafeKeyedMapDataDisposeJob : IJob
    {
        internal UnsafeKeyedMapDataDispose Data;

        public void Execute()
        {
            this.Data.Dispose();
        }
    }

    [NativeContainer]
    internal unsafe struct UnsafeKeyedMapDataDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal KeyedMapData* Buffer;

        internal AllocatorManager.AllocatorHandle AllocatorLabel;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        public void Dispose()
        {
            KeyedMapData.DeallocateHashMap(this.Buffer, this.AllocatorLabel);
        }
    }

    public unsafe struct UnsafeKeyedMap<TValue>
        where TValue : unmanaged
    {
        internal readonly AllocatorManager.AllocatorHandle allocator;

        [NativeDisableUnsafePtrRestriction]
        internal KeyedMapData* buffer;

        /// <summary>
        /// Initializes and returns an instance of UnsafeMultiHashMap.
        /// </summary>
        /// <param name="capacity"> The number of key-value pairs that should fit in the initial allocation. </param>
        /// <param name="allocator"> The allocator to use. </param>
        public UnsafeKeyedMap(int capacity, int maxKey, AllocatorManager.AllocatorHandle allocator)
        {
            Check.Assume(maxKey > 0);

            this.allocator = allocator;
            KeyedMapData.AllocateHashMap<TValue>(capacity, maxKey, allocator, out this.buffer);
            this.Clear();
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value> True if this hash map has been allocated (and not yet deallocated). </value>
        public bool IsCreated => this.buffer != null;

        /// <summary>
        /// Returns the number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value> The number of key-value pairs that fit in the current allocation. </value>
        /// <param name="value"> A new capacity. Must be larger than the current capacity. </param>
        /// <exception cref="Exception"> Thrown if `value` is less than the current capacity. </exception>
        public int Capacity
        {
            get => this.buffer->KeyCapacity;
            set => KeyedMapData.ReallocateHashMap<TValue>(this.buffer, value, this.allocator);
        }

        public void Dispose()
        {
            KeyedMapData.DeallocateHashMap(this.buffer, this.allocator);
            this.buffer = null;
        }

        public void Clear()
        {
            UnsafeUtility.MemSet(this.buffer->Next, 0xff, this.buffer->KeyCapacity * 4);
            UnsafeUtility.MemSet(this.buffer->Buckets, 0xff, this.buffer->BucketCapacity * 4);

            this.buffer->Length = 0;
        }

        public void Add(int key, TValue item)
        {
            CheckKeyOutOfBounds(this.buffer, key);

            // Allocate an entry from the free list
            if (this.buffer->Length >= this.buffer->KeyCapacity)
            {
                var newCap = KeyedMapData.GrowCapacity(this.buffer->KeyCapacity);
                KeyedMapData.ReallocateHashMap<TValue>(this.buffer, newCap, this.allocator);
            }

            var idx = this.buffer->Length++;

            CheckIndexOutOfBounds(this.buffer, idx);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(this.buffer->Keys, idx, key);
            UnsafeUtility.WriteArrayElement(this.buffer->Values, idx, item);

            // Add the index to the hash-map
            var buckets = (int*)this.buffer->Buckets;
            var nextPtrs = (int*)this.buffer->Next;

            nextPtrs[idx] = buckets[key];
            buckets[key] = idx;
        }

        /// <summary>
        /// Gets an iterator for a key.
        /// </summary>
        /// <param name="key"> The key. </param>
        /// <param name="item"> Outputs the associated value represented by the iterator. </param>
        /// <param name="it"> Outputs an iterator. </param>
        /// <returns> True if the key was present. </returns>
        public bool TryGetFirstValue(int key, out TValue item, out UnsafeKeyedMapIterator it)
        {
            CheckKeyOutOfBounds(this.buffer, key);

            it = default;

            if (this.buffer->Length <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            var buckets = (int*)this.buffer->Buckets;
            it.NextEntryIndex = buckets[key];
            return this.TryGetNextValue(out item, ref it);
        }

        /// <summary>
        /// Advances an iterator to the next value associated with its key.
        /// </summary>
        /// <param name="item"> Outputs the next value. </param>
        /// <param name="it"> A reference to the iterator to advance. </param>
        /// <returns> True if the key was present and had another value. </returns>
        public bool TryGetNextValue(out TValue item, ref UnsafeKeyedMapIterator it)
        {
            it.EntryIndex = it.NextEntryIndex;
            if (it.EntryIndex < 0 /* || entryIdx >= this.Buffer->KeyCapacity*/)
            {
                it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            var nextPtrs = (int*)this.buffer->Next;
            it.NextEntryIndex = nextPtrs[it.EntryIndex];

            // Read the value
            item = UnsafeUtility.ReadArrayElement<TValue>(this.buffer->Values, it.EntryIndex);
            return true;
        }

        public void SetLength(int length)
        {
            this.buffer->Length = length;
        }

        public void RecalculateBuckets()
        {
            var length = this.buffer->Length;

            // var data = hashMap.GetUnsafeBucketData();
            var buckets = (int*)this.buffer->Buckets;
            var nextPtrs = (int*)this.buffer->Next;
            var keys = (int*)this.buffer->Keys;

            for (var idx = 0; idx < length; idx++)
            {
                var key = keys[idx];
                CheckKeyOutOfBounds(this.buffer, key);
                nextPtrs[idx] = buckets[key];
                buckets[key] = idx;
            }
        }

        public int* GetUnsafeKeysPtr()
        {
            return (int*)this.buffer->Keys;
        }

        public TValue* GetUnsafeValuesPtr()
        {
            return (TValue*)this.buffer->Values;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckKeyOutOfBounds(KeyedMapData* data, int key)
        {
            if (key < 0 || key >= data->BucketCapacity)
            {
                throw new InvalidOperationException("key < 0 || key >= data->BucketCapacity");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckIndexOutOfBounds(KeyedMapData* data, int idx)
        {
            if (idx < 0 || idx >= data->KeyCapacity)
            {
                throw new InvalidOperationException("Internal Map error");
            }
        }
    }
}
