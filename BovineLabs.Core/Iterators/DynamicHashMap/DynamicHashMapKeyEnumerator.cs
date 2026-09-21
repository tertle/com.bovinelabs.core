namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections;

    public struct DynamicHashMapKeyEnumerator<TKey, TValue> : IEnumerator<TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal DynamicMultiHashMap<TKey, TValue> hashmap;
        internal TKey key;
        internal byte isFirst;

        private TValue _value;
        private HashMapIterator<TKey> _iterator;

        public void Dispose()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            //Avoids going beyond the end of the collection.
            if (isFirst == 1)
            {
                isFirst = 0;
                return hashmap.TryGetFirstValue(key, out _value, out _iterator);
            }

            return hashmap.TryGetNextValue(out _value, ref _iterator);
        }

        public void Reset()
        {
            isFirst = 1;
        }

        public TValue Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
        }

        object IEnumerator.Current => Current;

        public DynamicHashMapKeyEnumerator<TKey, TValue> GetEnumerator()
        {
            return this;
        }
    }
}
