namespace BovineLabs.Core.Collections
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// Pool for <see cref="UnsafeList{T}" /> that wraps <see cref="UnmanagedPool{T}" /> and provides list lifecycle helpers.
    /// </summary>
    /// <typeparam name="T">The element type in the pooled list.</typeparam>
    public readonly struct UnsafeListPool<T> : IDisposable
        where T : unmanaged
    {
        private readonly UnmanagedPool<UnsafeList<T>> pool;

        public UnsafeListPool(int capacity, Allocator allocator = Allocator.Persistent)
        {
            this.pool = new UnmanagedPool<UnsafeList<T>>(capacity, allocator);
        }

        public bool IsCreated => this.pool.IsCreated;

        public void Dispose()
        {
            while (this.pool.TryGet(out var list))
            {
                list.Dispose();
            }

            this.pool.Dispose();
        }

        public bool TryAdd(UnsafeList<T> element)
        {
            return this.pool.TryAdd(element);
        }

        public bool TryGet(out UnsafeList<T> element)
        {
            return this.pool.TryGet(out element);
        }

        public UnsafeList<T> GetOrCreate(int minimumCapacity, AllocatorManager.AllocatorHandle listAllocator)
        {
            if (this.TryGet(out var list))
            {
                return list;
            }

            return new UnsafeList<T>(minimumCapacity, listAllocator);
        }

        public void ReturnOrDispose(UnsafeList<T> list)
        {
            if (this.TryAdd(list))
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
