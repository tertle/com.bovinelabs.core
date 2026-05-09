// <copyright file="NativeUntypedHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Assertions;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary> An unordered, expandable untyped associative array. </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public unsafe struct NativeUntypedHashMap<TKey> : INativeDisposable
        where TKey : unmanaged, IEquatable<TKey>
    {
        public const int DefaultMinGrowth = 256;

        [NativeDisableUnsafePtrRestriction]
        internal NativeUntypedHashMapHelper<TKey>* data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeUntypedHashMap<TKey>>();
#endif

        /// <summary> Initializes a new instance of the <see cref="NativeUntypedHashMap{TKey}" /> struct. </summary>
        /// <param name="capacity"> The number of key-value pairs that should fit in the initial allocation. </param>
        /// <param name="allocator"> The allocator to use. </param>
        /// <param name="minGrowth"> The min growth of the hashmap. </param>
        public NativeUntypedHashMap(int capacity, AllocatorManager.AllocatorHandle allocator, int minGrowth = DefaultMinGrowth)
        {
            this.data = NativeUntypedHashMapHelper<TKey>.Alloc(capacity, capacity, minGrowth, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator);
            this.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<TKey>())
            {
                AtomicSafetyHandle.SetNestedContainer(this.m_Safety, true);
            }

            CollectionHelper.SetStaticSafetyId<NativeUntypedHashMap<TKey>>(ref this.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(this.m_Safety, true);
#endif
        }

        /// <summary> Gets a value indicating whether this hash map has been allocated (and not yet deallocated). </summary>
        /// <value> True if this hash map has been allocated (and not yet deallocated). </value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data != null && this.data->IsCreated;
        }

        /// <summary> Gets a value indicating whether this hash map is empty. </summary>
        /// <value> True if this hash map is empty or if the map has not been constructed. </value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!this.IsCreated)
                {
                    return true;
                }

                this.CheckRead();
                return this.data->IsEmpty;
            }
        }

        /// <summary> Gets the current number of key-value pairs in this hash map. </summary>
        /// <returns> The current number of key-value pairs in this hash map. </returns>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.CheckRead();
                return this.data->Count;
            }
        }

        /// <summary> Gets or sets the number of key-value pairs that fit in the current allocation. </summary>
        /// <param name="value"> A new capacity. Must be larger than the current capacity. </param>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                this.CheckRead();
                return this.data->Capacity;
            }

            set
            {
                this.CheckWrite();
                this.data->Resize(value);
            }
        }

        /// <summary> Releases all resources (memory). </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!AtomicSafetyHandle.IsDefaultValue(this.m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(this.m_Safety);
            }
#endif
            if (!this.IsCreated)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref this.m_Safety);
#endif

            NativeUntypedHashMapHelper<TKey>.Free(this.data);
            this.data = null;
        }

        /// <summary> Creates and schedules a job that will dispose this hash map. </summary>
        /// <param name="inputDeps"> A job handle. The newly scheduled job will depend upon this handle. </param>
        /// <returns> The handle of a new job that will dispose this hash map. </returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!AtomicSafetyHandle.IsDefaultValue(this.m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(this.m_Safety);
            }
