namespace BovineLabs.Core.Memory
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct UnsafeFixedPoolAllocator<T> : IDisposable
        where T : unmanaged
    {
        private readonly int _maxItems;
        private readonly AllocatorManager.AllocatorHandle _allocator;

        private Ptr _buffer;
        private UnsafeParallelHashSet<Ptr> _freeIndex;

        public UnsafeFixedPoolAllocator(int maxItems, Allocator allocator)
        {
            ValidateSize(maxItems);

            _maxItems = maxItems;
            _allocator = allocator;
            _freeIndex = new UnsafeParallelHashSet<Ptr>(maxItems, allocator);

            _buffer = CollectionMemory.Allocate(UnsafeUtility.SizeOf<T>() * maxItems, UnsafeUtility.AlignOf<T>(), allocator);

            for (var i = 0; i < maxItems; i++)
            {
                _freeIndex.Add((T*)_buffer + i);
            }
        }

        public bool IsCreated => _buffer.Value != null;

        public T* Alloc()
        {
            // ReSharper disable once UseMethodAny.2
            if (_freeIndex.Count() == 0)
            {
                return null;
            }

            using var e = _freeIndex.GetEnumerator();
            if (!e.MoveNext())
            {
                return null;
            }

            var ptr = e.Current;
            _freeIndex.Remove(ptr);
            return (T*)ptr;
        }

        public void Free(T* p)
        {
            ValidatePtr(p);
            _freeIndex.Add(p);
        }

        public void Dispose()
        {
            CollectionMemory.Free(_buffer, _allocator);
            _freeIndex.Dispose();
            _buffer = Ptr.Zero;
            _freeIndex = default;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ValidateSize(int maxItems)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (maxItems <= 0)
            {
                throw new ArgumentException("Null pointer");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidatePtr(T* p)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (p == null)
            {
                throw new ArgumentException("Null pointer");
            }

            if (p < _buffer || p >= (T*)_buffer + _maxItems)
            {
                throw new ArgumentException("Ptr not from this allocator");
            }

            if (_freeIndex.Contains(p))
            {
                throw new ArgumentException("Ptr already returned");
            }

            // This shouldn't be possible due to above checks
            if (_freeIndex.Count() == _maxItems)
            {
                throw new ArgumentException("More free than in Buffer");
            }
#endif
        }
    }
}
