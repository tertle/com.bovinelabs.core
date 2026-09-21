namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicPerfectHashMapDebuggerTypeProxy<,>))]
    public readonly unsafe struct DynamicPerfectHashMap<TKey, TValue> : IEnumerable<KVPair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged, IEquatable<TValue>
    {
        private readonly DynamicBuffer<byte> _buffer;

        internal DynamicPerfectHashMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);
            _buffer = buffer;
            Helper = buffer.AsHelper<TKey, TValue>(); // TODO enable
        }

        [field: NativeDisableUnsafePtrRestriction]
        internal DynamicPerfectHashMapHelper<TKey, TValue>* Helper { get; }

        /// <summary>
        /// Setting throws if a different key occupies the slot; the map cannot resolve hash collisions.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                _buffer.CheckReadAccess();
                if (Hint.Unlikely(!TryGetValue(key, out var value)))
                {
                    ThrowKeyNotPresent(key);
                    return default;
                }

                return value;
            }

            set
            {
                _buffer.CheckWriteAccess();
                if (Hint.Unlikely(!TryGetIndex(key, out var index)))
                {
                    ThrowKeyNotPresent(key);
                    return;
                }

                var values = Helper->Values;
                var current = values[index];

                if (!current.Equals(Helper->NullValue))
                {
                    if (!Helper->Keys[index].Equals(key))
                    {
                        ThrowKeyNotPresent(key);
                        return;
                    }

                    values[index] = value;
                    return;
                }

                Helper->Keys[index] = key;
                values[index] = value;
            }
        }

        public bool TryGetValue(TKey key, out TValue item)
        {
            _buffer.CheckReadAccess();
            if (!TryGetIndex(key, out var index))
            {
                item = default;
                return false;
            }

            var value = Helper->Values[index];
            if (value.Equals(Helper->NullValue) || !Helper->Keys[index].Equals(key))
            {
                item = default;
                return false;
            }

            item = value;
            return true;
        }

        public bool ContainsKey(TKey key)
        {
            _buffer.CheckReadAccess();
            if (!TryGetIndex(key, out var index))
            {
                return false;
            }

            var value = Helper->Values[index];
            return !value.Equals(Helper->NullValue) && Helper->Keys[index].Equals(key);
        }

        /// <summary>
        /// Not implemented for this collection.
        /// </summary>
        public IEnumerator<KVPair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented for this collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetIndex(TKey key, out int index)
        {
            index = IndexFor(key);
            return index >= 0 && index < Helper->Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexFor(TKey key)
        {
            return key.GetHashCode() & (Helper->Size - 1);
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
        private readonly DynamicPerfectHashMapHelper<TKey, TValue>* _helper;

        public DynamicPerfectHashMapDebuggerTypeProxy(DynamicPerfectHashMap<TKey, TValue> target)
        {
            _helper = target.Helper;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();

                if (_helper == null)
                {
                    return result;
                }

                var keys = _helper->Keys;
                var values = _helper->Values;
                var size = _helper->Size;

                for (var i = 0; i < size; ++i)
                {
                    var value = values[i];

                    if (UnsafeUtility.MemCmp(&value, &_helper->NullValue, sizeof(TValue)) != 0)
                    {
                        result.Add(new Pair<TKey, TValue>(keys[i], value));
                    }
                }

                return result;
            }
        }
    }
}
