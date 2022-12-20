// <copyright file="MemoryAllocator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine;

    public unsafe struct MemoryAllocator : IDisposable
    {
        private NativeHashSet<Ptr> allocated;

        public MemoryAllocator(Allocator allocator)
        {
            this.Allocator = allocator;
            this.allocated = new NativeHashSet<Ptr>(0, allocator);
        }

        public Allocator Allocator { get; }

        public void* Allocate(int itemSizeInBytes, int alignmentInBytes, int items = 1)
        {
            var ptr = AllocatorManager.Allocate(this.Allocator, itemSizeInBytes, alignmentInBytes, items);
            this.allocated.Add(ptr);
            return ptr;
        }

        public T* Create<T>(int count = 1)
            where T : unmanaged
        {
            Debug.Assert(count > 0);
            return (T*)this.Allocate(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), count);
        }

        // TODO turn into array
        public UnsafeList<T> CreateList<T>(int capacity)
            where T : unmanaged
        {
            var newCapacity = math.max(capacity, 64 / UnsafeUtility.SizeOf<T>());
            newCapacity = math.ceilpow2(newCapacity);

            var buffer = this.Create<T>(newCapacity);

            return new UnsafeList<T>
            {
                Ptr = buffer,
                m_capacity = newCapacity,
                Allocator = Allocator.None,
            };
        }

        public void FreeAll()
        {
            using var array = this.allocated.ToNativeArray(Allocator.Temp);

            foreach (var ptr in array)
            {
                AllocatorManager.Free(this.Allocator, ptr);
            }

            this.allocated.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.FreeAll();
            this.allocated.Dispose();
        }
    }
}
