namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Internal;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public unsafe struct NativeUntypedHashMap<TKey> : INativeDisposable
        where TKey : unmanaged, IEquatable<TKey>
    {
        public const int DefaultMinGrowth = 256;

        [NativeDisableUnsafePtrRestriction]
        internal NativeUntypedHashMapHelper<TKey>* data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        internal AtomicSafetyHandle m_Safety;
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeUntypedHashMap<TKey>>();
#endif

        public NativeUntypedHashMap(int capacity, AllocatorManager.AllocatorHandle allocator, int minGrowth = DefaultMinGrowth)
        {
            data = NativeUntypedHashMapHelper<TKey>.Alloc(capacity, capacity, minGrowth, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionChecks.CheckAllocator(allocator);
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<TKey>())
            {
                AtomicSafetyHandle.SetNestedContainer(m_Safety, true);
            }

            CollectionHelper.SetStaticSafetyId<NativeUntypedHashMap<TKey>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data != null && data->IsCreated;
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!IsCreated)
                {
                    return true;
                }

                CheckRead();
                return data->IsEmpty;
            }
        }

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return data->Count;
            }
        }

        /// <summary>
        /// Capacity cannot shrink.
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                CheckRead();
                return data->Capacity;
            }

            set
            {
                CheckWrite();
                data->Resize(value);
            }
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
#endif
            if (!IsCreated)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif

            NativeUntypedHashMapHelper<TKey>.Free(data);
            data = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
