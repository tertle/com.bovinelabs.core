namespace BovineLabs.Core.Memory
{
    using System;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    public unsafe struct UnsafeParallelPoolAllocator<T> : IDisposable
        where T : unmanaged
    {
        private readonly Allocator _allocator;

        [NativeDisableUnsafePtrRestriction]
        private UnsafePoolAllocator<T>* _pools;

        [NativeSetThreadIndex]
        private int _threadIndex;

        public UnsafeParallelPoolAllocator(int countPerChunk, Allocator allocator)
        {
            _allocator = allocator;
            _pools = (UnsafePoolAllocator<T>*)CollectionMemory.Allocate(UnsafeUtility.SizeOf<UnsafePoolAllocator<T>>() * JobsUtility.ThreadIndexCount,
                UnsafeUtility.AlignOf<UnsafePoolAllocator<T>>(), allocator);

            for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                _pools[i] = new UnsafePoolAllocator<T>(countPerChunk, allocator);
            }

            _threadIndex = 0;
        }

        public bool IsCreated => _pools != null;

        public void Dispose()
        {
            for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                _pools[i].Dispose();
            }

            CollectionMemory.Free(_pools, _allocator);
            _pools = null;
        }

        public T* Alloc()
        {
            return _pools[_threadIndex].Alloc();
        }

        public void Free(T* p)
        {
            _pools[_threadIndex].Free(p);
        }

        public int Allocated()
        {
            var allocated = 0;

            for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                allocated += _pools[i].Allocated();
            }

            return allocated;
        }
    }
}
