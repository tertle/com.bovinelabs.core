// <copyright file="UnsafeUntypedDynamicBuffer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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

        /// <summary> The number of elements the buffer holds. </summary>
        public int Length
        {
            get => this.buffer->Length;
            set => this.ResizeUninitialized(value);
        }

        /// <summary>
        /// The number of elements the buffer can hold.
        /// </summary>
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

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value> True if this container empty. </value>
        public bool IsEmpty => !this.IsCreated || this.Length == 0;

        /// <summary>
        /// Whether the memory for this dynamic buffer has been allocated.
        /// </summary>
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

        /// <summary>
        /// Array-like indexing operator.
        /// </summary>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.indexoperator" />
        /// </example>
        /// <param name="index"> The zero-based index. </param>
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

        /// <summary>
        /// Sets the length of this buffer, increasing the capacity if necessary.
        /// </summary>
        /// <remarks>
        /// If <paramref name="length" /> is less than the current
        /// length of the buffer, the length of the buffer is reduced while the
        /// capacity remains unchanged.
        /// </remarks>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.resizeuninitialized" />
        /// </example>
        /// <param name="length"> The new length of the buffer. </param>
        public void ResizeUninitialized(int length)
        {
            this.EnsureCapacity(length);
            this.buffer->Length = length;
        }

        /// <summary>
        /// Sets the length of this buffer, increasing the capacity if necessary.
        /// </summary>
        /// <remarks>
        /// If <paramref name="length" /> is less than the current
        /// length of the buffer, the length of the buffer is reduced while the
        /// capacity remains unchanged.
        /// </remarks>
        /// <param name="length"> The new length of this buffer. </param>
        /// <param name="options"> Whether to clear any newly allocated bytes to all zeroes. </param>
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

        /// <summary>
        /// Ensures that the buffer has at least the specified capacity.
        /// </summary>
        /// <remarks>
        /// If <paramref name="length" /> is greater than the current <see cref="Capacity" />
        /// of this buffer and greater than the capacity reserved with
        /// <see cref="InternalBufferCapacityAttribute" />, this function allocates a new memory block
        /// and copies the current buffer to it. The number of elements in the buffer remains
        /// unchanged.
        /// </remarks>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.reserve" />
        /// </example>
        /// <param name="length"> The buffer capacity is ensured to be at least this big. </param>
        public void EnsureCapacity(int length)
        {
            BufferHeader.EnsureCapacity(this.buffer, length, this.ElementSize, this.alignOf, BufferHeader.TrashMode.RetainOldData, false, 0);
        }

        /// <summary>
        /// Sets the buffer length to zero.
        /// </summary>
        /// <remarks>
        /// The capacity of the buffer remains unchanged. Buffer memory
        /// is not overwritten.
        /// </remarks>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.clear" />
        /// </example>
        public void Clear()
        {
            this.buffer->Length = 0;
        }

        /// <summary>
        /// Adds an element to the end of the buffer, resizing as necessary.
        /// </summary>
        /// <remarks> The buffer is resized if it has no additional capacity. </remarks>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.add" />
        /// </example>
        /// <param name="elem"> The element to add to the buffer. </param>
        /// <returns> The index of the added element, which is equal to the new length of the buffer minus one. </returns>
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

        /// <summary>
        /// Removes the specified number of elements, starting with the element at the specified index.
        /// </summary>
        /// <remarks> The buffer capacity remains unchanged. </remarks>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.removerange" />
        /// </example>
        /// <param name="index"> The first element to remove. </param>
        /// <param name="count"> How many elements tot remove. </param>
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

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.removeat" />
        /// </example>
        /// <param name="index"> The index of the element to remove. </param>
        public void RemoveAt(int index)
        {
            this.RemoveRange(index, 1);
        }

        /// <summary>
        /// Gets an <see langword="unsafe" /> read/write pointer to the contents of the buffer.
        /// </summary>
        /// <remarks> This function can only be called in unsafe code contexts. </remarks>
        /// <returns> A typed, unsafe pointer to the first element in the buffer. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafePtr()
        {
            return BufferHeader.GetElementPointer(this.buffer);
        }

        /// <summary>
        /// Gets an <see langword="unsafe" /> read-only pointer to the contents of the buffer.
        /// </summary>
        /// <remarks> This function can only be called in unsafe code contexts. </remarks>
        /// <returns> A typed, unsafe pointer to the first element in the buffer. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafeReadOnlyPtr()
        {
            return BufferHeader.GetElementPointer(this.buffer);
        }
    }
}
