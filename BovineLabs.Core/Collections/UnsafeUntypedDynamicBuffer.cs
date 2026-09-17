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
        private readonly BufferHeader* buffer;

        // Stores original internal capacity of the buffer header, so heap excess can be removed entirely when trimming.
        private readonly int internalCapacity;

        private readonly int alignOf;

        internal UnsafeUntypedDynamicBuffer(BufferHeader* header, int internalCapacity, int elementSize, int alignOf)
        {
            this.buffer = header;
            this.internalCapacity = internalCapacity;
            this.ElementSize = elementSize;
            this.alignOf = alignOf;
        }

        public int Length
        {
            get => this.buffer->Length;
            set => this.ResizeUninitialized(value);
        }

        public int Capacity
        {
            get
            {
                return this.buffer->Capacity;
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (value < this.Length)
                {
                    throw new InvalidOperationException($"Capacity {value} can't be set smaller than Length {this.Length}");
                }
#endif
                BufferHeader.SetCapacity(this.buffer, value, this.ElementSize, this.alignOf, BufferHeader.TrashMode.RetainOldData, false, 0,
                    this.internalCapacity);
            }
        }

        public bool IsEmpty => !this.IsCreated || this.Length == 0;

        public bool IsCreated => this.buffer != null;

        public int ElementSize { get; }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void CheckBounds(int index)
        {
            if ((uint)index >= (uint)this.Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in DynamicBuffer of '{this.Length}' Length.");
            }
        }

        public void* this[int index]
        {
            get
            {
                this.CheckBounds(index);
                return BufferHeader.GetElementPointer(this.buffer) + (index * this.ElementSize);
            }

            set
            {
                this.CheckBounds(index);
                var dst = BufferHeader.GetElementPointer(this.buffer) + (index * this.ElementSize);
                UnsafeUtility.MemCpy(dst, value, this.ElementSize);
            }
        }

        public void ResizeUninitialized(int length)
        {
            this.EnsureCapacity(length);
            this.buffer->Length = length;
        }

        public void Resize(int length, NativeArrayOptions options)
        {
            this.EnsureCapacity(length);

            var oldLength = this.buffer->Length;
            this.buffer->Length = length;
            if (options == NativeArrayOptions.ClearMemory && oldLength < length)
            {
                var num = length - oldLength;
                var ptr = BufferHeader.GetElementPointer(this.buffer);
                UnsafeUtility.MemClear(ptr + (oldLength * this.ElementSize), num * this.ElementSize);
            }
        }

        public void EnsureCapacity(int length)
        {
            BufferHeader.EnsureCapacity(this.buffer, length, this.ElementSize, this.alignOf, BufferHeader.TrashMode.RetainOldData, false, 0);
        }

        /// <summary>
        /// Does not overwrite the cleared memory or shrink capacity.
        /// </summary>
        public void Clear()
        {
            this.buffer->Length = 0;
        }

        public int Add(void* elem)
        {
            var length = this.Length;
            this.ResizeUninitialized(length + 1);
            this[length] = elem;
            return length;
        }

        public void AddRange(void* elem, int count)
        {
            var oldLength = this.Length;
            this.ResizeUninitialized(oldLength + count);

            void* basePtr = BufferHeader.GetElementPointer(this.buffer) + (oldLength * this.ElementSize);
            UnsafeUtility.MemCpy(basePtr, elem, (long)this.ElementSize * count);
        }

        public void RemoveRange(int index, int count)
        {
            this.CheckBounds(index);
            if (count == 0)
            {
                return;
            }

            this.CheckBounds((index + count) - 1);

            var elemSize = this.ElementSize;
            var basePtr = BufferHeader.GetElementPointer(this.buffer);

            UnsafeUtility.MemMove(basePtr + (index * elemSize), basePtr + ((index + count) * elemSize), (long)elemSize * (this.Length - count - index));

            this.buffer->Length -= count;
        }

        public void RemoveAt(int index)
        {
            this.RemoveRange(index, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafePtr()
        {
            return BufferHeader.GetElementPointer(this.buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafeReadOnlyPtr()
        {
            return BufferHeader.GetElementPointer(this.buffer);
        }
    }
}
