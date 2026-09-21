namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafePartialKeyedMap<TValue> : INativeDisposable
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private TValue* _values;

        [NativeDisableUnsafePtrRestriction]
        private int* _keys;

        private int _count;
        private int _nextCapacity;
        private int _bucketCapacity;

        public UnsafePartialKeyedMap(int* keys, TValue* values, int length, int bucketCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            CheckAllKeysOutOfBounds(keys, length, bucketCapacity);

            _keys = keys;
            _values = values;
            _count = length;
            _nextCapacity = length;
            _bucketCapacity = bucketCapacity;
            Allocator = allocator;

            // Allocated separate because buckets are fixed but next can change
            Next = (int*)CollectionMemory.Allocate(sizeof(int) * length, JobsUtility.CacheLineSize, allocator);
            Buckets = (int*)CollectionMemory.Allocate(sizeof(int) * bucketCapacity, JobsUtility.CacheLineSize, allocator);

            RecalculateBuckets();
        }

        public bool IsCreated => Buckets != null;

        [field: NativeDisableUnsafePtrRestriction]
        internal int* Next { get; private set; }

        [field: NativeDisableUnsafePtrRestriction]
        internal int* Buckets { get; private set; }

        internal AllocatorManager.AllocatorHandle Allocator { get; }

        public TValue this[int i]
        {
            get
            {
                CheckIndexOutOfBounds(i, _count);
                return _values[i];
            }
        }

        public static UnsafePartialKeyedMap<TValue>* Create(
            int* keys, TValue* values, int length, int bucketCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            var data = AllocatorManager.Allocate<UnsafePartialKeyedMap<TValue>>(allocator);
            *data = new UnsafePartialKeyedMap<TValue>(keys, values, length, bucketCapacity, allocator);
            return data;
        }

        public static void Destroy(UnsafePartialKeyedMap<TValue>* listData)
        {
            var allocator = listData->Allocator;
            listData->Dispose();
            AllocatorManager.Free(allocator, listData);
        }

        public void Dispose()
        {
            // Don't rewrite allocator
            CollectionMemory.Free(Buckets, Allocator);
            CollectionMemory.Free(Next, Allocator);
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafePartialKeyedMapDisposeJob
            {
                Next = Next,
                Buckets = Buckets,
                Allocator = Allocator,
            }.Schedule(inputDeps);

            Buckets = null;

            return jobHandle;
        }

        public void Update(int* newKeys, TValue* newValues, int newLength)
        {
            CheckAllKeysOutOfBounds(newKeys, newLength, _bucketCapacity);

            _keys = newKeys;
            _values = newValues;
            _count = newLength;

            // Check if we need more capacity
            if (_nextCapacity < newLength)
            {
                CollectionMemory.Free(Next, Allocator);
                _nextCapacity = newLength;
                Next = (int*)CollectionMemory.Allocate(sizeof(int) * _nextCapacity, JobsUtility.CacheLineSize, Allocator);
            }

            RecalculateBuckets();
        }

        public bool TryGetFirstValue(int key, out TValue item, out UnsafeKeyedMapIterator it)
        {
            CheckKeyOutOfBounds(key, _bucketCapacity);

            it = default;

            if (_count <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            it.NextEntryIndex = Buckets[key];
            return TryGetNextValue(out item, ref it);
        }

        public bool TryGetNextValue(out TValue item, ref UnsafeKeyedMapIterator it)
        {
            it.EntryIndex = it.NextEntryIndex;
            if (it.EntryIndex < 0)
            {
                it.NextEntryIndex = -1;
                item = default;

                return false;
            }

            var nextPtrs = Next;
            it.NextEntryIndex = nextPtrs[it.EntryIndex];

            // Read the value
            item = UnsafeUtility.ReadArrayElement<TValue>(_values, it.EntryIndex);

            return true;
        }

        private void RecalculateBuckets()
        {
            UnsafeUtility.MemSet(Buckets, 0xff, sizeof(int) * _bucketCapacity);

            for (var idx = 0; idx < _count; idx++)
            {
                var key = _keys[idx];
                Next[idx] = Buckets[key];
                Buckets[key] = idx;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllKeysOutOfBounds(int* keys, int length, int bucketCapacity)
        {
            for (var i = 0; i < length; i++)
            {
                CheckKeyOutOfBounds(keys[i], bucketCapacity);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckKeyOutOfBounds(int key, int bucketCapacity)
        {
            if (key < 0 || key >= bucketCapacity)
            {
                throw new InvalidOperationException($"{nameof(key)} < 0 || {nameof(key)} >= {nameof(bucketCapacity)}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckIndexOutOfBounds(int index, int count)
        {
            if (index < 0 || index >= count)
            {
                throw new InvalidOperationException($"{nameof(index)} < 0 || {nameof(index)} >= {nameof(count)}");
            }
        }
    }

    [BurstCompile]
    internal unsafe struct UnsafePartialKeyedMapDisposeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public int* Next;

        [NativeDisableUnsafePtrRestriction]
        public int* Buckets;

        public AllocatorManager.AllocatorHandle Allocator;

        public void Execute()
        {
            CollectionMemory.Free(Buckets, Allocator);
            CollectionMemory.Free(Next, Allocator);
        }
    }
}
