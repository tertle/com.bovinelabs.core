// <copyright file="BufferAccessorExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class BufferAccessorExtensions
    {
        public static DynamicBuffer<T> GetUnsafe<T>(this BufferAccessor<T> bufferAccessor, int index)
            where T : unmanaged, IBufferElementData
        {
            var accessor = UnsafeUtility.As<BufferAccessor<T>, InternalBufferAccessor>(ref bufferAccessor);

            accessor.AssertIndexInRange(index);
            var hdr = (BufferHeader*)(accessor.BasePointer + (index * accessor.Stride));

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBuffer<T>(hdr, accessor.Safety0, accessor.ArrayInvalidationSafety, accessor.IsReadOnly == 1, false, 0, accessor.InternalCapacity);
#else
            return new DynamicBuffer<T>(hdr, accessor.InternalCapacity);
#endif
        }

        public static DynamicBuffer<T> GetUnsafeRW<T>(this BufferAccessor<T> bufferAccessor, int index)
            where T : unmanaged, IBufferElementData
        {
            var accessor = UnsafeUtility.As<BufferAccessor<T>, InternalBufferAccessor>(ref bufferAccessor);

            accessor.AssertIndexInRange(index);
            var hdr = (BufferHeader*)(accessor.BasePointer + (index * accessor.Stride));

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBuffer<T>(hdr, accessor.Safety0, accessor.ArrayInvalidationSafety, false, false, 0, accessor.InternalCapacity);
#else
            return new DynamicBuffer<T>(hdr, accessor.InternalCapacity);
#endif
        }

        [NativeContainer]
        private struct InternalBufferAccessor
        {
            [NativeDisableUnsafePtrRestriction]
            public byte* BasePointer;

            public int Length;
            public int Stride;
            public int InternalCapacity;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public byte IsReadOnly;
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public AtomicSafetyHandle Safety0;
            public AtomicSafetyHandle ArrayInvalidationSafety;

            public int SafetyReadOnlyCount;
            public int SafetyReadWriteCount;
#endif

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            public void AssertIndexInRange(int index)
            {
                if (Hint.Unlikely(index < 0 || index >= this.Length))
                {
                    throw new InvalidOperationException($"index {index} out of range in LowLevelBufferAccessor of length {this.Length}");
                }
            }
        }
    }
}
