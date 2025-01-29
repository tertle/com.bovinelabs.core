// <copyright file="BlobHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Utility;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary>
    /// A read only hashmap that can be used inside a blob asset
    /// </summary>
    [MayOnlyLiveInBlobStorage]
    public struct BlobHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal BlobHashMapData<TKey, TValue> Data;

        /// <summary> Gets the current number of items in the container. </summary>
        public int Count => this.Data.Count[0];

        /// <summary> Retrieve a value by key. </summary>
        public ref TValue this[TKey key]
        {
            get
            {
                if (this.TryGetValue(key, out var value))
                {
                    return ref value.Ref;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                throw new KeyNotFoundException($"Key: {key} is not present in the BlobHashMap.");
#else
                return ref value.Ref;
#endif
            }
        }

        /// <summary> Gets the value associated with the specified key. </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="item"> If key is found item parameter will contain value. </param>
        /// <returns> Returns true if key is found, otherwise returns false. </returns>
        public bool TryGetValue(TKey key, out Ptr<TValue> item)
        {
            return this.Data.TryGetFirstValue(key, out item, out _);
        }

        /// <summary>
        /// Determines whether an key is in the container.
        /// </summary>
        /// <param name="key"> The key to locate in the container. </param>
        /// <returns> Returns true if the container contains the key. </returns>
        public bool ContainsKey(TKey key)
        {
            return this.TryGetValue(key, out _);
        }

        /// <summary> Returns an enumerator over the key-value pairs of this hash map. </summary>
        /// <returns> An enumerator over the key-value pairs of this hash map. </returns>
        public unsafe BlobHashMapEnumerator<TKey, TValue> GetEnumerator()
        {
            return new BlobHashMapEnumerator<TKey, TValue>(ref UnsafeUtility.AsRef<BlobHashMapData<TKey, TValue>>(UnsafeUtility.AddressOf(ref this)));
        }
    }
}
