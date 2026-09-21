namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicMultiHashMapDebuggerTypeProxy<,>))]
    public unsafe struct DynamicMultiHashMap<TKey, TValue> : IEnumerable<KVPair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicBuffer<byte> _buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<TKey>* _helper;

        internal DynamicMultiHashMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            _buffer = buffer;
            _helper = buffer.AsHelper<TKey>();
        }

        public readonly bool IsCreated => _buffer.IsCreated;

        public readonly bool IsEmpty
        {
            get
            {
                _buffer.CheckReadAccess();
                RefCheck();
                return !IsCreated || _helper->IsEmpty;
            }
        }

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                _buffer.CheckReadAccess();
                RefCheck();
                return _helper->Count;
            }
        }

        /// <summary>
        /// Capacity cannot shrink.
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                _buffer.CheckReadAccess();
                RefCheck();
                return _helper->Capacity;
            }

            set
            {
                _buffer.CheckWriteAccess();
                RefCheck();
                DynamicHashMapHelper<TKey>.Resize(_buffer, ref _helper, value);
            }
        }

        internal DynamicHashMapHelper<TKey>* Helper => _helper;

        public readonly void Clear()
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->Clear();
        }

        public void Add(TKey key, TValue item)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            var idx = DynamicHashMapHelper<TKey>.AddMulti(_buffer, ref _helper, key);
            UnsafeUtility.WriteArrayElement(_helper->Values, idx, item);
        }

        public bool TryAddUniquePair<T>(TKey key, T item)
            where T : unmanaged, IEquatable<TValue>
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            CheckValueSize<T>();

            if (_helper->TryGetFirstValue(key, out TValue value, out var it))
            {
                do
                {
                    if (item.Equals(value))
                    {
                        return false;
                    }
                }
                while (_helper->TryGetNextValue(out value, ref it));
            }

            var idx = DynamicHashMapHelper<TKey>.AddMulti(_buffer, ref _helper, key);
            UnsafeUtility.WriteArrayElement(_helper->Values, idx, item);
            return true;
        }

        public readonly int Remove(TKey key)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            return _helper->Remove(key);
        }

        public readonly void Remove(HashMapIterator<TKey> it)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->Remove(it);
        }

        /// <summary>
        /// Removes only one occurrence of the exact key-value pair.
        /// </summary>
        public readonly bool Remove<T>(TKey key, T value)
            where T : unmanaged, IEquatable<TValue>
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            CheckValueSize<T>();

            if (!_helper->TryGetFirstValue(key, out TValue item, out var it))
            {
                return false;
            }

            do
            {
                if (!value.Equals(item))
                {
                    continue;
                }

                _helper->Remove(it);
                return true;
            }
            while (_helper->TryGetNextValue(out item, ref it));

            return false;
        }

        public readonly bool TryGetFirstValue(TKey key, out TValue item, out HashMapIterator<TKey> it)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->TryGetFirstValue(key, out item, out it);
        }

        public readonly bool TryGetNextValue(out TValue item, ref HashMapIterator<TKey> it)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->TryGetNextValue(out item, ref it);
        }

        public readonly bool ContainsKey(TKey key)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->Find(key) != -1;
        }

        public readonly bool Contains<T>(TKey key, T value)
            where T : unmanaged, IEquatable<TValue>
        {
            _buffer.CheckReadAccess();
            RefCheck();
            var e = GetValuesForKey(key);
            while (e.MoveNext())
            {
                if (value.Equals(e.Current))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly int CountValuesForKey(TKey key)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            var count = 0;
            var e = GetValuesForKey(key);
            while (e.MoveNext())
            {
                count++;
            }

            return count;
        }

        public void Flatten()
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            DynamicHashMapHelper<TKey>.Flatten(_buffer, ref _helper);
        }

        public void AddBatchUnsafe(NativeArray<TKey> keys, NativeArray<TValue> values)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            CheckLengthsMatch(keys.Length, values.Length);
            AddBatchUnsafe((TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public void AddBatchUnsafe(TKey* keys, TValue* values, int length)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            DynamicHashMapHelper<TKey>.AddBatchUnsafe(_buffer, ref _helper, keys, (byte*)values, length);
        }

        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->GetKeyArray(allocator);
        }

        public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->GetValueArray<TValue>(allocator);
        }

        public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->GetKeyValueArrays<TValue>(allocator);
        }

        public readonly DynamicHashMapKeyEnumerator<TKey, TValue> GetValuesForKey(TKey key)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return new DynamicHashMapKeyEnumerator<TKey, TValue>
            {
                hashmap = this,
                key = key,
                isFirst = 1,
            };
        }

        public readonly DynamicHashMapEnumerator<TKey, TValue> GetEnumerator()
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return new DynamicHashMapEnumerator<TKey, TValue>(_helper);
        }

        /// <summary>
        /// Not implemented; use the concrete GetEnumerator instead.
        /// </summary>
        IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented; use the concrete GetEnumerator instead.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            if (_helper != _buffer.GetPtr())
            {
                throw new ArgumentException("DynamicMultiHashMap was not passed by ref when doing a resize and is now invalid");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicHashMapHelper<TKey>>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckLengthsMatch(int keys, int values)
        {
            if (keys != values)
            {
                throw new ArgumentException("Key and value array don't match");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckValueSize<T>()
            where T : unmanaged
        {
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<TValue>())
            {
                throw new InvalidOperationException("DynamicMultiHashMap exact-pair operation value type must match the map value type size.");
            }
        }
    }

    internal sealed unsafe class DynamicMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicHashMapHelper<TKey>* _helper;

        public DynamicMultiHashMapDebuggerTypeProxy(DynamicMultiHashMap<TKey, TValue> target)
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

                using var kva = _helper->GetKeyValueArrays<TValue>(Allocator.Temp);

                for (var i = 0; i < kva.Length; ++i)
                {
                    result.Add(new Pair<TKey, TValue>(kva.Keys[i], kva.Values[i]));
                }

                return result;
            }
        }
    }
}
