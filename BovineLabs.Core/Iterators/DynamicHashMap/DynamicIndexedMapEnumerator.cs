// <copyright file="DynamicIndexedMapEnumerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// An enumerator over the key-value pairs of a container.
    /// </summary>
    /// <remarks>
    /// In an enumerator's initial state, <see cref="Current" /> is not valid to read.
    /// From this state, the first <see cref="MoveNext" /> call advances the enumerator to the first key-value pair.
    /// </remarks>
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public struct DynamicIndexedMapEnumerator<TKey, TIndex, TValue> : IEnumerator<KIV<TKey, TIndex, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex : unmanaged, IEquatable<TIndex>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private DynamicIndexedMapHelper<TKey, TIndex, TValue>.Enumerator enumerator;

        internal unsafe DynamicIndexedMapEnumerator(DynamicIndexedMapHelper<TKey, TIndex, TValue>* data)
        {
            this.enumerator = new DynamicIndexedMapHelper<TKey, TIndex, TValue>.Enumerator(data);
        }

        /// <summary> The current key-value pair. </summary>
        public KIV<TKey, TIndex, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.enumerator.GetCurrent();
        }

        /// <summary> Gets the element at the current position of the enumerator in the container. </summary>
        object IEnumerator.Current => this.Current;

        /// <summary> Advances the enumerator to the next key-value pair. </summary>
        /// <returns> True if <see cref="Current" /> is valid to read after the call. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return this.enumerator.MoveNext();
        }

        /// <summary> Resets the enumerator to its initial state. </summary>
        public void Reset()
        {
            this.enumerator.Reset();
        }

        /// <summary> Does nothing. </summary>
        public void Dispose()
        {
        }
    }
}
