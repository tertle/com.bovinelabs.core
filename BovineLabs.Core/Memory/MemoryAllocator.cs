namespace BovineLabs.Core.Memory
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine;

    public unsafe struct MemoryAllocator : IDisposable
    {
        private NativeHashSet<Ptr> _allocated;

        public MemoryAllocator(Allocator allocator)
        {
            Allocator = allocator;
            _allocated = new NativeHashSet<Ptr>(0, allocator);
        }

        public Allocator Allocator { get; }

        public void* Allocate(int itemSizeInBytes, int alignmentInBytes, int items = 1)
        {
            var ptr = AllocatorManager.Allocate(Allocator, itemSizeInBytes, alignmentInBytes, items);
            _allocated.Add(ptr);
            return ptr;
        }

        public T* Create<T>(int count = 1)
            where T : unmanaged
        {
            Debug.Assert(count > 0);
            return (T*)Allocate(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), count);
        }

        // TODO turn into array
        public UnsafeList<T> CreateList<T>(int capacity)
            where T : unmanaged
        {
            var newCapacity = math.max(capacity, 64 / UnsafeUtility.SizeOf<T>());
            newCapacity = math.ceilpow2(newCapacity);

            var buffer = Create<T>(newCapacity);

            return new UnsafeList<T>
            {
                Ptr = buffer,
                m_capacity = newCapacity,
                Allocator = Allocator.None,
            };
        }

        public void FreeAll()
        {
            using var array = _allocated.ToNativeArray(Allocator.Temp);

            foreach (var ptr in array)
            {
                AllocatorManager.Free(Allocator, ptr);
            }

            _allocated.Clear();
        }

        public void Dispose()
        {
            FreeAll();
            _allocated.Dispose();
        }
    }
}
