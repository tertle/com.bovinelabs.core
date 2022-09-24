// <copyright file="NativeSlabAllocator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    [NativeContainer]
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct NativeSlabAllocator<T> : IDisposable
        where T : unmanaged
    {
        private UnsafeSlabAllocator<T> slabAllocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
#pragma warning disable SA1308
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by safety")]
        private AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by safety")]
        private DisposeSentinel m_DisposeSentinel;
#pragma warning restore SA1308
#endif

        public NativeSlabAllocator(int countPerSlab, Allocator allocator)
        {
            Debug.Assert(countPerSlab > 0);

            this.slabAllocator = new UnsafeSlabAllocator<T>(countPerSlab, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out this.m_Safety, out this.m_DisposeSentinel, 1, allocator);
#endif
        }

        public int AllocationCount
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
                return this.slabAllocator.AllocationCount;
            }
        }

        public bool IsCreated => this.slabAllocator.IsCreated;

        /// <summary> Returns a pointer. This memory is not cleared. </summary>
        /// <returns> The pointer. </returns>
        public T* Alloc()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
            return this.slabAllocator.Alloc();
        }

        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
            this.slabAllocator.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref this.m_Safety, ref this.m_DisposeSentinel);
#endif
            this.slabAllocator.Dispose();
        }
    }
}
