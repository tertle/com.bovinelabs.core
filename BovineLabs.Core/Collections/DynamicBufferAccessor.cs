// <copyright file="DynamicBufferAccessor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct DynamicBufferAccessor
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly byte* pointer;

        private readonly int internalCapacity;
        private readonly int stride;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly AtomicSafetyHandle safety0;
        private readonly AtomicSafetyHandle arrayInvalidationSafety;
        private readonly bool isReadOnly;
#endif

        /// <summary>
        /// The number of buffers in the chunk.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// The size (in bytes) of a single buffer element.
        /// </summary>
        public int ElementSize { get; }

        public int ElementAlign { get; }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal DynamicBufferAccessor(
            byte* basePointer, int length, int stride, int elementSize, int elementAlign, int internalCapacity, bool readOnly, AtomicSafetyHandle safety0,
            AtomicSafetyHandle arrayInvalidationSafety)
        {
            this.pointer = basePointer;
            this.internalCapacity = internalCapacity;
            this.ElementSize = elementSize;
            this.ElementAlign = elementAlign;
            this.stride = stride;
            this.Length = length;
            this.safety0 = safety0;
            this.arrayInvalidationSafety = arrayInvalidationSafety;
            this.isReadOnly = readOnly;
        }
#else
            internal DynamicBufferAccessor(byte* basePointer, int length, int stride, int elementSize, int elementAlign, int internalCapacity)
            {
                this.pointer = basePointer;
                this.internalCapacity = internalCapacity;
                this.ElementSize = elementSize;
                this.ElementAlign = elementAlign;
                this.stride = stride;
                this.Length = length;
            }
#endif

        public DynamicBuffer<T> GetBuffer<T>(int index)
            where T : unmanaged
        {
            this.CheckWriteAccess();
            this.AssertIndexInRange(index);
            var header = (BufferHeader*)(this.pointer + (index * this.stride));

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBuffer<T>(header, this.safety0, this.arrayInvalidationSafety, this.isReadOnly, false, 0, this.internalCapacity);
#else
            return new DynamicBuffer<T>(header, this.internalCapacity);
#endif
        }

        public UntypedDynamicBuffer GetUntypedBuffer(int index)
        {
            this.CheckWriteAccess();
            this.AssertIndexInRange(index);
            var header = (BufferHeader*)(this.pointer + (index * this.stride));

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new UntypedDynamicBuffer(header, this.safety0, this.arrayInvalidationSafety, this.isReadOnly, false, 0, this.internalCapacity,
                this.ElementSize, UntypedDynamicBuffer.AlignOf);
#else
            return new UntypedDynamicBuffer(header, this.internalCapacity, this.ElementSize, UntypedDynamicBuffer.AlignOf);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWriteAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.safety0);
            AtomicSafetyHandle.CheckWriteAndThrow(this.arrayInvalidationSafety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void AssertIndexInRange(int index)
        {
            if (index < 0 || index >= this.Length)
            {
                throw new InvalidOperationException($"index {index} out of range in LowLevelBufferAccessor of length {this.Length}");
            }
        }
    }
}
