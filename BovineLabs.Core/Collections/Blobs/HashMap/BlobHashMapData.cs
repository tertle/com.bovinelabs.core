namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.Utility;
    using Unity.Entities;

    internal struct BlobHashMapData<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal BlobArray<TValue> Values;
        internal BlobArray<TKey> Keys;
        internal BlobArray<int> Next;
        internal BlobArray<int> Buckets;
        internal BlobArray<int> Count; // only contains a single element containing the true count (set by builder)

        internal int BucketCapacityMask; // == buckets.Length - 1

        internal bool TryGetFirstValue(TKey key, out Ptr<TValue> item, out BlobMultiHashMapIterator<TKey> it)
        {
            it.Key = key;

            if (BucketCapacityMask < 0)
            {
                it.NextIndex = -1;
                item = default;
                return false;
            }

            // ReSharper disable once Unity.BurstAccessingManagedMethod
            var bucket = key.GetHashCode() & BucketCapacityMask;
            it.NextIndex = Buckets[bucket];

            return TryGetNextValue(out item, ref it);
        }

        internal bool TryGetNextValue(out Ptr<TValue> item, ref BlobMultiHashMapIterator<TKey> it)
        {
            var index = it.NextIndex;
            it.NextIndex = -1;
            item = default;

            if (index < 0)
            {
                return false;
            }

            while (!Keys[index].Equals(it.Key))
            {
                index = Next[index];
                if (index < 0)
                {
                    return false;
                }
            }

            it.NextIndex = Next[index];
            item = new Ptr<TValue>(ref Values[index]);
            return true;
        }
    }

    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    public readonly unsafe struct KVPair<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly BlobHashMapData<TKey, TValue>* _data;
        private readonly int _index;

        internal KVPair(BlobHashMapData<TKey, TValue>* data, int index)
        {
            _data = data;
            _index = index;
        }

        /// <summary>
        /// Returns default(TKey) for a null KeyValue.
        /// </summary>
        public ref TKey Key
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (_index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return ref _data->Keys[_index];
            }
        }

        public ref TValue Value
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (_index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return ref _data->Values[_index];
            }
        }
    }
}