#endif
            if (!this.IsCreated)
            {
                return inputDeps;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var jobHandle = new NativeUntypedHashMapDisposeJob<TKey>
            {
                Data = new NativeUntypedHashMapDispose<TKey>
                {
                    Buffer = this.data,
                    m_Safety = this.m_Safety,
                },
            }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(this.m_Safety);
#else
            var jobHandle = new NativeUntypedHashMapDisposeJob<TKey>
            {
                Data = new NativeUntypedHashMapDispose<TKey>
                {
                    Buffer = this.data,
                },
            }.Schedule(inputDeps);
#endif

            this.data = null;

            return jobHandle;
        }

        /// <summary> Removes all key-value pairs. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public void Clear()
        {
            this.CheckWrite();
            this.data->Clear();
        }

        /// <summary> Adds or sets a key-value pair. </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <typeparam name="TValue"> The type of value. </typeparam>
        public void AddOrSet<TValue>(TKey key, TValue item)
            where TValue : unmanaged
        {
            this.CheckWrite();
            this.data->AddOrSet(key, item);
        }

        /// <summary> Gets a value if it exists otherwise adds it then returns it by ref. </summary>
        /// <remarks>
        /// Unsafe because the returned ref points directly into the hash map storage. Consume it immediately and do not keep or use it after any later
        /// write to the same hash map, such as add-or-set, get-or-add, clear, or capacity-changing operations.
        /// </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="defaultValue"> Value to use if the key doesn't exist. </param>
        /// <typeparam name="TValue"> The type of value. </typeparam>
        /// <returns> The value in the map. </returns>
        public ref TValue GetOrAddRefUnsafe<TValue>(TKey key, TValue defaultValue = default)
            where TValue : unmanaged
        {
            this.CheckWrite();

            var idx = this.data->Find(key);
            if (idx == -1)
            {
                idx = this.data->AddUnique(key, defaultValue);
            }

            return ref this.data->GetValue<TValue>(idx);
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <returns> True if the key was present. </returns>
        /// <typeparam name="TValue"> The type of value. </typeparam>
        public readonly bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            this.CheckRead();
            return this.data->TryGetValue(key, out item);
        }

        public readonly bool ContainsKey(TKey key)
        {
            this.CheckRead();
            return this.data->Find(key) != -1;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
#endif
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeUntypedHashMapHelper<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        [NativeDisableUnsafePtrRestriction]
        internal byte* Values;

        [NativeDisableUnsafePtrRestriction]
        internal TKey* Keys;

        [NativeDisableUnsafePtrRestriction]
        internal int* Next;

        [NativeDisableUnsafePtrRestriction]
        internal int* Buckets;

        [NativeDisableUnsafePtrRestriction]
        internal int* Types;

        [NativeDisableUnsafePtrRestriction]
        internal int* Data;

        [NativeDisableUnsafePtrRestriction]
        internal byte* Buffer;

        internal int Count;
        internal int Capacity;
        internal int DataCapacity;
        internal int BucketCapacityMask;
        internal int Log2MinGrowth;
        internal int DataAllocatedIndex;
        internal AllocatorManager.AllocatorHandle Allocator;

        internal int BucketCapacity => this.BucketCapacityMask + 1;

        internal bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Buffer != null;
        }

        internal bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Count == 0;
        }

        internal static NativeUntypedHashMapHelper<TKey>* Alloc(int capacity, int dataCapacity, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            var data = (NativeUntypedHashMapHelper<TKey>*)Memory.Unmanaged.Allocate(
                sizeof(NativeUntypedHashMapHelper<TKey>), UnsafeUtility.AlignOf<NativeUntypedHashMapHelper<TKey>>(), allocator);
            data->Init(capacity, dataCapacity, minGrowth, allocator);

            return data;
        }

        internal static void Free(NativeUntypedHashMapHelper<TKey>* data)
        {
            if (data == null)
            {
                throw new InvalidOperationException("Hash based container has yet to be created or has been destroyed!");
            }

            data->Dispose();
            Memory.Unmanaged.Free(data, data->Allocator);
        }

        internal void Init(int capacity, int dataCapacity, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            this.Count = 0;
            this.Log2MinGrowth = (byte)(32 - math.lzcnt(math.max(1, minGrowth) - 1));

            capacity = CalcCapacityCeilPow2(0, capacity, this.Log2MinGrowth);
            dataCapacity = CalcCapacityCeilPow2(0, dataCapacity, this.Log2MinGrowth);

            var bucketCapacity = GetBucketSize(capacity);
            var totalSize = CalculateDataSize(capacity, bucketCapacity, dataCapacity, out var keyOffset, out var nextOffset, out var bucketOffset,
                out var typeOffset, out var dataOffset);

            this.Buffer = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, allocator);
            this.Values = this.Buffer;
            this.Keys = (TKey*)(this.Buffer + keyOffset);
            this.Next = (int*)(this.Buffer + nextOffset);
            this.Buckets = (int*)(this.Buffer + bucketOffset);
            this.Types = (int*)(this.Buffer + typeOffset);
            this.Data = (int*)(this.Buffer + dataOffset);

            this.Capacity = capacity;
            this.DataCapacity = dataCapacity;
            this.BucketCapacityMask = bucketCapacity - 1;
            this.DataAllocatedIndex = 0;
            this.Allocator = allocator;

            UnsafeUtility.MemSet(this.Buckets, 0xff, this.BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet(this.Next, 0xff, this.Capacity * sizeof(int));
        }

        internal void Dispose()
        {
            Memory.Unmanaged.Free(this.Buffer, this.Allocator);

            this.Buffer = null;
            this.Values = null;
            this.Keys = null;
            this.Next = null;
            this.Buckets = null;
            this.Types = null;
            this.Data = null;
            this.Count = 0;
            this.Capacity = 0;
            this.DataCapacity = 0;
            this.BucketCapacityMask = 0;
            this.DataAllocatedIndex = 0;
        }

        internal void Clear()
        {
            UnsafeUtility.MemSet(this.Buckets, 0xff, this.BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet(this.Next, 0xff, this.Capacity * sizeof(int));

            this.Count = 0;
            this.DataAllocatedIndex = 0;
        }

        internal void Resize(int newCapacity)
        {
            // This hashmap doesn't allow shrinking
            if (newCapacity <= this.Capacity)
            {
                return;
            }

            var newBucketCapacity = math.ceilpow2(GetBucketSize(newCapacity));
            this.Resize(newCapacity, newBucketCapacity);
        }

        internal void Resize(int newCapacity, int newBucketCapacity)
        {
            Assert.IsTrue(newCapacity > this.Capacity);

            var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, this.DataCapacity, out var keyOffset, out var nextOffset, out var bucketOffset,
                out var typeOffset, out var dataOffset);

            var newBuffer = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, this.Allocator);
            var newValues = newBuffer;
            var newKeys = (TKey*)(newBuffer + keyOffset);
            var newNext = (int*)(newBuffer + nextOffset);
            var newBuckets = (int*)(newBuffer + bucketOffset);
            var newTypes = (int*)(newBuffer + typeOffset);
            var newData = (int*)(newBuffer + dataOffset);

            var oldCapacity = this.Capacity;
            var oldBucketCapacity = this.BucketCapacity;
            var oldDataCapacity = this.DataCapacity;

            var oldKeys = this.Keys;
            var oldNext = this.Next;
            var oldBuckets = this.Buckets;

            UnsafeUtility.MemCpy(newValues, this.Values, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newKeys, oldKeys, oldCapacity * sizeof(TKey));
            UnsafeUtility.MemCpy(newTypes, this.Types, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newData, this.Data, oldDataCapacity * sizeof(int));

            UnsafeUtility.MemCpy(newNext, oldNext, oldCapacity * sizeof(int));
            UnsafeUtility.MemSet(newNext + oldCapacity, 0xff, (newCapacity - oldCapacity) * sizeof(int));

            // Re-hash the buckets, first clear the new bucket list, then insert all values from the old list.
            UnsafeUtility.MemSet(newBuckets, 0xff, newBucketCapacity * sizeof(int));

            for (var bucket = 0; bucket < oldBucketCapacity; ++bucket)
            {
                while (oldBuckets[bucket] >= 0)
                {
                    var curEntry = oldBuckets[bucket];
                    oldBuckets[bucket] = newNext[curEntry];
                    var newBucket = (int)((uint)oldKeys[curEntry].GetHashCode() & (newBucketCapacity - 1));
                    newNext[curEntry] = newBuckets[newBucket];
                    newBuckets[newBucket] = curEntry;
                }
            }

            Memory.Unmanaged.Free(this.Buffer, this.Allocator);

            this.Buffer = newBuffer;
            this.Values = newValues;
            this.Keys = newKeys;
            this.Next = newNext;
            this.Buckets = newBuckets;
            this.Types = newTypes;
            this.Data = newData;
            this.Capacity = newCapacity;
            this.BucketCapacityMask = newBucketCapacity - 1;
        }

        internal void ResizeData(int newCapacity)
        {
            // This hashmap doesn't allow shrinking
            if (newCapacity <= this.DataCapacity)
            {
                return;
            }

            var bucketCapacity = this.BucketCapacity;
            var totalSize = CalculateDataSize(this.Capacity, bucketCapacity, newCapacity, out var keyOffset, out var nextOffset, out var bucketOffset,
                out var typeOffset, out var dataOffset);

            var newBuffer = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, this.Allocator);
            var newValues = newBuffer;
            var newKeys = (TKey*)(newBuffer + keyOffset);
            var newNext = (int*)(newBuffer + nextOffset);
            var newBuckets = (int*)(newBuffer + bucketOffset);
            var newTypes = (int*)(newBuffer + typeOffset);
            var newData = (int*)(newBuffer + dataOffset);

            var oldCapacity = this.Capacity;
            var oldDataCapacity = this.DataCapacity;

            UnsafeUtility.MemCpy(newValues, this.Values, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newKeys, this.Keys, oldCapacity * sizeof(TKey));
            UnsafeUtility.MemCpy(newNext, this.Next, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newBuckets, this.Buckets, bucketCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newTypes, this.Types, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newData, this.Data, oldDataCapacity * sizeof(int));

            Memory.Unmanaged.Free(this.Buffer, this.Allocator);

            this.Buffer = newBuffer;
            this.Values = newValues;
            this.Keys = newKeys;
            this.Next = newNext;
            this.Buckets = newBuckets;
            this.Types = newTypes;
            this.Data = newData;
            this.DataCapacity = newCapacity;
        }

        internal void AddOrSet<TValue>(in TKey key, TValue value)
            where TValue : unmanaged
        {
            var idx = this.Find(key);
            var isLarge = sizeof(TValue) > sizeof(int);
            var add = idx == -1;

            if (add)
            {
                if (this.Count == this.Capacity)
                {
                    var newCap = CalcCapacityCeilPow2(this.Count, this.Capacity + (1 << this.Log2MinGrowth), this.Log2MinGrowth);
                    this.Resize(newCap);
                }

                idx = this.Count++;

                this.CheckIndexOutOfBounds(idx);

                UnsafeUtility.WriteArrayElement(this.Keys, idx, key);
                UnsafeUtility.WriteArrayElement(this.Types, idx, BurstRuntime.GetHashCode32<TValue>());

                var bucket = this.GetBucket(key);

                // Add the index to the hash-map
                var next = this.Next;
                next[idx] = this.Buckets[bucket];
                this.Buckets[bucket] = idx;
            }
            else
            {
                this.CheckType<TValue>(idx);
            }

            if (isLarge)
            {
                int dataAllocatedIndex;

                // Sets don't need to allocate, element should already exist
                if (add)
                {
                    Check.Assume(sizeof(TValue) % sizeof(int) == 0);

                    this.DataAllocatedIndex = AlignDataAllocatedIndex<TValue>(this.DataAllocatedIndex);

                    var minNewCapacity = this.DataAllocatedIndex + (sizeof(TValue) / sizeof(int));
                    if (minNewCapacity > this.DataCapacity)
                    {
                        var newCap = this.DataCapacity;
                        do
                        {
                            newCap = CalcCapacityCeilPow2(newCap + (1 << this.Log2MinGrowth), this.Log2MinGrowth);
                        }
                        while (newCap < minNewCapacity);

                        this.ResizeData(newCap);
                    }

                    dataAllocatedIndex = this.DataAllocatedIndex;

                    var dst = (int*)this.Values + idx;
                    *dst = this.DataAllocatedIndex;

                    this.DataAllocatedIndex += sizeof(TValue) / sizeof(int);
                }
                else
                {
                    // Set, just read the stored address
                    dataAllocatedIndex = *((int*)this.Values + idx);
                }

                var ptr = this.Data + dataAllocatedIndex;
                UnsafeUtility.MemCpy(ptr, &value, sizeof(TValue));
            }
            else
            {
                var dst = (TValue*)(this.Values + (idx * sizeof(int)));
                *dst = value;
            }
        }

        internal ref TValue GetValue<TValue>(int idx)
            where TValue : unmanaged
        {
            this.CheckType<TValue>(idx);

            var isLarge = sizeof(TValue) > sizeof(int);
            if (isLarge)
            {
                var dst = (int*)this.Values + idx;
                var dataAllocatedIndex = *dst;

                return ref UnsafeUtility.AsRef<TValue>(this.Data + dataAllocatedIndex);
            }

            return ref UnsafeUtility.AsRef<TValue>(this.Values + (idx * sizeof(int)));
        }

        internal int AddUnique<TValue>(in TKey key, TValue value)
            where TValue : unmanaged
        {
            this.CheckDoesNotExist(key);

            // Allocate an entry from the free list
            if (this.Count == this.Capacity)
            {
                var newCap = CalcCapacityCeilPow2(this.Count, this.Capacity + (1 << this.Log2MinGrowth), this.Log2MinGrowth);
                this.Resize(newCap);
            }

            var idx = this.Count++;

            this.CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(this.Keys, idx, key);
            UnsafeUtility.WriteArrayElement(this.Types, idx, BurstRuntime.GetHashCode32<TValue>());

            var bucket = this.GetBucket(key);

            // Add the index to the hash-map
            var next = this.Next;
            next[idx] = this.Buckets[bucket];
            this.Buckets[bucket] = idx;

            var isLarge = sizeof(TValue) > sizeof(int);
            if (isLarge)
            {
                Check.Assume(sizeof(TValue) % sizeof(int) == 0);

                this.DataAllocatedIndex = AlignDataAllocatedIndex<TValue>(this.DataAllocatedIndex);

                var minNewCapacity = this.DataAllocatedIndex + (sizeof(TValue) / sizeof(int));
                if (minNewCapacity > this.DataCapacity)
                {
                    var newCap = this.DataCapacity;
                    do
                    {
                        newCap = CalcCapacityCeilPow2(newCap + (1 << this.Log2MinGrowth), this.Log2MinGrowth);
                    }
                    while (newCap < minNewCapacity);

                    this.ResizeData(newCap);
                }

                var ptr = this.Data + this.DataAllocatedIndex;
                UnsafeUtility.MemCpy(ptr, &value, sizeof(TValue));

                var dst = (int*)this.Values + idx;
                *dst = this.DataAllocatedIndex;

                this.DataAllocatedIndex += sizeof(TValue) / sizeof(int);
            }
            else
            {
                var dst = (TValue*)(this.Values + (idx * sizeof(int)));
                *dst = value;
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
                item = this.GetValue<TValue>(idx);
                return true;
            }

            item = default;
            return false;
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

        private static long CalculateDataSize(
            int capacity, int bucketCapacity, int dataCapacity, out long outKeyOffset, out long outNextOffset, out long outBucketOffset, out long outTypeOffset,
            out long outDataOffset)
        {
            var sizeOfTKey = sizeof(TKey);
            var sizeOfInt = sizeof(int);
            var sizeOfTypeIndex = sizeof(int);
            var alignOfTKey = UnsafeUtility.AlignOf<TKey>();

            var valuesSize = (long)sizeOfInt * capacity;
            var keysSize = (long)sizeOfTKey * capacity;
            var nextSize = (long)sizeOfInt * capacity;
            var bucketSize = (long)sizeOfInt * bucketCapacity;
            var typeSize = (long)sizeOfTypeIndex * capacity;
            var dataSize = (long)sizeOfInt * dataCapacity;

            // Layout is:
            // Values (int[capacity]) -> Keys (TKey[capacity]) -> Next (int[capacity]) -> Buckets (int[bucketCapacity]) -> Types (int[capacity]) -> Data (int[dataCapacity])
            // Explicitly align each segment to avoid misaligned reads/writes on strict platforms.
            outKeyOffset = CollectionHelper.Align(valuesSize, alignOfTKey);
            outNextOffset = CollectionHelper.Align(outKeyOffset + keysSize, sizeOfInt);
            outBucketOffset = CollectionHelper.Align(outNextOffset + nextSize, sizeOfInt);
            outTypeOffset = CollectionHelper.Align(outBucketOffset + bucketSize, sizeOfInt);

            // Large values are stored in the Data segment; align the segment so values with higher alignment (e.g. 16) can be stored correctly.
            outDataOffset = CollectionHelper.Align(outTypeOffset + typeSize, 16);

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
        private void CheckType<TValue>(int idx)
            where TValue : unmanaged
        {
            var expected = BurstRuntime.GetHashCode32<TValue>();
            var actual = UnsafeUtility.ReadArrayElement<int>(this.Types, idx);
            if (!expected.Equals(actual))
            {
                throw new InvalidOperationException($"Type {actual} does not match stored {expected}");
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

    [BurstCompile]
    internal unsafe struct NativeUntypedHashMapDisposeJob<TKey> : IJob
        where TKey : unmanaged, IEquatable<TKey>
    {
        internal NativeUntypedHashMapDispose<TKey> Data;

        public void Execute()
        {
            this.Data.Dispose();
        }
    }

    [NativeContainer]
    internal unsafe struct NativeUntypedHashMapDispose<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        [NativeDisableUnsafePtrRestriction]
        internal NativeUntypedHashMapHelper<TKey>* Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        internal void Dispose()
        {
            NativeUntypedHashMapHelper<TKey>.Free(this.Buffer);
        }
    }
}
