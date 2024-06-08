// <copyright file="BlobBuilderMultiHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using Unity.Entities;

    public ref struct BlobBuilderMultiHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private BlobBuilderHashMapData<TKey, TValue> data;

        internal BlobBuilderMultiHashMap(int capacity, int bucketCapacityRatio, ref BlobBuilder blobBuilder, ref BlobHashMapData<TKey, TValue> data)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (capacity <= 0)
            {
                throw new ArgumentException("Must be greater than zero", nameof(capacity));
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
            this.data.TryAdd(key, item, true);
        }

        public ref TValue AddUnique(TKey key)
        {
            return ref this.data.AddUnique(key, true);
        }
    }
}
