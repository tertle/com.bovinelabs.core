// <copyright file="DynamicHashMapKeyEnumerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections;

    /// <summary>
    /// An enumerator over the values of an individual key in a multi hash map.
    /// </summary>
    /// <remarks>
    /// In an enumerator's initial state, <see cref="Current" /> is not valid to read.
    /// The first <see cref="MoveNext" /> call advances the enumerator to the first value of the key.
    /// </remarks>
    public struct DynamicHashMapKeyEnumerator<TKey, TValue> : IEnumerator<TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal DynamicMultiHashMap<TKey, TValue> hashmap;
        internal TKey key;
        internal byte isFirst;

        private TValue value;
        private HashMapIterator<TKey> iterator;

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Advances the enumerator to the next value of the key.
        /// </summary>
        /// <returns> True if <see cref="Current" /> is valid to read after the call. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            //Avoids going beyond the end of the collection.
            if (this.isFirst == 1)
            {
                this.isFirst = 0;
                return this.hashmap.TryGetFirstValue(this.key, out this.value, out this.iterator);
            }

            return this.hashmap.TryGetNextValue(out this.value, ref this.iterator);
        }

        /// <summary>
        /// Resets the enumerator to its initial state.
        /// </summary>
        public void Reset()
        {
            this.isFirst = 1;
        }

        /// <summary>
        /// The current value.
        /// </summary>
        /// <value> The current value. </value>
        public TValue Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.value;
        }

        object IEnumerator.Current => this.Current;

        /// <summary>
        /// Returns this enumerator.
        /// </summary>
        /// <returns> This enumerator. </returns>
        public DynamicHashMapKeyEnumerator<TKey, TValue> GetEnumerator()
        {
            return this;
        }
    }
}
