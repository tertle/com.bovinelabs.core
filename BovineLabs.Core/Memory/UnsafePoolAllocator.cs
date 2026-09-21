namespace BovineLabs.Core.Memory
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    public unsafe struct UnsafePoolAllocator<T> : IDisposable
        where T : unmanaged
    {
        private UnsafeSlabAllocator<T> _slabAllocator;
        private UnsafeParallelHashSet<Ptr> _free;

        public UnsafePoolAllocator(int countPerChunk, Allocator allocator)
        {
            Debug.Assert(countPerChunk > 0);

            _slabAllocator = new UnsafeSlabAllocator<T>(countPerChunk, allocator);
            _free = new UnsafeParallelHashSet<Ptr>(0, allocator);
        }

        public bool IsCreated => _slabAllocator.IsCreated;

        public void Dispose()
        {
            _slabAllocator.Dispose();
            _free.Dispose();
            _slabAllocator = default;
            _free = default;
        }

        public T* Alloc()
        {
            using var e = _free.GetEnumerator();
            if (!e.MoveNext())
            {
                // No free allocations, need to allocate new
                return _slabAllocator.Alloc();
            }

            var ptr = e.Current;
            _free.Remove(ptr);
            return (T*)ptr;
        }

        public void Free(T* p)
        {
            _free.Add(new Ptr(p));
        }

        public int Allocated()
        {
            return _slabAllocator.Allocated();
        }
    }
}
