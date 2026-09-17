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
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicUntypedBufferHelper* helper;

        internal DynamicUntypedBuffer(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsUntypedBufferHelper();
        }

        /// <summary> Gets a value indicating whether this buffer has been allocated (and not yet deallocated). </summary>
        /// <value> True if this buffer has been allocated (and not yet deallocated). </value>
        public readonly bool IsCreated => this.buffer.IsCreated;

        /// <summary> Gets a value indicating whether this buffer is empty. </summary>
        /// <value> True if this buffer is empty or if the buffer has not been constructed. </value>
        public readonly bool IsEmpty
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return !this.IsCreated || this.helper->IsEmpty;
            }
        }

        /// <summary> Gets the current number of elements in this buffer. </summary>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.helper->Count;
            }
        }

        /// <summary> Gets or sets the number of elements that fit in the current allocation. </summary>
        /// <value> The number of elements that fit in the current allocation. </value>
        /// <param name="value"> A new capacity. Must be larger than the current capacity. </param>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.helper->Capacity;
            }

            set
            {
                this.buffer.CheckWriteAccess();
                this.RefCheck();
                DynamicUntypedBufferHelper.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal DynamicUntypedBufferHelper* Helper => this.helper;

        /// <summary> Removes all elements. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        /// <summary> Adds an element to the end of the buffer. </summary>
        /// <param name="value"> The value to add. </param>
        /// <typeparam name="TValue"> The value type. </typeparam>
        /// <returns> The index of the added element. </returns>
        public int Add<TValue>(TValue value)
            where TValue : unmanaged
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return DynamicUntypedBufferHelper.Add(this.buffer, ref this.helper, value);
        }

        /// <summary> Gets a writable reference to the element at the given index. </summary>
        /// <param name="index"> The zero-based index. </param>
        /// <typeparam name="TValue"> The value type. </typeparam>
        /// <returns> A writable reference to the element. </returns>
        public ref TValue ElementAt<TValue>(int index)
            where TValue : unmanaged
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return ref DynamicUntypedBufferHelper.GetValue<TValue>(this.helper, index);
        }

        /// <summary> Gets a readonly reference to the element at the given index. </summary>
        /// <param name="index"> The zero-based index. </param>
        /// <typeparam name="TValue"> The value type. </typeparam>
        /// <returns> A readonly reference to the element. </returns>
        public ref readonly TValue ElementAtRO<TValue>(int index)
            where TValue : unmanaged
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return ref DynamicUntypedBufferHelper.GetValue<TValue>(this.helper, index);
        }

        /// <summary> Sets the value for the element at the given index. </summary>
        /// <param name="index"> The zero-based index. </param>
        /// <param name="value"> The value to set. </param>
        /// <typeparam name="TValue"> The value type. </typeparam>
        public void Set<TValue>(int index, TValue value)
            where TValue : unmanaged
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicUntypedBufferHelper.SetValue(this.helper, index, value);
        }

        /// <summary> Removes the element at the specified index. </summary>
        /// <param name="index"> The index of the element to remove. </param>
        public void RemoveAt(int index)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicUntypedBufferHelper.RemoveAt(ref this.helper, index);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            if (this.helper != this.buffer.GetPtr())
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
