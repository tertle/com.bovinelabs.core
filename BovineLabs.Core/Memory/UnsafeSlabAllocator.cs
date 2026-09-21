namespace BovineLabs.Core.Memory
{
    using System;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    public unsafe struct UnsafeSlabAllocator<T> : IDisposable
        where T : unmanaged
    {
        private readonly int _countPerSlab;
        private readonly AllocatorManager.AllocatorHandle _allocator;

        private UnsafeList<Ptr>* _slabs;

        [NativeDisableUnsafePtrRestriction]
        private int* _count;

        public UnsafeSlabAllocator(int countPerSlab, AllocatorManager.AllocatorHandle allocator)
        {
            Debug.Assert(countPerSlab > 0);

            _slabs = UnsafeList<Ptr>.Create(0, allocator);
            _allocator = allocator;
            _countPerSlab = countPerSlab;

            _count = (int*)CollectionMemory.Allocate(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), allocator);
            *_count = countPerSlab;
        }

        public int AllocationCount => (_countPerSlab * (_slabs->Length - 1)) + *_count;

        public bool IsCreated => _count != null;

        /// <summary>
        /// Returned memory is not cleared.
        /// </summary>
        public T* Alloc()
        {
            if (*_count == _countPerSlab)
            {
                *_count = 0;
                var ptr = (Ptr)CollectionMemory.Allocate(_countPerSlab * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), _allocator);
                _slabs->Add(ptr);
            }

            var lastSlab = (T*)(*_slabs)[^1];
            return lastSlab + (*_count)++;
        }

        public void Clear()
        {
            for (var i = 0; i < _slabs->Length; i++)
            {
                CollectionMemory.Free((*_slabs)[i], _allocator);
            }

            _slabs->Clear();
            *_count = _countPerSlab;
        }

        public void Dispose()
        {
            Clear();
            UnsafeList<Ptr>.Destroy(_slabs);

            CollectionMemory.Free(_count, _allocator);

            _count = default;
            _slabs = default;
        }

        public int Allocated()
        {
            return _slabs->Length * _countPerSlab * UnsafeUtility.SizeOf<T>();
        }
    }
}
