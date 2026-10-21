namespace BovineLabs.Core.Collections
{
    using System;
    using Unity.Entities;

    public ref struct BlobBuilderMultiHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private BlobBuilderHashMapData<TKey, TValue> _data;

        internal BlobBuilderMultiHashMap(int capacity, int bucketCapacityRatio, ref BlobBuilder blobBuilder, ref BlobHashMapData<TKey, TValue> data)
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

            _data = new BlobBuilderHashMapData<TKey, TValue>(capacity, bucketCapacityRatio, ref blobBuilder, ref data);
        }

        public int Capacity => _data.KeyCapacity;

        public int Count => _data.Count;

        public void Add(TKey key, TValue item)
        {
            _data.TryAdd(key, item, true);
        }

        public ref TValue Add(TKey key)
        {
            return ref _data.AddUnique(key, true);
        }
    }
}
