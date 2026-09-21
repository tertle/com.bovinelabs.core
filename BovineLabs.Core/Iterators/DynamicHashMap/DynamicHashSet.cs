namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicHashSetDebuggerTypeProxy<>))]
    public unsafe struct DynamicHashSet<T> : IEnumerable<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly DynamicBuffer<byte> _buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<T>* _helper;

        internal DynamicHashSet(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            _buffer = buffer;
            _helper = buffer.AsHelper<T>();
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
                DynamicHashMapHelper<T>.Resize(_buffer, ref _helper, value);
            }
        }

        internal readonly DynamicHashMapHelper<T>* Helper => _helper;

        public readonly void Clear()
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->Clear();
        }

        public bool Add(T item)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            return DynamicHashMapHelper<T>.TryAdd(_buffer, ref _helper, item) != -1;
        }

        public readonly bool Remove(T item)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            return _helper->TryRemove(item) != -1;
        }

        public readonly bool Contains(T item)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->Find(item) != -1;
        }

        public void Flatten()
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            DynamicHashMapHelper<T>.Flatten(_buffer, ref _helper);
        }

        public readonly NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->GetKeyArray(allocator);
        }

        public readonly DynamicHashSetEnumerator<T> GetEnumerator()
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return new DynamicHashSetEnumerator<T>(_helper);
        }

        /// <summary>
        /// Not implemented; use the concrete GetEnumerator instead.
        /// </summary>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
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
                throw new ArgumentException("DynamicHashSet was not passed by ref when doing a resize and is now invalid");
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

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicHashMapHelper<T>>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }
    }

    internal sealed unsafe class DynamicHashSetDebuggerTypeProxy<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly DynamicHashMapHelper<T>* _helper;

        public DynamicHashSetDebuggerTypeProxy(DynamicHashSet<T> target)
        {
            _helper = target.Helper;
        }

        public List<T> Items
        {
            get
            {
                var result = new List<T>();

                if (_helper == null)
                {
                    return result;
                }

                using var items = _helper->GetKeyArray(Allocator.Temp);

                for (var i = 0; i < items.Length; ++i)
                {
                    result.Add(items[i]);
                }

                return result;
            }
        }
    }
}
