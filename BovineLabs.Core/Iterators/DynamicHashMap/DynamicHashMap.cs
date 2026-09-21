namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicHashMapDebuggerTypeProxy<,>))]
    public unsafe struct DynamicHashMap<TKey, TValue> : IEnumerable<KVPair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicBuffer<byte> _buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<TKey>* _helper;

        internal DynamicHashMap(DynamicBuffer<byte> buffer)
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

        public TValue this[TKey key]
        {
            readonly get
            {
                _buffer.CheckReadAccess();
                RefCheck();
                if (TryGetValue(key, out var res))
                {
                    return res;
                }

                ThrowKeyNotPresent(key);

                return default;
            }

            set
            {
                _buffer.CheckWriteAccess();
                RefCheck();

                var idx = _helper->Find(key);

                if (idx == -1)
                {
                    // Use optimized path that doesn't repeat the Find() call
                    idx = DynamicHashMapHelper<TKey>.AddWithKnownAbsence(_buffer, ref _helper, key);
                }

                UnsafeUtility.WriteArrayElement(_helper->Values, idx, value);
            }
        }

        public readonly void Clear()
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->Clear();
        }

        public readonly void ClearDense()
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->ClearDense();
        }

        public bool TryAdd(TKey key, TValue item)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var idx = DynamicHashMapHelper<TKey>.TryAdd(_buffer, ref _helper, key);
            if (idx != -1)
            {
                UnsafeUtility.WriteArrayElement(_helper->Values, idx, item);
                return true;
            }

            return false;
        }

        public void Add(TKey key, TValue item)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var idx = DynamicHashMapHelper<TKey>.AddUnique(_buffer, ref _helper, key);
            UnsafeUtility.WriteArrayElement(_helper->Values, idx, item);
        }

        /// <summary>
        /// The returned reference aliases map storage. Consume immediately; any later map write or capacity change invalidates it.
        /// </summary>
        public ref TValue GetOrAddRefUnsafe(TKey key, TValue defaultValue = default)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var idx = _helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicHashMapHelper<TKey>.AddWithKnownAbsence(_buffer, ref _helper, key);
                UnsafeUtility.WriteArrayElement(_helper->Values, idx, defaultValue);
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(_helper->Values, idx);
        }

        /// <summary>
        /// The returned reference aliases map storage. Consume immediately; any later map write or capacity change invalidates it.
        /// </summary>
        public ref TValue GetOrAddRefUnsafe(TKey key, out bool add, TValue defaultValue = default)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var idx = _helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicHashMapHelper<TKey>.AddWithKnownAbsence(_buffer, ref _helper, key);
                UnsafeUtility.WriteArrayElement(_helper->Values, idx, defaultValue);
                add = true;
            }
            else
            {
                add = false;
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(_helper->Values, idx);
        }

        public Ptr<TValue> GetRef(TKey key)
        {
            _buffer.CheckReadAccess();
            RefCheck();

            var idx = _helper->Find(key);
            if (idx == -1)
            {
                return default;
            }

            return new Ptr<TValue>((TValue*)_helper->Values + idx);
        }

        public readonly TValue GetOrDefault(TKey key, TValue defaultValue = default)
        {
            _buffer.CheckReadAccess();
            RefCheck();

            var idx = _helper->Find(key);
            if (idx == -1)
            {
                return defaultValue;
            }

            return UnsafeUtility.ReadArrayElement<TValue>(_helper->Values, idx);
        }

        public readonly bool Remove(TKey key)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            return _helper->TryRemove(key) != -1;
        }

        public readonly bool TryGetValue(TKey key, out TValue item)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->TryGetValue(key, out item);
        }

        public readonly bool ContainsKey(TKey key)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->Find(key) != -1;
        }

        public void Flatten()
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            DynamicHashMapHelper<TKey>.Flatten(_buffer, ref _helper);
        }

        public void RemoveRangeShiftDown(int index, int range)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->RemoveRangeShiftDown(index, range);
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

        public void AddBatchUnsafe(NativeSlice<TKey> keys, NativeSlice<TValue> values)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            CheckLengthsMatch(keys.Length, values.Length);
            DynamicHashMapHelper<TKey>.AddBatchUnsafe(_buffer, ref _helper, keys, values);
        }

        public void AddBatchUnsafe(NativeSlice<TKey> keys, NativeArray<TValue> values)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            CheckLengthsMatch(keys.Length, values.Length);
            DynamicHashMapHelper<TKey>.AddBatchUnsafe(_buffer, ref _helper, keys, values);
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

        public readonly DynamicHashMapEnumerator<TKey, TValue> GetEnumerator()
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return new DynamicHashMapEnumerator<TKey, TValue>(_helper);
        }

        public TValue* GetUnsafeValuePtr()
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return (TValue*)_helper->Values;
        }

        public TKey* GetUnsafeKeyPtr()
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->Keys;
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
            var ptr = _buffer.GetPtr();
            if (_helper != ptr)
            {
                throw new ArgumentException("DynamicHashMap was not passed by ref when doing a resize and is now invalid");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
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
        private static void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckLengthsMatch(int keys, int values)
        {
            if (keys != values)
            {
                throw new ArgumentException("Key and value array don't match");
            }
        }
    }

    internal sealed unsafe class DynamicHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicHashMapHelper<TKey>* _helper;

        public DynamicHashMapDebuggerTypeProxy(DynamicHashMap<TKey, TValue> target)
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
