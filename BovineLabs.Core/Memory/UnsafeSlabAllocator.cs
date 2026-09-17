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
        private readonly int countPerSlab;
        private readonly AllocatorManager.AllocatorHandle allocator;

        private UnsafeList<Ptr>* slabs;

        [NativeDisableUnsafePtrRestriction]
        private int* count;

        public UnsafeSlabAllocator(int countPerSlab, AllocatorManager.AllocatorHandle allocator)
        {
            Debug.Assert(countPerSlab > 0);

            this.slabs = UnsafeList<Ptr>.Create(0, allocator);
            this.allocator = allocator;
            this.countPerSlab = countPerSlab;

            this.count = (int*)CollectionMemory.Allocate(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), allocator);
            *this.count = countPerSlab;
        }

        public int AllocationCount => (this.countPerSlab * (this.slabs->Length - 1)) + *this.count;

        public bool IsCreated => this.count != null;

        /// <summary>
        /// Returned memory is not cleared.
        /// </summary>
        public T* Alloc()
        {
            if (*this.count == this.countPerSlab)
            {
                *this.count = 0;
                var ptr = (Ptr)CollectionMemory.Allocate(this.countPerSlab * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), this.allocator);
                this.slabs->Add(ptr);
            }

            var lastSlab = (T*)(*this.slabs)[^1];
            return lastSlab + (*this.count)++;
        }

        public void Clear()
        {
            for (var i = 0; i < this.slabs->Length; i++)
            {
                CollectionMemory.Free((*this.slabs)[i], this.allocator);
            }

            this.slabs->Clear();
            *this.count = this.countPerSlab;
        }

        public void Dispose()
        {
            this.Clear();
            UnsafeList<Ptr>.Destroy(this.slabs);

            CollectionMemory.Free(this.count, this.allocator);

            this.count = default;
            this.slabs = default;
        }

        public int Allocated()
        {
            return this.slabs->Length * this.countPerSlab * UnsafeUtility.SizeOf<T>();
        }
    }
}
