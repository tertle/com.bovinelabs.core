// <copyright file="BlobBuilderHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using Unity.Entities;

    public ref struct BlobBuilderHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private BlobBuilderHashMapData<TKey, TValue> data;

        internal BlobBuilderHashMap(int capacity, int bucketCapacityRatio, ref BlobBuilder blobBuilder, ref BlobHashMapData<TKey, TValue> data)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (capacity < 0)
            {
                throw new ArgumentException("Must not be negative", nameof(capacity));
            }

            if (bucketCapacityRatio <= 0)
            {
                throw new ArgumentException("Must be greater than zero", nameof(bucketCapacityRatio));
            }
#endif

            this.data = new BlobBuilderHashMapData<TKey, TValue>(capacity, bucketCapacityRatio, ref blobBuilder, ref data);
        }

        public int Capacity => this.data.KeyCapacity;

        public int Count => this.data.Count;

        public void Add(TKey key, TValue item)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!this.data.TryAdd(key, item, false))
            {
                throw new ArgumentException($"An item with key {key} already exists", nameof(key));
            }
#else
            TryAdd(key, item);
#endif
        }

        public ref TValue AddUnique(TKey key)
        {
            return ref this.data.AddUnique(key, false);
        }

        public bool TryAdd(TKey key, TValue value)
        {
            return this.data.TryAdd(key, value, false);
        }

        public bool ContainsKey(TKey key)
        {
            return this.data.ContainsKey(key);
        }
    }
}
