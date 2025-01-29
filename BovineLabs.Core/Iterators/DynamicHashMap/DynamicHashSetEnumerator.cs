// <copyright file="DynamicHashSetEnumerator.cs" company="BovineLabs">
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
    /// An enumerator over the values of a set.
    /// </summary>
    /// <remarks>
    /// In an enumerator's initial state, <see cref="Current" /> is invalid.
    /// The first <see cref="MoveNext" /> call advances the enumerator to the first value.
    /// </remarks>
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public struct DynamicHashSetEnumerator<T> : IEnumerator<T>
        where T : unmanaged, IEquatable<T>
    {
        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<T>.Enumerator enumerator;

        internal unsafe DynamicHashSetEnumerator(DynamicHashMapHelper<T>* data)
        {
            this.enumerator = new DynamicHashMapHelper<T>.Enumerator(data);
        }

        /// <summary> The current value. </summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.enumerator.GetCurrentKey();
        }

        /// <summary>
        /// Gets the element at the current position of the enumerator in the container.
        /// </summary>
        object IEnumerator.Current => this.Current;

        /// <summary> Advances the enumerator to the next value. </summary>
        /// <returns> True if `Current` is valid to read after the call. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return this.enumerator.MoveNext();
        }

        /// <summary>
        /// Resets the enumerator to its initial state.
        /// </summary>
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
