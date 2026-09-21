namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Utility;
    using Unity.Burst.CompilerServices;
    using Unity.Entities;

    public struct BlobPerfectHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged, IEquatable<TValue>
    {
        internal BlobArray<TValue> Values;
        internal int Capacity;
        internal TValue NullValue;

        public ref TValue this[TKey key]
        {
            get
            {
                if (Hint.Likely(TryGetValue(key, out var value)))
                {
                    return ref value.Ref;
                }

                ThrowKeyNotPresent(key);
                return ref value.Ref;
            }
        }

        public bool TryGetValue(TKey key, out Ptr<TValue> item)
        {
            if (!TryGetIndex(key, out var index))
            {
                item = default;
                return false;
            }

            item = new Ptr<TValue>(ref Values[index]);
            return !item.Ref.Equals(NullValue);
        }

        public bool ContainsKey(TKey key)
        {
            if (!TryGetIndex(key, out var index))
            {
                return false;
            }

            ref var value = ref Values[index];
            return !value.Equals(NullValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetIndex(TKey key, out int index)
        {
            index = IndexFor(key);
            return index >= 0 && index < Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexFor(TKey key)
        {
            return key.GetHashCode() & (Capacity - 1);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
        }
    }
}
