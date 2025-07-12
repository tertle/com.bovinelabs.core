// <copyright file="BlobHashMapDataBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        private readonly int bucketCapacityMask;

        private BlobBuilderArray<TValue> values;
        private BlobBuilderArray<TKey> keys;
        private BlobBuilderArray<int> next;
        private BlobBuilderArray<int> buckets;
        private BlobBuilderArray<int> count;

        internal BlobBuilderHashMapData(int capacity, int bucketCapacityRatio, ref BlobBuilder blobBuilder, ref BlobHashMapData<TKey, TValue> data)
        {
            var bucketCapacity = math.ceilpow2(capacity * bucketCapacityRatio);

            // bucketCapacityMask is neccessary for retrieval so set it on the data too
            this.bucketCapacityMask = data.BucketCapacityMask = bucketCapacity - 1;
            this.KeyCapacity = capacity;

            this.values = blobBuilder.Allocate(ref data.Values, capacity);
            this.keys = blobBuilder.Allocate(ref data.Keys, capacity);
            this.next = blobBuilder.Allocate(ref data.Next, capacity);
            this.buckets = blobBuilder.Allocate(ref data.Buckets, bucketCapacity);

            // so far the only way I've found to modify the true count on the data itself (without using unsafe code)
            // is by storing it in an array we can still access in the Add method.
            // count is only used in GetKeyArray and GetValueArray to size the array to the true count instead of capacity
            // count and keyCapacity are like
            this.count = blobBuilder.Allocate(ref data.Count, 1);

            this.Clear();
        }

        internal int Count => this.count[0];

        internal bool TryAdd(TKey key, TValue item, bool multi)
        {
            ref var c = ref this.count[0];

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (c >= this.KeyCapacity)
            {
                throw new InvalidOperationException("HashMap is full");
            }
#endif

            var bucket = key.GetHashCode() & this.bucketCapacityMask;

            if (!multi && this.ContainsKey(bucket, key))
            {
                return false;
            }

            var index = c++;
            this.keys[index] = key;
            this.values[index] = item;
            this.next[index] = this.buckets[bucket];
            this.buckets[bucket] = index;

            return true;
        }

        internal unsafe ref TValue AddUnique(TKey key, bool multi)
        {
            ref var c = ref this.count[0];

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (c >= this.KeyCapacity)
            {
                throw new InvalidOperationException("HashMap is full");
            }
#endif

            var bucket = key.GetHashCode() & this.bucketCapacityMask;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!multi && this.ContainsKey(bucket, key))
            {
                throw new InvalidOperationException("Already contains key");
            }
#endif

            var index = c++;
            this.keys[index] = key;
            this.next[index] = this.buckets[bucket];
            this.buckets[bucket] = index;

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(this.values.GetUnsafePtr(), index);
        }

        internal bool ContainsKey(TKey key)
        {
            var bucket = key.GetHashCode() & this.bucketCapacityMask;

            return this.ContainsKey(bucket, key);
        }

        // Safety check for regular hashmap Add

        private bool ContainsKey(int bucket, TKey key)
        {
            var index = this.buckets[bucket];

            if (index < 0)
            {
                return false;
            }

            while (!this.keys[index].Equals(key))
            {
                index = this.next[index];

                if (index < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void Clear()
        {
            for (var i = 0; i < this.buckets.Length; i++)
            {
                this.buckets[i] = -1;
            }

            for (var i = 0; i < this.next.Length; i++)
            {
                this.next[i] = -1;
            }
        }
    }
}