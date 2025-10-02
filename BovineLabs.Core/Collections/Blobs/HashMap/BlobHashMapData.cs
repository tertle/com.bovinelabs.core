// <copyright file="BlobHashMapData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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

            if (this.BucketCapacityMask < 0)
            {
                it.NextIndex = -1;
                item = default;
                return false;
            }

            // ReSharper disable once Unity.BurstAccessingManagedMethod
            var bucket = key.GetHashCode() & this.BucketCapacityMask;
            it.NextIndex = this.Buckets[bucket];

            return this.TryGetNextValue(out item, ref it);
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

            while (!this.Keys[index].Equals(it.Key))
            {
                index = this.Next[index];
                if (index < 0)
                {
                    return false;
                }
            }

            it.NextIndex = this.Next[index];
            item = new Ptr<TValue>(ref this.Values[index]);
            return true;
        }
    }

    /// <summary> A key-value pair. </summary>
    /// <remarks> Used for enumerators. </remarks>
    /// <typeparam name="TKey"> The type of the keys. </typeparam>
    /// <typeparam name="TValue"> The type of the values. </typeparam>
    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    public readonly unsafe struct KVPair<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly BlobHashMapData<TKey, TValue>* data;
        private readonly int index;

        internal KVPair(BlobHashMapData<TKey, TValue>* data, int index)
        {
            this.data = data;
            this.index = index;
        }

        /// <summary>
        /// The key.
        /// </summary>
        /// <value> The key. If this KeyValue is Null, returns the default of TKey. </value>
        public ref TKey Key
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (this.index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return ref this.data->Keys[this.index];
            }
        }

        /// <summary>
        /// Value of key/value pair.
        /// </summary>
        public ref TValue Value
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (this.index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return ref this.data->Values[this.index];
            }
        }
    }
}
