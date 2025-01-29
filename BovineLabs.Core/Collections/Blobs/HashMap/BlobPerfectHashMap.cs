// <copyright file="BlobPerfectHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
                if (Hint.Likely(this.TryGetValue(key, out var value)))
                {
                    return ref value.Ref;
                }

                this.ThrowKeyNotPresent(key);
                return ref value.Ref;
            }
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <returns> True if the key was present. </returns>
        public bool TryGetValue(TKey key, out Ptr<TValue> item)
        {
            if (!this.TryGetIndex(key, out var index))
            {
                item = default;
                return false;
            }

            item = new Ptr<TValue>(ref this.Values[index]);
            return !item.Ref.Equals(this.NullValue);
        }

        public bool ContainsKey(TKey key)
        {
            if (!this.TryGetIndex(key, out var index))
            {
                return false;
            }

            ref var value = ref this.Values[index];
            return !value.Equals(this.NullValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetIndex(TKey key, out int index)
        {
            index = this.IndexFor(key);
            return index >= 0 && index < this.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexFor(TKey key)
        {
            return key.GetHashCode() & (this.Capacity - 1);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
        }
    }
}
