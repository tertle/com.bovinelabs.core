// <copyright file="SlabAllocator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using System;
    using BovineLabs.Core.Collections;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    [NativeContainer]
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    public unsafe struct SlabAllocator<T> : IDisposable
        where T : unmanaged
    {
        private readonly int countPerSlab;
        private readonly Allocator allocator;
        private readonly UnsafeListPtr<IntPtr> slabs;

        [NativeDisableUnsafePtrRestriction]
        private readonly int* count;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        private DisposeSentinel m_DisposeSentinel;
#endif

        public SlabAllocator(int countPerSlab, Allocator allocator)
        {
            Debug.Assert(countPerSlab > 0);

            this.slabs = new UnsafeListPtr<IntPtr>(0, allocator);
            this.allocator = allocator;
            this.countPerSlab = countPerSlab;

            this.count = (int*)Memory.Unmanaged.Allocate(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), allocator);
            *this.count = countPerSlab;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out this.m_Safety, out this.m_DisposeSentinel, 1, allocator);
#endif
        }

        public int AllocationCount => (this.countPerSlab * (this.slabs.Length - 1)) + *this.count;

        public bool IsCreated => this.slabs.IsCreated;

        /// <summary>
        /// Returns a pointer
        /// Please note, this memory is not cleared.
        /// </summary>
        /// <returns></returns>
        public T* Alloc()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif

            if (*this.count == this.countPerSlab)
            {
                *this.count = 0;
                var ptr = (IntPtr)UnsafeUtility.Malloc(this.countPerSlab * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), this.allocator);
                this.slabs.Add(ptr);
            }

            var lastSlab = (T*)this.slabs[this.slabs.Length - 1].ToPointer();
            return lastSlab + (*this.count)++;
        }

        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif

            for (var i = 0; i < this.slabs.Length; i++)
            {
                UnsafeUtility.Free(this.slabs[i].ToPointer(), this.allocator);
            }

            this.slabs.Clear();
            *this.count = this.countPerSlab;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Clear();
            this.slabs.Dispose();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref this.m_Safety, ref this.m_DisposeSentinel);
#endif
            Memory.Unmanaged.Free(this.count, this.allocator);
        }
    }
}
