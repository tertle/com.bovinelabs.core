namespace BovineLabs.Core.Collections
{
    using System;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    internal ref struct BlobBuilderHashMapData<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        // we store these values in the builder because we cannot access BlobHashMapData itself (it must live in blob storage)
        internal readonly int KeyCapacity;
        private readonly int _bucketCapacityMask;

        private BlobBuilderArray<TValue> _values;
        private BlobBuilderArray<TKey> _keys;
        private BlobBuilderArray<int> _next;
        private BlobBuilderArray<int> _buckets;
        private BlobBuilderArray<int> _count;

        internal BlobBuilderHashMapData(int capacity, int bucketCapacityRatio, ref BlobBuilder blobBuilder, ref BlobHashMapData<TKey, TValue> data)
        {
            var bucketCapacity = math.ceilpow2(capacity * bucketCapacityRatio);

            // bucketCapacityMask is neccessary for retrieval so set it on the data too
            _bucketCapacityMask = data.BucketCapacityMask = bucketCapacity - 1;
            KeyCapacity = capacity;

            _values = blobBuilder.Allocate(ref data.Values, capacity);
            _keys = blobBuilder.Allocate(ref data.Keys, capacity);
            _next = blobBuilder.Allocate(ref data.Next, capacity);
            _buckets = blobBuilder.Allocate(ref data.Buckets, bucketCapacity);

            // so far the only way I've found to modify the true count on the data itself (without using unsafe code)
            // is by storing it in an array we can still access in the Add method.
            // count is only used in GetKeyArray and GetValueArray to size the array to the true count instead of capacity
            // count and keyCapacity are like
            _count = blobBuilder.Allocate(ref data.Count, 1);

            Clear();
        }

        internal int Count => _count[0];

        internal bool TryAdd(TKey key, TValue item, bool multi)
        {
            ref var c = ref _count[0];

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (c >= KeyCapacity)
            {
                throw new InvalidOperationException("HashMap is full");
            }
#endif

            var bucket = key.GetHashCode() & _bucketCapacityMask;

            if (!multi && ContainsKey(bucket, key))
            {
                return false;
            }

            var index = c++;
            _keys[index] = key;
            _values[index] = item;
            _next[index] = _buckets[bucket];
            _buckets[bucket] = index;

            return true;
        }

        internal unsafe ref TValue AddUnique(TKey key, bool multi)
        {
            ref var c = ref _count[0];

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (c >= KeyCapacity)
            {
                throw new InvalidOperationException("HashMap is full");
            }
#endif

            var bucket = key.GetHashCode() & _bucketCapacityMask;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!multi && ContainsKey(bucket, key))
            {
                throw new InvalidOperationException("Already contains key");
            }
#endif

            var index = c++;
            _keys[index] = key;
            _next[index] = _buckets[bucket];
            _buckets[bucket] = index;

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(_values.GetUnsafePtr(), index);
        }

        internal bool ContainsKey(TKey key)
        {
            var bucket = key.GetHashCode() & _bucketCapacityMask;

            return ContainsKey(bucket, key);
        }

        // Safety check for regular hashmap Add

        private bool ContainsKey(int bucket, TKey key)
        {
            var index = _buckets[bucket];

            if (index < 0)
            {
                return false;
            }

            while (!_keys[index].Equals(key))
            {
                index = _next[index];

                if (index < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void Clear()
        {
            for (var i = 0; i < _buckets.Length; i++)
            {
                _buckets[i] = -1;
            }

            for (var i = 0; i < _next.Length; i++)
            {
                _next[i] = -1;
            }
        }
    }
}