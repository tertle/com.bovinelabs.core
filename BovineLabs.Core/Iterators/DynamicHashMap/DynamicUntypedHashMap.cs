namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct DynamicUntypedHashMap<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        private readonly DynamicBuffer<byte> _buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicUntypedHashMapHelper<TKey>* _helper;

        internal DynamicUntypedHashMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            _buffer = buffer;
            _helper = buffer.AsUntypedHelper<TKey>();
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
                DynamicUntypedHashMapHelper<TKey>.Resize(_buffer, ref _helper, value);
            }
        }

        internal DynamicUntypedHashMapHelper<TKey>* Helper => _helper;

        public void Add<TValue>(TKey key, TValue item)
            where TValue : unmanaged
        {
            DynamicUntypedHashMapHelper<TKey>.AddUnique(_buffer, ref _helper, key, item);
        }

        public void AddOrSet<TValue>(TKey key, TValue item)
            where TValue : unmanaged
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            DynamicUntypedHashMapHelper<TKey>.AddOrSet(_buffer, ref _helper, key, item);
        }

        public void AddOrSet(TKey key, void* value, int length)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            DynamicUntypedHashMapHelper<TKey>.AddOrSetRaw(_buffer, ref _helper, key, value, length);
        }

        /// <summary>
        /// The returned reference aliases map storage. Consume immediately; any later map write or capacity change invalidates it.
        /// </summary>
        public ref TValue GetOrAddRefUnsafe<TValue>(TKey key, TValue defaultValue = default)
            where TValue : unmanaged
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var idx = _helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicUntypedHashMapHelper<TKey>.AddUnique(_buffer, ref _helper, key, defaultValue);
            }

            return ref DynamicUntypedHashMapHelper<TKey>.GetValue<TValue>(_helper, idx);
        }

        public byte* GetOrAddRaw(TKey key, void* defaultValue, int length, out int storedLength)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var idx = _helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicUntypedHashMapHelper<TKey>.AddUniqueRaw(_buffer, ref _helper, key, defaultValue, length);
            }

            return DynamicUntypedHashMapHelper<TKey>.GetValueRaw(_helper, idx, out storedLength);
        }

        public readonly bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->TryGetValue(key, out item);
        }

        public readonly bool TryGetValue(TKey key, out byte* value, out int length)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->TryGetValueRaw(key, out value, out length);
        }

        public readonly bool ContainsKey(TKey key)
        {
            _buffer.CheckReadAccess();
            RefCheck();

            var idx = _helper->Find(key);
            return idx != -1;
        }

        public readonly bool Remove(TKey key)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            return _helper->TryRemove(key) != -1;
        }

        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->GetKeyArray(allocator);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            if (_helper != _buffer.GetPtr())
            {
                throw new ArgumentException("DynamicUntypedHashMap was not passed by ref when doing a resize and is now invalid");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicUntypedHashMapHelper<TKey>>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }
    }
}
