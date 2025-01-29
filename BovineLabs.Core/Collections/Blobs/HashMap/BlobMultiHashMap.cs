// <copyright file="BlobMultiHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using BovineLabs.Core.Utility;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> A MultiHashMap that can be used as inside blob asset. </summary>
    [MayOnlyLiveInBlobStorage]
    public struct BlobMultiHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal BlobHashMapData<TKey, TValue> Data;

        /// <summary> The current number of items in the container. </summary>
        public int Count => this.Data.Count[0];

        /// <summary> Retrieve iterator for the first value for the key. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="item"> Output value. </param>
        /// <param name="it"> Iterator. </param>
        /// <returns> Returns true if the container contains the key. </returns>
        public bool TryGetFirstValue(TKey key, out Ptr<TValue> item, out BlobMultiHashMapIterator<TKey> it)
        {
            return this.Data.TryGetFirstValue(key, out item, out it);
        }

        /// <summary> Retrieve iterator to the next value for the key. </summary>
        /// <param name="item"> Output value. </param>
        /// <param name="it"> Iterator. </param>
        /// <returns> Returns true if next value for the key is found. </returns>
        public bool TryGetNextValue(out Ptr<TValue> item, ref BlobMultiHashMapIterator<TKey> it)
        {
            return this.Data.TryGetNextValue(out item, ref it);
        }

        /// <summary> Determines whether an key is in the container. </summary>
        /// <param name="key"> The key to locate in the container. </param>
        /// <returns> Returns true if the container contains the key. </returns>
        public bool ContainsKey(TKey key)
        {
            return this.Data.TryGetFirstValue(key, out _, out _);
        }

        /// <summary> Returns an enumerator over the key-value pairs of this hash map. </summary>
        /// <returns> An enumerator over the key-value pairs of this hash map. </returns>
        public unsafe BlobHashMapEnumerator<TKey, TValue> GetEnumerator()
        {
            return new BlobHashMapEnumerator<TKey, TValue>(ref UnsafeUtility.AsRef<BlobHashMapData<TKey, TValue>>(UnsafeUtility.AddressOf(ref this)));
        }
    }
}
