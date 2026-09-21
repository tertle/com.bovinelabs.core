namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    [NativeContainerIsReadOnly]
    public struct DynamicHashMapEnumerator<TKey, TValue> : IEnumerator<KVPair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<TKey>.Enumerator _enumerator;

        internal unsafe DynamicHashMapEnumerator(DynamicHashMapHelper<TKey>* data)
        {
            _enumerator = new DynamicHashMapHelper<TKey>.Enumerator(data);
        }

        public KVPair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _enumerator.GetCurrent<TValue>();
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
