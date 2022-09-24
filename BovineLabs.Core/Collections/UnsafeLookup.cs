namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeLookupDebuggerTypeProxy<,>))]
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
    public unsafe struct UnsafeLookup<TKey, TValue> : INativeDisposable, IEnumerable<KeyValue<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeLookupData* buffer;
        internal AllocatorManager.AllocatorHandle allocatorLabel;

        public UnsafeLookup(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            this.allocatorLabel = allocator;
            UnsafeLookupData.AllocateLookup<TKey, TValue>(capacity, capacity * 2, allocator, out this.buffer);
            this.Clear();
        }

        public bool IsEmpty => !this.IsCreated || this.buffer->allocatedIndexLength <= 0;

        public bool IsCreated => this.buffer != null;

        public int Length => this.buffer->allocatedIndexLength;

        public int Capacity
        {
            get => buffer->keyCapacity;
            set => UnsafeLookupData.ReallocateLookup<TKey, TValue>(this.buffer, value, UnsafeLookupData.GetBucketSize(value), this.allocatorLabel);
        }

        /// <summary>
        /// Gets and sets values by key.
        /// </summary>
        /// <remarks>Getting a key that is not present will throw. Setting a key that is not already present will add the key.</remarks>
        /// <param name="key">The key to look up.</param>
        /// <value>The value associated with the key.</value>
        /// <exception cref="ArgumentException">For getting, thrown if the key was not present.</exception>
        public TValue this[TKey key]
        {
            get
            {
                this.TryGetValue(key, out var res);
                return res;
            }

            set
            {
                if (this.TryGetFirstValue(key, out _, out var iterator))
                {
                    this.SetValue(ref iterator, ref value);
                }
                else
                {
                    this.Add(key, value);
                }
            }
        }

        public void Clear()
        {
            UnsafeUtility.MemSet(this.buffer->buckets, 0xff, (this.buffer->bucketCapacityMask + 1) * 4);
            UnsafeUtility.MemSet(this.buffer->next, 0xff, this.buffer->keyCapacity * 4);
            this.buffer->allocatedIndexLength = 0;
        }

        public void Dispose()
        {
            UnsafeLookupData.DeallocateLookup(this.buffer, this.allocatorLabel);
            this.buffer = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafeLookupDisposeJob { Data = this.buffer, Allocator = this.allocatorLabel }.Schedule(inputDeps);
            this.buffer = null;
            return jobHandle;
        }

        public void Add(TKey key, TValue value)
        {
            this.AddBatch(&key, &value, 1);
        }

        public void AddBatch(NativeArray<TKey> keys, NativeArray<TValue> values)
        {
            CheckLengthsMatch(keys, values);
            this.AddBatch((TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public void AddBatch([NoAlias] TKey* keys, [NoAlias] TValue* values, int length)
        {
            length = CollectionHelper.AssumePositive(length);

            var oldLength = length;
            var newLength = oldLength + length;

            if (this.Capacity < newLength)
            {
                this.Capacity = newLength;
            }

            var keyPtr = ((TKey*)this.buffer->keys) + oldLength;
            var valuePtr = ((TValue*)this.buffer->values) + oldLength;

            UnsafeUtility.MemCpy(keyPtr, keys, length * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(valuePtr, values, length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)this.buffer->buckets;
            var nextPtrs = ((int*)this.buffer->next) + oldLength;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & this.buffer->bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = oldLength + idx;
            }

            this.buffer->allocatedIndexLength += length;
        }

        public void AddBatch(NativeArray<TKey> keys)
        {
            this.AddBatch((TKey*)keys.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public void AddBatch([NoAlias] TKey* keys, int length)
        {
            var oldLength = this.Length;
            var newLength = oldLength + length;

            if (Capacity < newLength)
            {
                Capacity = newLength;
            }

            var keyPtr = ((TKey*)this.buffer->keys) + oldLength;

            using (new Unity.Profiling.ProfilerMarker("MemCpy").Auto())
            {
                UnsafeUtility.MemCpy(keyPtr, keys, length * UnsafeUtility.SizeOf<TKey>());
            }

            var buckets = (int*)this.buffer->buckets;
            var nextPtrs = ((int*)this.buffer->next) + oldLength;

            using (new Unity.Profiling.ProfilerMarker("GetHashCode").Auto())
            {
                for (var idx = 0; idx < length; idx++)
                {
                    var bucket = keys[idx].GetHashCode() & this.buffer->bucketCapacityMask;
                    nextPtrs[idx] = buckets[bucket];
                    buckets[bucket] = oldLength + idx;
                }
            }

            this.buffer->allocatedIndexLength += length;
        }

        public bool TryGetValue(TKey key, out TValue item)
        {
            return this.TryGetFirstValue(key, out item, out _);
        }

        public bool ContainsKey(TKey key)
        {
            return this.TryGetValue(key, out _);
        }

        public bool TryGetFirstValue(TKey key, out TValue item, out NativeLookupIterator<TKey> it)
        {
            it.key = key;

            if (this.buffer->allocatedIndexLength <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            int* buckets = (int*)this.buffer->buckets;
            int bucket = key.GetHashCode() & this.buffer->bucketCapacityMask;
            it.EntryIndex = it.NextEntryIndex = buckets[bucket];
            return this.TryGetNextValue(out item, ref it);
        }

        public bool TryGetNextValue(out TValue item, ref NativeLookupIterator<TKey> it)
        {
            int entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;
            item = default;
            if (entryIdx < 0 || entryIdx >= this.buffer->keyCapacity)
            {
                return false;
            }

            int* nextPtrs = (int*)this.buffer->next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(this.buffer->keys, entryIdx).Equals(it.key))
            {
                entryIdx = nextPtrs[entryIdx];
                if (entryIdx < 0 || entryIdx >= this.buffer->keyCapacity)
                {
                    return false;
                }
            }

            it.NextEntryIndex = nextPtrs[entryIdx];
            it.EntryIndex = entryIdx;

            // Read the value
            item = UnsafeUtility.ReadArrayElement<TValue>(this.buffer->values, entryIdx);

            return true;
        }

        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TKey>(this.Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeLookupData.GetKeyArray(this.buffer, result);
            return result;
        }

        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TValue>(this.Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeLookupData.GetValueArray(this.buffer, result);
            return result;
        }

        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(this.Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeLookupData.GetKeyValueArrays(this.buffer, result);
            return result;
        }

        public void CalculateHashes()
        {
            var keys = (int*)this.buffer->keys;
            var buckets = (int*)this.buffer->buckets;
            var nextPtrs = (int*)this.buffer->next;
            var length = this.Length;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & this.buffer->bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }
        }

        public IEnumerator<KeyValue<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        internal bool SetValue(ref NativeLookupIterator<TKey> it, ref TValue item)
        {
            int entryIdx = it.EntryIndex;
            if (entryIdx < 0 || entryIdx >= this.buffer->keyCapacity)
            {
                return false;
            }

            UnsafeUtility.WriteArrayElement(this.buffer->values, entryIdx, item);
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckLengthsMatch(NativeArray<TKey> keys, NativeArray<TValue> values)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (keys.Length != values.Length)
            {
                throw new ArgumentException("Key and value array don't match");
            }
#endif
        }

        /// <summary> Returns a parallel writer for this hash map. </summary>
        /// <returns>A parallel writer for this hash map.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.threadIndex = 0;
            writer.buffer = this.buffer;
            return writer;
        }

        /// <summary>
        /// A parallel writer for an UnsafeMultiHashMap.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a NativeMultiHashMap.
        /// </remarks>
        [NativeContainerIsAtomicWriteOnly]
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeLookupData* buffer;

            [NativeSetThreadIndex]
            internal int threadIndex;

            /// <summary> Returns the number of key-value pairs that fit in the current allocation. </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public int Capacity => buffer->keyCapacity;

            public void Add(TKey key, TValue value)
            {
                var newLength = Interlocked.Add(ref this.buffer->allocatedIndexLength, 1);

                this.CheckCapacity(newLength);

                var oldLength = newLength - 1;

                var keyPtr = (TKey*)buffer->keys;
                var valuePtr = (TValue*)buffer->values;

                keyPtr[oldLength] = key;
                valuePtr[oldLength] = value;

                var buckets = (int*)buffer->buckets;
                var nextPtrs = ((int*)buffer->next);

                var hash = key.GetHashCode() & this.buffer->bucketCapacityMask;
                var next = Interlocked.Exchange(ref UnsafeUtility.ArrayElementAsRef<int>(buckets, hash), oldLength);
                nextPtrs[oldLength] = next;
            }

            public void AddBatch(NativeArray<TKey> keys, NativeArray<TValue> values)
            {
                CheckLengthsMatch(keys, values);
                this.AddBatch((TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
            }

            public void AddBatch(TKey* keys, TValue* values, int length)
            {
                var newLength = Interlocked.Add(ref this.buffer->allocatedIndexLength, length);

                this.CheckCapacity(newLength);

                var oldLength = newLength - length;

                var keyPtr = ((TKey*)buffer->keys) + oldLength;
                var valuePtr = ((TValue*)buffer->values) + oldLength;

                UnsafeUtility.MemCpy(keyPtr, keys, length * UnsafeUtility.SizeOf<TKey>());
                UnsafeUtility.MemCpy(valuePtr, values, length * UnsafeUtility.SizeOf<TValue>());

                var buckets = (int*)buffer->buckets;
                var nextPtrs = ((int*)buffer->next) + oldLength;

                for (var idx = 0; idx < length; idx++)
                {
                    var hash = keys[idx].GetHashCode() & this.buffer->bucketCapacityMask;
                    var index = oldLength + idx;
                    var next = Interlocked.Exchange(ref UnsafeUtility.ArrayElementAsRef<int>(buckets, hash), index);
                    nextPtrs[idx] = next;
                }
            }

            public void AddKeyValues(NativeArray<TKey> keys, NativeArray<TValue> values)
            {
                CheckLengthsMatch(keys, values);
                this.AddKeyValues((TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
            }

            public void AddKeyValues(TKey* keys, TValue* values, int length)
            {
                var newLength = Interlocked.Add(ref this.buffer->allocatedIndexLength, length);

                this.CheckCapacity(newLength);

                var oldLength = newLength - length;

                var keyPtr = ((TKey*)buffer->keys) + oldLength;
                var valuePtr = ((TValue*)buffer->values) + oldLength;

                UnsafeUtility.MemCpy(keyPtr, keys, length * UnsafeUtility.SizeOf<TKey>());
                UnsafeUtility.MemCpy(valuePtr, values, length * UnsafeUtility.SizeOf<TValue>());
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckCapacity(int length)
            {
                if (length > this.Capacity)
                {
                    throw new InvalidOperationException("Lookup is full");
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    [BurstCompatible]
    internal unsafe struct UnsafeLookupData
    {
        [FieldOffset(0)]
        internal byte* values;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(8)]
        internal byte* keys;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(16)]
        internal byte* next;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(24)]
        internal byte* buckets;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(32)]
        internal int keyCapacity;

        [FieldOffset(36)]
        internal int bucketCapacityMask; // = bucket capacity - 1

        [FieldOffset(40)]
        internal int allocatedIndexLength;

        internal static int GetBucketSize(int capacity)
        {
            return capacity * 2;
        }

        internal static int GrowCapacity(int capacity)
        {
            if (capacity == 0)
            {
                return 1;
            }

            return capacity * 2;
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        internal static void AllocateLookup<TKey, TValue>(int length, int bucketLength, AllocatorManager.AllocatorHandle label, out UnsafeLookupData* outBuf)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            var data = (UnsafeLookupData*)Memory.Unmanaged.Allocate(sizeof(UnsafeLookupData), UnsafeUtility.AlignOf<UnsafeLookupData>(), label);

            bucketLength = math.ceilpow2(bucketLength);

            data->keyCapacity = length;
            data->bucketCapacityMask = bucketLength - 1;

            var totalSize = CalculateDataSize<TKey, TValue>(length, bucketLength, out var keyOffset, out var nextOffset, out var bucketOffset);

            data->values = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, label);
            data->keys = data->values + keyOffset;
            data->next = data->values + nextOffset;
            data->buckets = data->values + bucketOffset;

            outBuf = data;
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        internal static void ReallocateLookup<TKey, TValue>(
            UnsafeLookupData* data,
            int newCapacity,
            int newBucketCapacity,
            AllocatorManager.AllocatorHandle label)
            where TKey : struct
            where TValue : struct
        {
            newBucketCapacity = math.ceilpow2(newBucketCapacity);

            if (data->keyCapacity == newCapacity && (data->bucketCapacityMask + 1) == newBucketCapacity)
            {
                return;
            }

            CheckLookupReallocateDoesNotShrink(data, newCapacity);

            var totalSize = CalculateDataSize<TKey, TValue>(newCapacity, newBucketCapacity, out var keyOffset, out var nextOffset, out var bucketOffset);

            var newData = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, label);
            var newKeys = newData + keyOffset;
            var newNext = newData + nextOffset;
            var newBuckets = newData + bucketOffset;

            // The items are taken from a free-list and might not be tightly packed, copy all of the old capcity
            UnsafeUtility.MemCpy(newData, data->values, data->keyCapacity * UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpy(newKeys, data->keys, data->keyCapacity * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(newNext, data->next, data->keyCapacity * UnsafeUtility.SizeOf<int>());

            for (var emptyNext = data->keyCapacity; emptyNext < newCapacity; ++emptyNext)
            {
                ((int*)newNext)[emptyNext] = -1;
            }

            // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
            for (var bucket = 0; bucket < newBucketCapacity; ++bucket)
            {
                ((int*)newBuckets)[bucket] = -1;
            }

            for (var bucket = 0; bucket <= data->bucketCapacityMask; ++bucket)
            {
                var buckets = (int*)data->buckets;
                var nextPtrs = (int*)newNext;
                while (buckets[bucket] >= 0)
                {
                    var curEntry = buckets[bucket];
                    buckets[bucket] = nextPtrs[curEntry];
                    var newBucket = UnsafeUtility.ReadArrayElement<TKey>(data->keys, curEntry).GetHashCode() & (newBucketCapacity - 1);
                    nextPtrs[curEntry] = ((int*)newBuckets)[newBucket];
                    ((int*)newBuckets)[newBucket] = curEntry;
                }
            }

            Memory.Unmanaged.Free(data->values, label);
            if (data->allocatedIndexLength > data->keyCapacity)
            {
                data->allocatedIndexLength = data->keyCapacity;
            }

            data->values = newData;
            data->keys = newKeys;
            data->next = newNext;
            data->buckets = newBuckets;
            data->keyCapacity = newCapacity;
            data->bucketCapacityMask = newBucketCapacity - 1;
        }

        internal static void DeallocateLookup(UnsafeLookupData* data, AllocatorManager.AllocatorHandle allocator)
        {
            Memory.Unmanaged.Free(data->values, allocator);
            Memory.Unmanaged.Free(data, allocator);
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        internal static int CalculateDataSize<TKey, TValue>(int length, int bucketLength, out int keyOffset, out int nextOffset, out int bucketOffset)
            where TKey : struct
            where TValue : struct
        {
            var sizeOfTValue = UnsafeUtility.SizeOf<TValue>();
            var sizeOfTKey = UnsafeUtility.SizeOf<TKey>();
            var sizeOfInt = UnsafeUtility.SizeOf<int>();

            var valuesSize = CollectionHelper.Align(sizeOfTValue * length, JobsUtility.CacheLineSize);
            var keysSize = CollectionHelper.Align(sizeOfTKey * length, JobsUtility.CacheLineSize);
            var nextSize = CollectionHelper.Align(sizeOfInt * length, JobsUtility.CacheLineSize);
            var bucketSize = CollectionHelper.Align(sizeOfInt * bucketLength, JobsUtility.CacheLineSize);
            var totalSize = valuesSize + keysSize + nextSize + bucketSize;

            keyOffset = 0 + valuesSize;
            nextOffset = keyOffset + keysSize;
            bucketOffset = nextOffset + nextSize;

            return totalSize;
        }

        internal static bool MoveNext(UnsafeLookupData* data, ref int bucketIndex, ref int nextIndex, out int index)
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;
            var capacityMask = data->bucketCapacityMask;

            if (nextIndex != -1)
            {
                index = nextIndex;
                nextIndex = bucketNext[nextIndex];
                return true;
            }

            for (var i = bucketIndex; i <= capacityMask; ++i)
            {
                var idx = bucketArray[i];

                if (idx != -1)
                {
                    index = idx;
                    bucketIndex = i + 1;
                    nextIndex = bucketNext[idx];

                    return true;
                }
            }

            index = -1;
            bucketIndex = capacityMask + 1;
            nextIndex = -1;
            return false;
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        internal static void GetKeyArray<TKey>(UnsafeLookupData* data, NativeArray<TKey> result)
            where TKey : struct
        {
            UnsafeUtility.MemCpy(result.GetUnsafePtr(), data->keys, data->allocatedIndexLength);
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        internal static void GetValueArray<TValue>(UnsafeLookupData* data, NativeArray<TValue> result)
            where TValue : struct
        {
            UnsafeUtility.MemCpy(result.GetUnsafePtr(), data->values, data->allocatedIndexLength);
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        internal static void GetKeyValueArrays<TKey, TValue>(UnsafeLookupData* data, NativeKeyValueArrays<TKey, TValue> result)
            where TKey : struct
            where TValue : struct
        {
            GetKeyArray(data, result.Keys);
            GetValueArray(data, result.Values);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckLookupReallocateDoesNotShrink(UnsafeLookupData* data, int newCapacity)
        {
            if (data->keyCapacity > newCapacity)
            {
                throw new Exception("Shrinking a lookup is not supported");
            }
        }
    }

    [BurstCompile]
    internal unsafe struct UnsafeLookupDisposeJob : IJobBurstSchedulable
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeLookupData* Data;

        public AllocatorManager.AllocatorHandle Allocator;

        public void Execute()
        {
            UnsafeLookupData.DeallocateLookup(this.Data, this.Allocator);
        }
    }

    /// <summary>
    /// A key-value pair.
    /// </summary>
    /// <remarks>Used for enumerators.</remarks>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    [BurstCompatible(GenericTypeArguments = new[] {typeof(int), typeof(int)})]
    public unsafe struct KeyValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal UnsafeLookupData* buffer;
        internal int index;
        internal int next;

        /// <summary>  An invalid KeyValue. </summary>
        /// <value>In a hash map enumerator's initial state, its <see cref="UnsafeLookup{TKey,TValue}.Enumerator.Current"/> value is Null.</value>
        public static KeyValue<TKey, TValue> Null => new() {index = -1};

        /// <summary>
        /// The key.
        /// </summary>
        /// <value>The key. If this KeyValue is Null, returns the default of TKey.</value>
        public TKey Key
        {
            get
            {
                if (this.index != -1)
                {
                    return UnsafeUtility.ReadArrayElement<TKey>(this.buffer->keys, this.index);
                }

                return default;
            }
        }

        /// <summary> Value of key/value pair. </summary>
        public ref TValue Value
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (this.index == -1)
                    throw new ArgumentException("must be valid");
#endif

                return ref UnsafeUtility.AsRef<TValue>(this.buffer->values + (UnsafeUtility.SizeOf<TValue>() * this.index));
            }
        }

        /// <summary>
        /// Gets the key and the value.
        /// </summary>
        /// <param name="key">Outputs the key. If this KeyValue is Null, outputs the default of TKey.</param>
        /// <param name="value">Outputs the value. If this KeyValue is Null, outputs the default of TValue.</param>
        /// <returns>True if the key-value pair is valid.</returns>
        public bool GetKeyValue(out TKey key, out TValue value)
        {
            if (this.index != -1)
            {
                key = UnsafeUtility.ReadArrayElement<TKey>(this.buffer->keys, this.index);
                value = UnsafeUtility.ReadArrayElement<TValue>(this.buffer->values, this.index);
                return true;
            }

            key = default;
            value = default;
            return false;
        }
    }

    internal unsafe struct UnsafeLookupDataEnumerator
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeLookupData* buffer;
        internal int index;
        internal int bucketIndex;
        internal int nextIndex;

        internal UnsafeLookupDataEnumerator(UnsafeLookupData* data)
        {
            this.buffer = data;
            this.index = -1;
            this.bucketIndex = 0;
            this.nextIndex = -1;
        }

        internal bool MoveNext()
        {
            return UnsafeLookupData.MoveNext(this.buffer, ref this.bucketIndex, ref this.nextIndex, out this.index);
        }

        internal void Reset()
        {
            this.index = -1;
            this.bucketIndex = 0;
            this.nextIndex = -1;
        }

        internal KeyValue<TKey, TValue> GetCurrent<TKey, TValue>()
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return new KeyValue<TKey, TValue> { buffer = this.buffer, index = this.index };
        }

        internal TKey GetCurrentKey<TKey>()
            where TKey : struct, IEquatable<TKey>
        {
            if (this.index != -1)
            {
                return UnsafeUtility.ReadArrayElement<TKey>(this.buffer->keys, this.index);
            }

            return default;
        }
    }

    internal sealed class UnsafeLookupDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
        where TValue : unmanaged
    {
#if !NET_DOTS
        private UnsafeLookup<TKey, TValue> target;

        public UnsafeLookupDebuggerTypeProxy(UnsafeLookup<TKey, TValue> target)
        {
            this.target = target;
        }

        private static (NativeArray<TKey> WithDuplicates, int Uniques) GetUniqueKeyArray(ref UnsafeLookup<TKey, TValue> Lookup, AllocatorManager.AllocatorHandle allocator)
        {
            var withDuplicates = Lookup.GetKeyArray(allocator);
            withDuplicates.Sort();
            int uniques = withDuplicates.Unique();
            return (withDuplicates, uniques);
        }

        public List<ListPair<TKey, List<TValue>>> Items
        {
            get
            {
                var result = new List<ListPair<TKey, List<TValue>>>();
                var keys = GetUniqueKeyArray(ref this.target, Allocator.Temp);

                using (keys.WithDuplicates)
                {
                    for (var k = 0; k < keys.Uniques; ++k)
                    {
                        var values = new List<TValue>();
                        if (this.target.TryGetFirstValue(keys.Item1[k], out var value, out var iterator))
                        {
                            do
                            {
                                values.Add(value);
                            }
                            while (this.target.TryGetNextValue(out value, ref iterator));
                        }

                        result.Add(new ListPair<TKey, List<TValue>>(keys.Item1[k], values));
                    }
                }

                return result;
            }
        }
#endif
    }

}
