// <copyright file="NativeKeyedMap.cs" company="BovineLabs">
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
    /// <remarks>The iteration order over the values associated with a key is an implementation detail. Do not rely upon any particular ordering.</remarks>
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    public struct UnsafeKeyedMapIterator
    {
        internal int Key;
        internal int NextEntryIndex;
    }

    [StructLayout(LayoutKind.Explicit)]
    [BurstCompatible]
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

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
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

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int), typeof(int) })]
        internal static void ReallocateHashMap<TValue>(KeyedMapData* data, int newCapacity, AllocatorManager.AllocatorHandle label)
            where TValue : unmanaged
        {
            if (data->KeyCapacity == newCapacity)
            {
                return;
            }

            CheckHashMapReallocateDoesNotShrink(data, newCapacity);

            int totalSize = CalculateDataSize<TValue>(newCapacity, data->BucketCapacity, out var keyOffset, out var nextOffset, out var bucketOffset);

            byte* newData = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, label);
            byte* newKeys = newData + keyOffset;
            byte* newNext = newData + nextOffset;
            byte* newBuckets = newData + bucketOffset;

            // The items are taken from a free-list and might not be tightly packed, copy all of the old capcity
            UnsafeUtility.MemCpy(newData, data->Values, data->KeyCapacity * UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpy(newKeys, data->Keys, data->KeyCapacity * UnsafeUtility.SizeOf<int>());
            UnsafeUtility.MemCpy(newNext, data->Next, data->KeyCapacity * UnsafeUtility.SizeOf<int>());
            UnsafeUtility.MemCpy(newBuckets, data->Buckets, data->BucketCapacity * UnsafeUtility.SizeOf<int>());

            for (int emptyNext = data->KeyCapacity; emptyNext < newCapacity; ++emptyNext)
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

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
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
        static void CheckHashMapReallocateDoesNotShrink(KeyedMapData* data, int newCapacity)
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
    [BurstCompatible]
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
        /// Initializes and returns an instance of UnsafeParallelMultiHashMap.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
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
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public bool IsCreated => this.buffer != null;

        /// <summary>
        /// Returns the number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value>The number of key-value pairs that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        /// <exception cref="Exception">Thrown if `value` is less than the current capacity.</exception>
        public int Capacity
        {
            get => this.buffer->KeyCapacity;
            set => KeyedMapData.ReallocateHashMap<TValue>(this.buffer, value, this.allocator);
        }

        public void Dispose()
        {
            KeyedMapData.DeallocateHashMap(buffer, allocator);
            buffer = null;
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
            int* buckets = (int*)this.buffer->Buckets;
            var nextPtrs = (int*)this.buffer->Next;

            nextPtrs[idx] = buckets[key];
            buckets[key] = idx;
        }

        /// <summary>
        /// Gets an iterator for a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">Outputs the associated value represented by the iterator.</param>
        /// <param name="it">Outputs an iterator.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetFirstValue(int key, out TValue item, out UnsafeKeyedMapIterator it)
        {
            CheckKeyOutOfBounds(this.buffer, key);

            it.Key = key;

            if (this.buffer->Length <= 0)
            {
                it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            var buckets = (int*)this.buffer->Buckets;
            it.NextEntryIndex = buckets[key];
            return TryGetNextValue(out item, ref it);
        }

        /// <summary>
        /// Advances an iterator to the next value associated with its key.
        /// </summary>
        /// <param name="item">Outputs the next value.</param>
        /// <param name="it">A reference to the iterator to advance.</param>
        /// <returns>True if the key was present and had another value.</returns>
        public bool TryGetNextValue(out TValue item, ref UnsafeKeyedMapIterator it)
        {
            int entryIdx = it.NextEntryIndex;
            if (entryIdx < 0/* || entryIdx >= this.Buffer->KeyCapacity*/)
            {
                it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            var nextPtrs = (int*)this.buffer->Next;
            it.NextEntryIndex = nextPtrs[entryIdx];

            // Read the value
            item = UnsafeUtility.ReadArrayElement<TValue>(this.buffer->Values, entryIdx);
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

        public int* GetUnsafeKeysPtr() => (int*)this.buffer->Keys;
        public TValue* GetUnsafeValuesPtr() => (TValue*)this.buffer->Values;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckKeyOutOfBounds(KeyedMapData* data, int key)
        {
            if (key < 0 || key >= data->BucketCapacity)
                throw new InvalidOperationException("key < 0 || key >= data->BucketCapacity");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckIndexOutOfBounds(KeyedMapData* data, int idx)
        {
            if (idx < 0 || idx >= data->KeyCapacity)
                throw new InvalidOperationException("Internal Map error");
        }
    }

    public struct NativeKeyedMap<TValue>
        where TValue : unmanaged
    {
        internal UnsafeKeyedMap<TValue> keyedMapData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<UnsafeKeyedMap<TValue>>();

#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif
#endif

        /// <summary>
        /// Returns a newly allocated multi hash map.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeKeyedMap(int capacity, int maxKey, AllocatorManager.AllocatorHandle allocator)
            : this(capacity, maxKey, allocator, 2)
        {
        }

        NativeKeyedMap(int capacity, int maxKey, AllocatorManager.AllocatorHandle allocator, int disposeSentinelStackDepth)
        {
            this = default;
            this.Initialize(capacity, maxKey, ref allocator, disposeSentinelStackDepth);
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public bool IsCreated => this.keyedMapData.IsCreated;

        /// <summary>
        /// Returns the number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value>The number of key-value pairs that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        /// <exception cref="Exception">Thrown if `value` is less than the current capacity.</exception>
        public int Capacity
        {
            get
            {
                CheckRead();
                return this.keyedMapData.Capacity;
            }

            set
            {
                CheckWrite();
                this.keyedMapData.Capacity = value;
            }
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            this.keyedMapData.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this hash map.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future */]
        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
#else
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref this.m_DisposeSentinel);
#endif
            var jobHandle = new UnsafeKeyedMapDataDisposeJob
                {
                    Data = new UnsafeKeyedMapDataDispose
                        { Buffer = this.keyedMapData.buffer, AllocatorLabel = this.keyedMapData.allocator, m_Safety = this.m_Safety },
                }
                .Schedule(inputDeps);

            AtomicSafetyHandle.Release(this.m_Safety);
#else
            var jobHandle = new UnsafeKeyedMapDataDisposeJob { Data = new UnsafeKeyedMapDataDispose { Buffer = this.keyedMapData.buffer, AllocatorLabel = this.keyedMapData.allocator } }.Schedule(inputDeps);
#endif
            this.keyedMapData.buffer = null;

            return jobHandle;
        }

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            this.CheckWrite();
            this.keyedMapData.Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>
        /// If a key-value pair with this key is already present, an additional separate key-value pair is added.
        /// </remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        public void Add(int key, TValue item)
        {
            this.CheckWrite();
            this.keyedMapData.Add(key, item);
        }

        /// <summary>
        /// Gets an iterator for a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">Outputs the associated value represented by the iterator.</param>
        /// <param name="it">Outputs an iterator.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetFirstValue(int key, out TValue item, out UnsafeKeyedMapIterator it)
        {
            CheckRead();
            return this.keyedMapData.TryGetFirstValue(key, out item, out it);
        }

        /// <summary>
        /// Advances an iterator to the next value associated with its key.
        /// </summary>
        /// <param name="item">Outputs the next value.</param>
        /// <param name="it">A reference to the iterator to advance.</param>
        /// <returns>True if the key was present and had another value.</returns>
        public bool TryGetNextValue(out TValue item, ref UnsafeKeyedMapIterator it)
        {
            CheckRead();
            return this.keyedMapData.TryGetNextValue(out item, ref it);
        }

        public void SetLength(int length)
        {
            this.CheckWrite();
            this.keyedMapData.SetLength(length);
        }

        public void RecalculateBuckets()
        {
            this.CheckWrite();
            this.keyedMapData.RecalculateBuckets();
        }

        public unsafe int* GetUnsafeKeysPtr() {

            this.CheckWrite();
            return this.keyedMapData.GetUnsafeKeysPtr();
        }

        public unsafe TValue* GetUnsafeValuesPtr()
        {
            this.CheckWrite();
            return this.keyedMapData.GetUnsafeValuesPtr();
        }

        public unsafe int* GetUnsafeReadOnlyKeysPtr() {

            this.CheckRead();
            return this.keyedMapData.GetUnsafeKeysPtr();
        }

        public unsafe TValue* GetUnsafeReadOnlyValuesPtr()
        {
            this.CheckRead();
            return this.keyedMapData.GetUnsafeValuesPtr();
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(AllocatorManager.AllocatorHandle) })]
        private void Initialize<U>(int capacity, int maxKey, ref U allocator, int disposeSentinelStackDepth)
            where U : unmanaged, AllocatorManager.IAllocator
        {
            this.keyedMapData = new UnsafeKeyedMap<TValue>(capacity, maxKey, allocator.Handle);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (allocator.IsCustomAllocator)
            {
                this.m_Safety = AtomicSafetyHandle.Create();
                this.m_DisposeSentinel = null;
            }
            else
            {
                DisposeSentinel.Create(out this.m_Safety, out this.m_DisposeSentinel, disposeSentinelStackDepth, allocator.ToAllocator);
            }

            CollectionHelper.SetStaticSafetyId<NativeKeyedMap<TValue>>(ref this.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(this.m_Safety, true);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
#endif
        }
    }
}
