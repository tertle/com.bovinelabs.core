namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct DynamicBufferAccessor
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly byte* _pointer;

        private readonly int _internalCapacity;
        private readonly int _stride;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly AtomicSafetyHandle _safety0;
        private readonly AtomicSafetyHandle _arrayInvalidationSafety;
        private readonly bool _isReadOnly;
#endif

        public int Length { get; }

        public int ElementSize { get; }

        public int ElementAlign { get; }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal DynamicBufferAccessor(
            byte* basePointer, int length, int stride, int elementSize, int elementAlign, int internalCapacity, bool readOnly, AtomicSafetyHandle safety0,
            AtomicSafetyHandle arrayInvalidationSafety)
        {
            _pointer = basePointer;
            _internalCapacity = internalCapacity;
            ElementSize = elementSize;
            ElementAlign = elementAlign;
            _stride = stride;
            Length = length;
            _safety0 = safety0;
            _arrayInvalidationSafety = arrayInvalidationSafety;
            _isReadOnly = readOnly;
        }
#else
        internal DynamicBufferAccessor(byte* basePointer, int length, int stride, int elementSize, int elementAlign, int internalCapacity)
        {
            _pointer = basePointer;
            _internalCapacity = internalCapacity;
            ElementSize = elementSize;
            ElementAlign = elementAlign;
            _stride = stride;
            Length = length;
        }
#endif

        public DynamicBuffer<T> GetBuffer<T>(int index)
            where T : unmanaged
        {
            CheckWriteAccess();
            AssertIndexInRange(index);
            var header = (BufferHeader*)(_pointer + (index * _stride));

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBuffer<T>(header, _safety0, _arrayInvalidationSafety, _isReadOnly, false, 0, _internalCapacity);
#else
            return new DynamicBuffer<T>(header, _internalCapacity);
#endif
        }

        public UntypedDynamicBuffer GetUntypedBuffer(int index)
        {
            CheckWriteAccess();
            AssertIndexInRange(index);
            var header = (BufferHeader*)(_pointer + (index * _stride));

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new UntypedDynamicBuffer(header, _safety0, _arrayInvalidationSafety, _isReadOnly, false, 0, _internalCapacity,
                ElementSize, ElementAlign);
#else
            return new UntypedDynamicBuffer(header, _internalCapacity, ElementSize, ElementAlign);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWriteAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(_safety0);
            AtomicSafetyHandle.CheckWriteAndThrow(_arrayInvalidationSafety);
#endif
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
