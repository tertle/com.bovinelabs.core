// <copyright file="UnsafeUntypedDynamicBufferAccessor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct UnsafeUntypedDynamicBufferAccessor
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly byte* pointer;

        private readonly int internalCapacity;
        private readonly int stride;

        /// <summary> The number of buffers in the chunk. </summary>
        public int Length { get; }

        /// <summary> The size (in bytes) of a single buffer element. </summary>
        public int ElementSize { get; }

        internal UnsafeUntypedDynamicBufferAccessor(byte* basePointer, int length, int stride, int elementSize, int internalCapacity)
        {
            this.pointer = basePointer;
            this.internalCapacity = internalCapacity;
            this.ElementSize = elementSize;
            this.stride = stride;
            this.Length = length;
        }

        public UnsafeUntypedDynamicBuffer GetUntypedBuffer(int index)
        {
            this.AssertIndexInRange(index);
            var header = (BufferHeader*)(this.pointer + (index * this.stride));

            return new UnsafeUntypedDynamicBuffer(header, this.internalCapacity, this.ElementSize, UntypedDynamicBuffer.AlignOf);
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
