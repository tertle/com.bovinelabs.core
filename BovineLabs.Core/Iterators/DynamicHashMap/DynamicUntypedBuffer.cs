namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct DynamicUntypedBuffer
    {
        private readonly DynamicBuffer<byte> _buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicUntypedBufferHelper* _helper;

        internal DynamicUntypedBuffer(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            _buffer = buffer;
            _helper = buffer.AsUntypedBufferHelper();
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

        public readonly int Length
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
                DynamicUntypedBufferHelper.Resize(_buffer, ref _helper, value);
            }
        }

        internal DynamicUntypedBufferHelper* Helper => _helper;

        public void Clear()
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->Clear();
        }

        public int Add<TValue>(TValue value)
            where TValue : unmanaged
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            return DynamicUntypedBufferHelper.Add(_buffer, ref _helper, value);
        }

        public ref TValue ElementAt<TValue>(int index)
            where TValue : unmanaged
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            return ref DynamicUntypedBufferHelper.GetValue<TValue>(_helper, index);
        }

        public ref readonly TValue ElementAtRO<TValue>(int index)
            where TValue : unmanaged
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return ref DynamicUntypedBufferHelper.GetValue<TValue>(_helper, index);
        }

        public void Set<TValue>(int index, TValue value)
            where TValue : unmanaged
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            DynamicUntypedBufferHelper.SetValue(_helper, index, value);
        }

        public void RemoveAt(int index)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            DynamicUntypedBufferHelper.RemoveAt(ref _helper, index);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            if (_helper != _buffer.GetPtr())
            {
                throw new ArgumentException("DynamicUntypedBuffer was not passed by ref when doing a resize and is now invalid");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicUntypedBufferHelper>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }
    }
}
