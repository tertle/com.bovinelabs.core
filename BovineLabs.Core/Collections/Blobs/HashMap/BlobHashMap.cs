namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections.Generic;
    using Unity.Collections;
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

        /// <summary>
        /// Retrieve a value by key
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                if (this.TryGetValue(key, out var value))
                {
                    return value;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                throw new KeyNotFoundException($"Key: {key} is not present in the BlobHashMap.");
#else
                return default;
#endif
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="item">If key is found item parameter will contain value</param>
        /// <returns>Returns true if key is found, otherwise returns false.</returns>
        public bool TryGetValue(TKey key, out TValue item) => this.Data.TryGetFirstValue(key, out item, out _);

        /// <summary>
        /// Determines whether an key is in the container.
        /// </summary>
        /// <param name="key">The key to locate in the container.</param>
        /// <returns>Returns true if the container contains the key.</returns>
        public bool ContainsKey(TKey key) => this.TryGetValue(key, out _);

        /// <summary>
        /// The current number of items in the container
        /// </summary>
        public int Count => this.Data.Count[0];

        /// <summary>
        /// Returns an array containing all of the keys in this container
        /// </summary>
        public NativeArray<TKey> GetKeyArray(Allocator allocator) => this.Data.GetKeys(allocator);

        /// <summary>
        /// Returns an array containing all of the values in this container
        /// </summary>
        public NativeArray<TValue> GetValueArray(Allocator allocator) => this.Data.GetValues(allocator);
    }

}
