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

        private TValue value;
        private HashMapIterator<TKey> iterator;

        public void Dispose()
        {
        }

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

        public void Reset()
        {
            this.isFirst = 1;
        }

        public TValue Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.value;
        }

        object IEnumerator.Current => this.Current;

        public DynamicHashMapKeyEnumerator<TKey, TValue> GetEnumerator()
        {
            return this;
        }
    }
}
