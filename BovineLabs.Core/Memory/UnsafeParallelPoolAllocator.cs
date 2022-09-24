// <copyright file="UnsafeParallelPoolAllocator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct UnsafeParallelPoolAllocator<T> : IDisposable
        where T : unmanaged
    {
        private readonly Allocator allocator;

        [NativeDisableUnsafePtrRestriction]
        private UnsafePoolAllocator<T>* pools;

        [NativeSetThreadIndex]
        private int threadIndex;

        public UnsafeParallelPoolAllocator(int countPerChunk, Allocator allocator)
        {
            this.allocator = allocator;
            this.pools = (UnsafePoolAllocator<T>*)Memory.Unmanaged.Allocate(
                UnsafeUtility.SizeOf<UnsafePoolAllocator<T>>() * JobsUtility.MaxJobThreadCount,
                UnsafeUtility.AlignOf<UnsafePoolAllocator<T>>(),
                allocator);

            for (var i = 0; i < JobsUtility.MaxJobThreadCount; i++)
            {
                this.pools[i] = new UnsafePoolAllocator<T>(countPerChunk, allocator);
            }

            this.threadIndex = 0;
        }

        public bool IsCreated => this.pools != null;

        /// <inheritdoc/>
        public void Dispose()
        {
            for (var i = 0; i < JobsUtility.MaxJobThreadCount; i++)
            {
                this.pools[i].Dispose();
            }

            Memory.Unmanaged.Free(this.pools, this.allocator);
            this.pools = null;
        }

        public T* Alloc()
        {
            return this.pools[this.threadIndex].Alloc();
        }

        public void Free(T* p)
        {
            this.pools[this.threadIndex].Free(p);
        }
    }
}
