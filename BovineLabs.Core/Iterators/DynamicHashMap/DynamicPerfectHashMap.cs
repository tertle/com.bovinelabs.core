// <copyright file="DynamicPerfectHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> A fixed-size, collision-free hash map stored in a dynamic buffer. </summary>
    /// <typeparam name="TKey"> The key type. </typeparam>
    /// <typeparam name="TValue"> The value type. </typeparam>
    [DebuggerTypeProxy(typeof(DynamicPerfectHashMapDebuggerTypeProxy<,>))]
    public readonly unsafe struct DynamicPerfectHashMap<TKey, TValue> : IEnumerable<KVPair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged, IEquatable<TValue>
    {
        private readonly DynamicBuffer<byte> buffer;

        internal DynamicPerfectHashMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);
            this.buffer = buffer;
            this.Helper = buffer.AsHelper<TKey, TValue>(); // TODO enable
        }

        [field: NativeDisableUnsafePtrRestriction]
        internal DynamicPerfectHashMapHelper<TKey, TValue>* Helper { get; }

        /// <summary> Gets and sets values by key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <value> The value associated with the key. </value>
        /// <remarks>
        /// Getting a key that is not present will throw.
        /// Setting a key that is not already present will add the key if the slot is empty; otherwise it will throw if the slot is occupied by a different key.
        /// </remarks>
        /// <exception cref="ArgumentException"> Thrown if the key is not present. </exception>
        public TValue this[TKey key]
        {
            get
            {
                this.buffer.CheckReadAccess();
                if (Hint.Unlikely(!this.TryGetValue(key, out var value)))
                {
                    this.ThrowKeyNotPresent(key);
                    return default;
                }

                return value;
            }

            set
            {
                this.buffer.CheckWriteAccess();
                if (Hint.Unlikely(!this.TryGetIndex(key, out var index)))
                {
                    this.ThrowKeyNotPresent(key);
                    return;
                }

                var values = this.Helper->Values;
                var current = values[index];

                if (!current.Equals(this.Helper->NullValue))
                {
                    if (!this.Helper->Keys[index].Equals(key))
                    {
                        this.ThrowKeyNotPresent(key);
                        return;
                    }

                    values[index] = value;
                    return;
                }

                this.Helper->Keys[index] = key;
                values[index] = value;
            }
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <returns> True if the key was present. </returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            this.buffer.CheckReadAccess();
            if (!this.TryGetIndex(key, out var index))
            {
                item = default;
                return false;
            }

            var value = this.Helper->Values[index];
            if (value.Equals(this.Helper->NullValue) || !this.Helper->Keys[index].Equals(key))
            {
                item = default;
                return false;
            }

            item = value;
            return true;
        }

        /// <summary> Checks whether the map contains a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <returns> True if the key is present. </returns>
        public bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            if (!this.TryGetIndex(key, out var index))
            {
                return false;
            }

            var value = this.Helper->Values[index];
            return !value.Equals(this.Helper->NullValue) && this.Helper->Keys[index].Equals(key);
        }

        /// <summary> This method is not implemented for this type. </summary>
        /// <returns> An enumerator. </returns>
        /// <exception cref="NotImplementedException"> Always thrown. </exception>
        public IEnumerator<KVPair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary> This method is not implemented for this type. </summary>
        /// <returns> An enumerator. </returns>
        /// <exception cref="NotImplementedException"> Always thrown. </exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetIndex(TKey key, out int index)
        {
            index = this.IndexFor(key);
            return index >= 0 && index < this.Helper->Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexFor(TKey key)
        {
            return key.GetHashCode() & (this.Helper->Size - 1);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicPerfectHashMapHelper<TKey, TValue>>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
        }
    }

    internal sealed unsafe class DynamicPerfectHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged, IEquatable<TValue>
    {
        private readonly DynamicPerfectHashMapHelper<TKey, TValue>* helper;

        /// <summary> Initializes a new instance of the <see cref="DynamicPerfectHashMapDebuggerTypeProxy{TKey, TValue}" /> class. </summary>
        /// <param name="target"> The target map. </param>
        public DynamicPerfectHashMapDebuggerTypeProxy(DynamicPerfectHashMap<TKey, TValue> target)
        {
            this.helper = target.Helper;
        }

        /// <summary> Gets the key/value pairs present in the map. </summary>
        /// <value> A list of pairs. </value>
        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();

                if (this.helper == null)
                {
                    return result;
                }

                var keys = this.helper->Keys;
                var values = this.helper->Values;
                var size = this.helper->Size;

                for (var i = 0; i < size; ++i)
                {
                    var value = values[i];

                    if (UnsafeUtility.MemCmp(&value, &this.helper->NullValue, sizeof(TValue)) != 0)
                    {
                        result.Add(new Pair<TKey, TValue>(keys[i], value));
                    }
                }

                return result;
            }
        }
    }
}