#endif
            if (!IsCreated)
            {
                return inputDeps;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var jobHandle = new NativeUntypedHashMapDisposeJob<TKey>
            {
                Data = new NativeUntypedHashMapDispose<TKey>
                {
                    Buffer = data,
                    m_Safety = m_Safety,
                },
            }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(m_Safety);
#else
            var jobHandle = new NativeUntypedHashMapDisposeJob<TKey>
            {
                Data = new NativeUntypedHashMapDispose<TKey>
                {
                    Buffer = this.data,
                },
            }.Schedule(inputDeps);
#endif

            data = null;

            return jobHandle;
        }

        public void Clear()
        {
            CheckWrite();
            data->Clear();
        }

        public void AddOrSet<TValue>(TKey key, TValue item)
            where TValue : unmanaged
        {
            CheckWrite();
            data->AddOrSet(key, item);
        }

        /// <summary>
        /// The returned reference aliases map storage. Consume immediately; any later write, clear, or capacity change invalidates it.
        /// </summary>
        public ref TValue GetOrAddRefUnsafe<TValue>(TKey key, TValue defaultValue = default)
            where TValue : unmanaged
        {
            CheckWrite();

            var idx = data->Find(key);
            if (idx == -1)
            {
                idx = data->AddUnique(key, defaultValue);
            }

            return ref data->GetValue<TValue>(idx);
        }

        public readonly bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            CheckRead();
            return data->TryGetValue(key, out item);
        }

        public readonly bool ContainsKey(TKey key)
        {
            CheckRead();
            return data->Find(key) != -1;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
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

        internal int BucketCapacity => BucketCapacityMask + 1;

        internal bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer != null;
        }

        internal bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Count == 0;
        }

        internal static NativeUntypedHashMapHelper<TKey>* Alloc(int capacity, int dataCapacity, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            var data = (NativeUntypedHashMapHelper<TKey>*)CollectionMemory.Allocate(
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
            CollectionMemory.Free(data, data->Allocator);
        }

        internal void Init(int capacity, int dataCapacity, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            Count = 0;
            Log2MinGrowth = (byte)(32 - math.lzcnt(math.max(1, minGrowth) - 1));

            capacity = CalcCapacityCeilPow2(0, capacity, Log2MinGrowth);
            dataCapacity = CalcCapacityCeilPow2(0, dataCapacity, Log2MinGrowth);

            var bucketCapacity = GetBucketSize(capacity);
            var totalSize = CalculateDataSize(capacity, bucketCapacity, dataCapacity, out var keyOffset, out var nextOffset, out var bucketOffset,
                out var typeOffset, out var dataOffset);

            Buffer = (byte*)CollectionMemory.Allocate(totalSize, JobsUtility.CacheLineSize, allocator);
            Values = Buffer;
            Keys = (TKey*)(Buffer + keyOffset);
            Next = (int*)(Buffer + nextOffset);
            Buckets = (int*)(Buffer + bucketOffset);
            Types = (int*)(Buffer + typeOffset);
            Data = (int*)(Buffer + dataOffset);

            Capacity = capacity;
            DataCapacity = dataCapacity;
            BucketCapacityMask = bucketCapacity - 1;
            DataAllocatedIndex = 0;
            Allocator = allocator;

            UnsafeUtility.MemSet(Buckets, 0xff, BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet(Next, 0xff, Capacity * sizeof(int));
        }

        internal void Dispose()
        {
            CollectionMemory.Free(Buffer, Allocator);

            Buffer = null;
            Values = null;
            Keys = null;
            Next = null;
            Buckets = null;
            Types = null;
            Data = null;
            Count = 0;
            Capacity = 0;
            DataCapacity = 0;
            BucketCapacityMask = 0;
            DataAllocatedIndex = 0;
        }

        internal void Clear()
        {
            UnsafeUtility.MemSet(Buckets, 0xff, BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet(Next, 0xff, Capacity * sizeof(int));

            Count = 0;
            DataAllocatedIndex = 0;
        }

        internal void Resize(int newCapacity)
        {
            // This hashmap doesn't allow shrinking
            if (newCapacity <= Capacity)
            {
                return;
            }

            var newBucketCapacity = math.ceilpow2(GetBucketSize(newCapacity));
            Resize(newCapacity, newBucketCapacity);
        }

        internal void Resize(int newCapacity, int newBucketCapacity)
        {
            Assert.IsTrue(newCapacity > Capacity);

            var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, DataCapacity, out var keyOffset, out var nextOffset, out var bucketOffset,
                out var typeOffset, out var dataOffset);

            var newBuffer = (byte*)CollectionMemory.Allocate(totalSize, JobsUtility.CacheLineSize, Allocator);
            var newValues = newBuffer;
            var newKeys = (TKey*)(newBuffer + keyOffset);
            var newNext = (int*)(newBuffer + nextOffset);
            var newBuckets = (int*)(newBuffer + bucketOffset);
            var newTypes = (int*)(newBuffer + typeOffset);
            var newData = (int*)(newBuffer + dataOffset);

            var oldCapacity = Capacity;
            var oldBucketCapacity = BucketCapacity;
            var oldDataCapacity = DataCapacity;

            var oldKeys = Keys;
            var oldNext = Next;
            var oldBuckets = Buckets;

            UnsafeUtility.MemCpy(newValues, Values, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newKeys, oldKeys, oldCapacity * sizeof(TKey));
            UnsafeUtility.MemCpy(newTypes, Types, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newData, Data, oldDataCapacity * sizeof(int));

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

            CollectionMemory.Free(Buffer, Allocator);

            Buffer = newBuffer;
            Values = newValues;
            Keys = newKeys;
            Next = newNext;
            Buckets = newBuckets;
            Types = newTypes;
            Data = newData;
            Capacity = newCapacity;
            BucketCapacityMask = newBucketCapacity - 1;
        }

        internal void ResizeData(int newCapacity)
        {
            // This hashmap doesn't allow shrinking
            if (newCapacity <= DataCapacity)
            {
                return;
            }

            var bucketCapacity = BucketCapacity;
            var totalSize = CalculateDataSize(Capacity, bucketCapacity, newCapacity, out var keyOffset, out var nextOffset, out var bucketOffset,
                out var typeOffset, out var dataOffset);

            var newBuffer = (byte*)CollectionMemory.Allocate(totalSize, JobsUtility.CacheLineSize, Allocator);
            var newValues = newBuffer;
            var newKeys = (TKey*)(newBuffer + keyOffset);
            var newNext = (int*)(newBuffer + nextOffset);
            var newBuckets = (int*)(newBuffer + bucketOffset);
            var newTypes = (int*)(newBuffer + typeOffset);
            var newData = (int*)(newBuffer + dataOffset);

            var oldCapacity = Capacity;
            var oldDataCapacity = DataCapacity;

            UnsafeUtility.MemCpy(newValues, Values, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newKeys, Keys, oldCapacity * sizeof(TKey));
            UnsafeUtility.MemCpy(newNext, Next, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newBuckets, Buckets, bucketCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newTypes, Types, oldCapacity * sizeof(int));
            UnsafeUtility.MemCpy(newData, Data, oldDataCapacity * sizeof(int));

            CollectionMemory.Free(Buffer, Allocator);

            Buffer = newBuffer;
            Values = newValues;
            Keys = newKeys;
            Next = newNext;
            Buckets = newBuckets;
            Types = newTypes;
            Data = newData;
            DataCapacity = newCapacity;
        }

        internal void AddOrSet<TValue>(in TKey key, TValue value)
            where TValue : unmanaged
        {
            var idx = Find(key);
            var isLarge = sizeof(TValue) > sizeof(int);
            var add = idx == -1;

            if (add)
            {
                if (Count == Capacity)
                {
                    var newCap = CalcCapacityCeilPow2(Count, Capacity + (1 << Log2MinGrowth), Log2MinGrowth);
                    Resize(newCap);
                }

                idx = Count++;

                CheckIndexOutOfBounds(idx);

                UnsafeUtility.WriteArrayElement(Keys, idx, key);
                UnsafeUtility.WriteArrayElement(Types, idx, BurstRuntime.GetHashCode32<TValue>());

                var bucket = GetBucket(key);

                // Add the index to the hash-map
                var next = Next;
                next[idx] = Buckets[bucket];
                Buckets[bucket] = idx;
            }
            else
            {
                CheckType<TValue>(idx);
            }

            if (isLarge)
            {
                int dataAllocatedIndex;

                // Sets don't need to allocate, element should already exist
                if (add)
                {
                    Check.Assume(sizeof(TValue) % sizeof(int) == 0);

                    DataAllocatedIndex = AlignDataAllocatedIndex<TValue>(DataAllocatedIndex);

                    var minNewCapacity = DataAllocatedIndex + (sizeof(TValue) / sizeof(int));
                    if (minNewCapacity > DataCapacity)
                    {
                        var newCap = DataCapacity;
                        do
                        {
                            newCap = CalcCapacityCeilPow2(newCap + (1 << Log2MinGrowth), Log2MinGrowth);
                        }
                        while (newCap < minNewCapacity);

                        ResizeData(newCap);
                    }

                    dataAllocatedIndex = DataAllocatedIndex;

                    var dst = (int*)Values + idx;
                    *dst = DataAllocatedIndex;

                    DataAllocatedIndex += sizeof(TValue) / sizeof(int);
                }
                else
                {
                    // Set, just read the stored address
                    dataAllocatedIndex = *((int*)Values + idx);
                }

                var ptr = Data + dataAllocatedIndex;
                UnsafeUtility.MemCpy(ptr, &value, sizeof(TValue));
            }
            else
            {
                var dst = (TValue*)(Values + (idx * sizeof(int)));
                *dst = value;
            }
        }

        internal ref TValue GetValue<TValue>(int idx)
            where TValue : unmanaged
        {
            CheckType<TValue>(idx);

            var isLarge = sizeof(TValue) > sizeof(int);
            if (isLarge)
            {
                var dst = (int*)Values + idx;
                var dataAllocatedIndex = *dst;

                return ref UnsafeUtility.AsRef<TValue>(Data + dataAllocatedIndex);
            }

            return ref UnsafeUtility.AsRef<TValue>(Values + (idx * sizeof(int)));
        }

        internal int AddUnique<TValue>(in TKey key, TValue value)
            where TValue : unmanaged
        {
            CheckDoesNotExist(key);

            // Allocate an entry from the free list
            if (Count == Capacity)
            {
                var newCap = CalcCapacityCeilPow2(Count, Capacity + (1 << Log2MinGrowth), Log2MinGrowth);
                Resize(newCap);
            }

            var idx = Count++;

            CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(Keys, idx, key);
            UnsafeUtility.WriteArrayElement(Types, idx, BurstRuntime.GetHashCode32<TValue>());

            var bucket = GetBucket(key);

            // Add the index to the hash-map
            var next = Next;
            next[idx] = Buckets[bucket];
            Buckets[bucket] = idx;

            var isLarge = sizeof(TValue) > sizeof(int);
            if (isLarge)
            {
                Check.Assume(sizeof(TValue) % sizeof(int) == 0);

                DataAllocatedIndex = AlignDataAllocatedIndex<TValue>(DataAllocatedIndex);

                var minNewCapacity = DataAllocatedIndex + (sizeof(TValue) / sizeof(int));
                if (minNewCapacity > DataCapacity)
                {
                    var newCap = DataCapacity;
                    do
                    {
                        newCap = CalcCapacityCeilPow2(newCap + (1 << Log2MinGrowth), Log2MinGrowth);
                    }
                    while (newCap < minNewCapacity);

                    ResizeData(newCap);
                }

                var ptr = Data + DataAllocatedIndex;
                UnsafeUtility.MemCpy(ptr, &value, sizeof(TValue));

                var dst = (int*)Values + idx;
                *dst = DataAllocatedIndex;

                DataAllocatedIndex += sizeof(TValue) / sizeof(int);
            }
            else
            {
                var dst = (TValue*)(Values + (idx * sizeof(int)));
                *dst = value;
            }

            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetBucket(in TKey key)
        {
            return (int)((uint)key.GetHashCode() & BucketCapacityMask);
        }

        internal int Find(TKey key)
        {
            if (Count > 0)
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

        internal bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            var idx = Find(key);

            if (idx != -1)
            {
                item = GetValue<TValue>(idx);
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
            if (Find(key) != -1)
            {
                throw new ArgumentException($"An item with the same key has already been added: {key}");
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckType<TValue>(int idx)
            where TValue : unmanaged
        {
            var expected = BurstRuntime.GetHashCode32<TValue>();
            var actual = UnsafeUtility.ReadArrayElement<int>(Types, idx);
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
            Data.Dispose();
        }
    }

    [NativeContainer]
    internal unsafe struct NativeUntypedHashMapDispose<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        [NativeDisableUnsafePtrRestriction]
        internal NativeUntypedHashMapHelper<TKey>* Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        internal AtomicSafetyHandle m_Safety;
#endif

        internal void Dispose()
        {
            NativeUntypedHashMapHelper<TKey>.Free(Buffer);
        }
    }
}
