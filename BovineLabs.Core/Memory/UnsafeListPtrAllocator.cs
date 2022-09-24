// <copyright file="UnsafeListPtrAllocator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using System;
    using BovineLabs.Core.Collections;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct UnsafeListPtrAllocator<T>
        where T : unmanaged
    {
        private NativeList<Ptr> ptrs;

        private readonly Allocator allocator;

        public UnsafeListPtrAllocator(Allocator allocator)
        {
            this.allocator = allocator;
            this.ptrs = new NativeList<Ptr>(this.allocator);
        }

        public UnsafeListPtr<T> Alloc()
        {
            var p = new UnsafeListPtr<T>(Allocator.Persistent);
            this.ptrs.Add(p.GetUnsafeList());
            return p;
        }

        public void Dispose()
        {
            foreach (var p in this.ptrs.AsArray())
            {
                UnsafeList<T>.Destroy((UnsafeList<T>*)p);
            }

            this.ptrs.Dispose();
        }

        public int Count => this.ptrs.Length;
    }
}
