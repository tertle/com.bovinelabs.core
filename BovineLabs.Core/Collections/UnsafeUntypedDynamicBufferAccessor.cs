namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct UnsafeUntypedDynamicBufferAccessor
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly byte* _pointer;

        private readonly int _internalCapacity;
        private readonly int _stride;

        public int Length { get; }

        public int ElementSize { get; }

        internal UnsafeUntypedDynamicBufferAccessor(byte* basePointer, int length, int stride, int elementSize, int internalCapacity)
        {
            _pointer = basePointer;
            _internalCapacity = internalCapacity;
            ElementSize = elementSize;
            _stride = stride;
            Length = length;
        }

        public UnsafeUntypedDynamicBuffer GetUntypedBuffer(int index)
        {
            AssertIndexInRange(index);
            var header = (BufferHeader*)(_pointer + (index * _stride));

            return new UnsafeUntypedDynamicBuffer(header, _internalCapacity, ElementSize, UntypedDynamicBuffer.AlignOf);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void AssertIndexInRange(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new InvalidOperationException($"index {index} out of range in LowLevelBufferAccessor of length {Length}");
            }
        }
    }
}
