namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public unsafe struct UnsafeUntypedDynamicBuffer
    {
        [NativeDisableUnsafePtrRestriction]
        [NoAlias]
        private readonly BufferHeader* _buffer;

        // Stores original internal capacity of the buffer header, so heap excess can be removed entirely when trimming.
        private readonly int _internalCapacity;

        private readonly int _alignOf;

        internal UnsafeUntypedDynamicBuffer(BufferHeader* header, int internalCapacity, int elementSize, int alignOf)
        {
            _buffer = header;
            _internalCapacity = internalCapacity;
            ElementSize = elementSize;
            _alignOf = alignOf;
        }

        public int Length
        {
            get => _buffer->Length;
            set => ResizeUninitialized(value);
        }

        public int Capacity
        {
            get
            {
                return _buffer->Capacity;
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (value < Length)
                {
                    throw new InvalidOperationException($"Capacity {value} can't be set smaller than Length {Length}");
                }
#endif
                BufferHeader.SetCapacity(_buffer, value, ElementSize, _alignOf, BufferHeader.TrashMode.RetainOldData, false, 0,
                    _internalCapacity);
            }
        }

        public bool IsEmpty => !IsCreated || Length == 0;

        public bool IsCreated => _buffer != null;

        public int ElementSize { get; }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void CheckBounds(int index)
        {
            if ((uint)index >= (uint)Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in DynamicBuffer of '{Length}' Length.");
            }
        }

        public void* this[int index]
        {
            get
            {
                CheckBounds(index);
                return BufferHeader.GetElementPointer(_buffer) + (index * ElementSize);
            }

            set
            {
                CheckBounds(index);
                var dst = BufferHeader.GetElementPointer(_buffer) + (index * ElementSize);
                UnsafeUtility.MemCpy(dst, value, ElementSize);
            }
        }

        public void ResizeUninitialized(int length)
        {
            EnsureCapacity(length);
            _buffer->Length = length;
        }

        public void Resize(int length, NativeArrayOptions options)
        {
            EnsureCapacity(length);

            var oldLength = _buffer->Length;
            _buffer->Length = length;
            if (options == NativeArrayOptions.ClearMemory && oldLength < length)
            {
                var num = length - oldLength;
                var ptr = BufferHeader.GetElementPointer(_buffer);
                UnsafeUtility.MemClear(ptr + (oldLength * ElementSize), num * ElementSize);
            }
        }

        public void EnsureCapacity(int length)
        {
            BufferHeader.EnsureCapacity(_buffer, length, ElementSize, _alignOf, BufferHeader.TrashMode.RetainOldData, false, 0);
        }

        /// <summary>
        /// Does not overwrite the cleared memory or shrink capacity.
        /// </summary>
        public void Clear()
        {
            _buffer->Length = 0;
        }

        public int Add(void* elem)
        {
            var length = Length;
            ResizeUninitialized(length + 1);
            this[length] = elem;
            return length;
        }

        public void AddRange(void* elem, int count)
        {
            var oldLength = Length;
            ResizeUninitialized(oldLength + count);

            void* basePtr = BufferHeader.GetElementPointer(_buffer) + (oldLength * ElementSize);
            UnsafeUtility.MemCpy(basePtr, elem, (long)ElementSize * count);
        }

        public void RemoveRange(int index, int count)
        {
            CheckBounds(index);
            if (count == 0)
            {
                return;
            }

            CheckBounds((index + count) - 1);

            var elemSize = ElementSize;
            var basePtr = BufferHeader.GetElementPointer(_buffer);

            UnsafeUtility.MemMove(basePtr + (index * elemSize), basePtr + ((index + count) * elemSize), (long)elemSize * (Length - count - index));

            _buffer->Length -= count;
        }

        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafePtr()
        {
            return BufferHeader.GetElementPointer(_buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafeReadOnlyPtr()
        {
            return BufferHeader.GetElementPointer(_buffer);
        }
    }
}
