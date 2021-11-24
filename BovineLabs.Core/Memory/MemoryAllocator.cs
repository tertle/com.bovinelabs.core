// <copyright file="MemoryAllocator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    public unsafe struct MemoryAllocator : IDisposable
    {
        private readonly Allocator allocator;
        private NativeHashSet<Ptr> allocated;

        public MemoryAllocator(Allocator allocator)
        {
            this.allocator = allocator;
            this.allocated = new NativeHashSet<Ptr>(0, allocator);
        }

        public Allocator Allocator => this.allocator;

        public void* Malloc(long size, int alignOf)
        {
            var ptr = UnsafeUtility.Malloc(size, alignOf, this.Allocator);
            this.allocated.Add(ptr);
            return ptr;
        }

        public T* Create<T>(int count = 1)
            where T : unmanaged
        {
            Debug.Assert(count > 0);

            var ptr = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * count, UnsafeUtility.AlignOf<T>(), this.Allocator);
            this.allocated.Add(ptr);
            return ptr;
        }

        public void FreeAll()
        {
            using var array = this.allocated.ToNativeArray(Allocator.Temp);

            foreach (var ptr in array)
            {
                UnsafeUtility.Free(ptr, this.Allocator);
            }

            this.allocated.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.FreeAll();
            this.allocated.Dispose();
        }
    }
}
