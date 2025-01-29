// <copyright file="UntypedDynamicHashMapIterator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    [NativeContainerIsReadOnly]
    public unsafe struct UntypedDynamicHashMapIterator : IEnumerator<(IntPtr Key, IntPtr Value)>
    {
        private readonly int keySize;
        private readonly int valueSize;

        [NativeDisableUnsafePtrRestriction]
        private UntypedDynamicHashMapHelper.Enumerator enumerator;

        internal UntypedDynamicHashMapIterator(UntypedDynamicHashMapHelper* data, int keySize, int valueSize)
        {
            this.keySize = keySize;
            this.valueSize = valueSize;
            this.enumerator = new UntypedDynamicHashMapHelper.Enumerator(data);
        }

        /// <summary> The current key-value pair. </summary>
        public (IntPtr Key, IntPtr Value) Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var current = this.enumerator.GetCurrent();

                var helper = (UntypedDynamicHashMapHelper*)current.UntypedDynamicHashMapHelper;
                var key = helper->Keys + (this.keySize * current.Index);
                var value = helper->Values + (this.valueSize * current.Index);

                return ((IntPtr)key, (IntPtr)value);
            }
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
