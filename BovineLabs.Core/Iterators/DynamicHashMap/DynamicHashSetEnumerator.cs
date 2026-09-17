namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

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

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.enumerator.GetCurrentKey();
        }

        object IEnumerator.Current => this.Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return this.enumerator.MoveNext();
        }

        public void Reset()
        {
            this.enumerator.Reset();
        }

        public void Dispose()
        {
        }
    }
}
