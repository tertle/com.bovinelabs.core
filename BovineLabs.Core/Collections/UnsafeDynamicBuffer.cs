// <copyright file="UnsafeDynamicBuffer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    [DebuggerTypeProxy(typeof(DynamicBufferDebugView<>))]
    public unsafe struct UnsafeDynamicBuffer<T> : IQueryTypeParameter, IEnumerable<T>, INativeList<T>
        where T : unmanaged, IBufferElementData
    {
        [NoAlias]
        private BufferHeader* buffer;

        // Stores original internal capacity of the buffer header, so heap excess can be removed entirely when trimming.
        private int internalCapacity;

        internal UnsafeDynamicBuffer(BufferHeader* header, int internalCapacity)
        {
            this.buffer = header;
            this.internalCapacity = internalCapacity;
        }

        /// <summary> Gets or sets the number of elements the buffer holds. </summary>
        public int Length
        {
            readonly get => this.buffer->Length;
            set => this.ResizeUninitialized(value);
        }

        /// <summary> Gets or sets the number of elements the buffer can hold. </summary>
        public int Capacity
        {
            readonly get
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
                BufferHeader.SetCapacity(this.buffer, value, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), BufferHeader.TrashMode.RetainOldData, false,
                    0, this.internalCapacity);
            }
        }

        /// <summary> Gets a value indicating whether reports whether container is empty. </summary>
        /// <value> True if this container empty. </value>
        public readonly bool IsEmpty => !this.IsCreated || this.Length == 0;

        /// <summary> Gets a value indicating whether whether the memory for this dynamic buffer has been allocated. </summary>
        public readonly bool IsCreated => this.buffer != null;

        /// <summary> Array-like indexing operator. </summary>
        /// <param name="index"> The zero-based index. </param>
        public T this[int index]
        {
            readonly get
            {
                this.CheckBounds(index);
                return UnsafeUtility.ReadArrayElement<T>(BufferHeader.GetElementPointer(this.buffer), index);
            }

            set
            {
                this.CheckBounds(index);
                UnsafeUtility.WriteArrayElement(BufferHeader.GetElementPointer(this.buffer), index, value);
            }
        }

        /// <summary>
        /// Gets the reference to the element at the given index.
        /// </summary>
        /// <param name="index"> The zero-based index. </param>
        /// <returns> Returns the reference to the element at the index. </returns>
        public ref T ElementAt(int index)
        {
            this.CheckBounds(index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(BufferHeader.GetElementPointer(this.buffer), index);
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
                var sizeOf = UnsafeUtility.SizeOf<T>();
                UnsafeUtility.MemClear(ptr + (oldLength * sizeOf), num * sizeOf);
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
            BufferHeader.EnsureCapacity(this.buffer, length, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), BufferHeader.TrashMode.RetainOldData, false,
                0);
        }

        /// <summary> Sets the buffer length to zero. </summary>
        /// <remarks>
        /// The capacity of the buffer remains unchanged. Buffer memory
        /// is not overwritten.
        /// </remarks>
        public void Clear()
        {
            this.buffer->Length = 0;
        }

        /// <summary>
        /// Removes any excess capacity in the buffer.
        /// </summary>
        /// <remarks>
        /// Sets the buffer capacity to the current length.
        /// If the buffer memory size changes, the current contents
        /// of the buffer are copied to a new block of memory and the
        /// old memory is freed. If the buffer now fits in the space in the
        /// chunk reserved with <see cref="InternalBufferCapacityAttribute" />,
        /// then the buffer contents are moved to the chunk.
        /// </remarks>
        public void TrimExcess()
        {
            var oldPtr = this.buffer->Pointer;
            var length = this.buffer->Length;

            if (length == this.Capacity || oldPtr == null)
            {
                return;
            }

            var elemSize = UnsafeUtility.SizeOf<T>();
            var elemAlign = UnsafeUtility.AlignOf<T>();

            bool isInternal;
            byte* newPtr;

            // If the size fits in the internal buffer, prefer to move the elements back there.
            if (length <= this.internalCapacity)
            {
                newPtr = (byte*)(this.buffer + 1);
                isInternal = true;
            }
            else
            {
                newPtr = (byte*)Memory.Unmanaged.Allocate((long)elemSize * length, elemAlign, Allocator.Persistent);
                isInternal = false;
            }

            UnsafeUtility.MemCpy(newPtr, oldPtr, (long)elemSize * length);

            this.buffer->Capacity = Math.Max(length, this.internalCapacity);
            this.buffer->Pointer = isInternal ? null : newPtr;

            Memory.Unmanaged.Free(oldPtr, Allocator.Persistent);
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
        public int Add(T elem)
        {
            var length = this.Length;
            this.ResizeUninitialized(length + 1);
            this[length] = elem;
            return length;
        }

        /// <summary>
        /// Inserts an element at the specified index, resizing as necessary.
        /// </summary>
        /// <remarks> The buffer is resized if it has no additional capacity. </remarks>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.insert" />
        /// </example>
        /// <param name="index"> The position at which to insert the new element. </param>
        /// <param name="elem"> The element to add to the buffer. </param>
        public void Insert(int index, T elem)
        {
            var length = this.Length;
            this.ResizeUninitialized(length + 1);
            this.CheckBounds(index); // CheckBounds after ResizeUninitialized since index == length is allowed
            var elemSize = UnsafeUtility.SizeOf<T>();
            var basePtr = BufferHeader.GetElementPointer(this.buffer);
            UnsafeUtility.MemMove(basePtr + ((index + 1) * elemSize), basePtr + (index * elemSize), (long)elemSize * (length - index));
            this[index] = elem;
        }

        /// <summary>
        /// Adds all the elements from <paramref name="newElems" /> to the end
        /// of the buffer, resizing as necessary.
        /// </summary>
        /// <remarks> The buffer is resized if it has no additional capacity. </remarks>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.addrange" />
        /// </example>
        /// <param name="newElems"> The native array of elements to insert. </param>
        public void AddRange(NativeArray<T> newElems)
        {
            var elemSize = UnsafeUtility.SizeOf<T>();
            var oldLength = this.Length;
            this.ResizeUninitialized(oldLength + newElems.Length);

            var basePtr = BufferHeader.GetElementPointer(this.buffer);
            UnsafeUtility.MemCpy(basePtr + ((long)oldLength * elemSize), newElems.GetUnsafeReadOnlyPtr(), (long)elemSize * newElems.Length);
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

            var elemSize = UnsafeUtility.SizeOf<T>();
            var basePtr = BufferHeader.GetElementPointer(this.buffer);

            UnsafeUtility.MemMove(basePtr + (index * elemSize), basePtr + ((index + count) * elemSize), (long)elemSize * (this.Length - count - index));

            this.buffer->Length -= count;
        }

        /// <summary>
        /// Removes the specified number of elements, starting with the element at the specified index. It replaces the
        /// elements that were removed with a range of elements from the back of the buffer. This is more efficient
        /// than moving all elements following the removed elements, but does change the order of elements in the buffer.
        /// </summary>
        /// <remarks> The buffer capacity remains unchanged. </remarks>
        /// <param name="index"> The first element to remove. </param>
        /// <param name="count"> How many elements tot remove. </param>
        public void RemoveRangeSwapBack(int index, int count)
        {
            this.CheckBounds(index);
            if (count == 0)
            {
                return;
            }

            this.CheckBounds((index + count) - 1);

            ref var l = ref this.buffer->Length;
            var basePtr = BufferHeader.GetElementPointer(this.buffer);
            var elemSize = UnsafeUtility.SizeOf<T>();
            var copyFrom = math.max(l - count, index + count);
            void* dst = basePtr + (index * elemSize);
            void* src = basePtr + (copyFrom * elemSize);
            UnsafeUtility.MemMove(dst, src, (l - copyFrom) * elemSize);
            l -= count;
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
        /// Removes the element at the specified index and swaps the last element into its place. This is more efficient
        /// than moving all elements following the removed element, but does change the order of elements in the buffer.
        /// </summary>
        /// <param name="index"> The index of the element to remove. </param>
        public void RemoveAtSwapBack(int index)
        {
            this.CheckBounds(index);

            ref var l = ref this.buffer->Length;
            l -= 1;
            var newLength = l;
            if (index != newLength)
            {
                var basePtr = BufferHeader.GetElementPointer(this.buffer);
                UnsafeUtility.WriteArrayElement(basePtr, index, UnsafeUtility.ReadArrayElement<T>(basePtr, newLength));
            }
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void AssertReinterpretSizesMatch<TU>()
            where TU : struct
        {
            if (UnsafeUtility.SizeOf<TU>() != UnsafeUtility.SizeOf<T>())
            {
                throw new InvalidOperationException($"Types {typeof(TU)} and {typeof(T)} are of different sizes; cannot reinterpret");
            }
        }

        /// <summary>
        /// Returns a dynamic buffer of a different type, pointing to the same buffer memory.
        /// </summary>
        /// <remarks>
        /// No memory modification occurs. The reinterpreted type must be the same size
        /// in memory as the original type.
        /// </remarks>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.reinterpret" />
        /// </example>
        /// <typeparam name="TU"> The reinterpreted type. </typeparam>
        /// <returns> A dynamic buffer of the reinterpreted type. </returns>
        /// <exception cref="InvalidOperationException">
        /// If the reinterpreted type is a different
        /// size than the original.
        /// </exception>
        public readonly UnsafeDynamicBuffer<TU> Reinterpret<TU>()
            where TU : unmanaged, IBufferElementData
        {
            AssertReinterpretSizesMatch<TU>();
            return new UnsafeDynamicBuffer<TU>(this.buffer, this.internalCapacity);
        }

        /// <summary>
        /// Return a native array that aliases the original buffer contents.
        /// </summary>
        /// <remarks>
        /// You can only access the native array as long as the
        /// the buffer memory has not been reallocated. Several dynamic buffer operations,
        /// such as <see cref="Add" /> and <see cref="TrimExcess" /> can result in
        /// buffer reallocation.
        /// </remarks>
        /// <returns> A NativeArray view of this buffer. </returns>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.asnativearray" />
        /// </example>
        public readonly NativeArray<T> AsNativeArray()
        {
            var shadow = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(BufferHeader.GetElementPointer(this.buffer), this.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref shadow, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            return shadow;
        }

        /// <summary>
        /// Provides an enumerator for iterating over the buffer elements.
        /// </summary>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.getenumerator" />
        /// </example>
        /// <returns> The enumerator. </returns>
        public readonly NativeArray<T>.Enumerator GetEnumerator()
        {
            var array = this.AsNativeArray();
            return new NativeArray<T>.Enumerator(ref array);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary> Copies the buffer into a new native array. </summary>
        /// <param name="allocator">
        /// The type of memory allocation to use when creating the
        /// native array.
        /// </param>
        /// <returns> A native array containing copies of the buffer elements. </returns>
        public readonly NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            return CollectionHelper.CreateNativeArray(this.AsNativeArray(), allocator);
        }

        /// <summary>
        /// Copies all the elements from the specified native array into this dynamic buffer.
        /// </summary>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.copyfrom.nativearray" />
        /// </example>
        /// <param name="v"> The native array containing the elements to copy. </param>
        public void CopyFrom(NativeArray<T> v)
        {
            //todo remove workaround: See DOTS-1454
            this.ResizeUninitialized(v.Length);
            var vs = new NativeSlice<T>(v);
            vs.CopyTo(this.AsNativeArray());
        }

        /// <summary>
        /// Copies all the elements from the specified native slice into this dynamic buffer.
        /// </summary>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.copyfrom.nativeslice" />
        /// </example>
        /// <param name="v"> The native slice containing the elements to copy. </param>
        public void CopyFrom(NativeSlice<T> v)
        {
            this.ResizeUninitialized(v.Length);
            v.CopyTo(this.AsNativeArray());
        }

        /// <summary>
        /// Copies all the elements from another dynamic buffer.
        /// </summary>
        /// <example>
        ///     <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.copyfrom.dynamicbuffer" />
        /// </example>
        /// <param name="v"> The dynamic buffer containing the elements to copy. </param>
        public void CopyFrom(UnsafeDynamicBuffer<T> v)
        {
            this.ResizeUninitialized(v.Length);

            UnsafeUtility.MemCpy(BufferHeader.GetElementPointer(this.buffer), BufferHeader.GetElementPointer(v.buffer),
                this.Length * UnsafeUtility.SizeOf<T>());
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void CheckBounds(int index)
        {
            if ((uint)index >= (uint)this.Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in DynamicBuffer of '{this.Length}' Length.");
            }
        }
    }

    internal sealed class DynamicBufferDebugView<T>
        where T : unmanaged, IBufferElementData
    {
        private readonly UnsafeDynamicBuffer<T> buffer;

        public DynamicBufferDebugView(UnsafeDynamicBuffer<T> source)
        {
            this.buffer = source;
        }

        public T[] Items => this.buffer.AsNativeArray().ToArray();
    }
}
