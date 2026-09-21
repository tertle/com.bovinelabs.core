namespace BovineLabs.Core.Collections
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public readonly struct UnsafeListPool<T> : IDisposable
        where T : unmanaged
    {
        private readonly UnmanagedPool<UnsafeList<T>> _pool;

        public UnsafeListPool(int capacity, Allocator allocator = Allocator.Persistent)
        {
            _pool = new UnmanagedPool<UnsafeList<T>>(capacity, allocator);
        }

        public bool IsCreated => _pool.IsCreated;

        public void Dispose()
        {
            while (_pool.TryGet(out var list))
            {
                list.Dispose();
            }

            _pool.Dispose();
        }

        public bool TryAdd(UnsafeList<T> element)
        {
            return _pool.TryAdd(element);
        }

        public bool TryGet(out UnsafeList<T> element)
        {
            return _pool.TryGet(out element);
        }

        public UnsafeList<T> GetOrCreate(int minimumCapacity, AllocatorManager.AllocatorHandle listAllocator)
        {
            if (TryGet(out var list))
            {
                return list;
            }

            return new UnsafeList<T>(minimumCapacity, listAllocator);
        }

        public void ReturnOrDispose(UnsafeList<T> list)
        {
            if (TryAdd(list))
            {
                return;
            }

            if (list.IsCreated)
            {
                list.Dispose();
            }
        }
    }
}
