// <copyright file="DynamicBufferInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class DynamicBufferInternal
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static DynamicBuffer<T> Create<T>(
            void* header,
            AtomicSafetyHandle safety,
            AtomicSafetyHandle arrayInvalidationSafety,
            bool isReadOnly,
            bool useMemoryInitPattern,
            byte memoryInitPattern,
            int internalCapacity)
            where T : struct
        {
            return new DynamicBuffer<T>((BufferHeader*)header, safety, arrayInvalidationSafety, isReadOnly, useMemoryInitPattern, memoryInitPattern, internalCapacity);
        }
#else
        public static DynamicBuffer<T> Create<T>(void* header, int internalCapacity)
            where T : struct
        {
            return new DynamicBuffer<T>((BufferHeader*)header, internalCapacity);
        }
#endif
    }
}
