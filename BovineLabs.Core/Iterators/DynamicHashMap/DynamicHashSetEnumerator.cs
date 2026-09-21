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
        private DynamicHashMapHelper<T>.Enumerator _enumerator;

        internal unsafe DynamicHashSetEnumerator(DynamicHashMapHelper<T>* data)
        {
            _enumerator = new DynamicHashMapHelper<T>.Enumerator(data);
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _enumerator.GetCurrentKey();
        }

        object IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        public void Dispose()
        {
        }
    }
}
