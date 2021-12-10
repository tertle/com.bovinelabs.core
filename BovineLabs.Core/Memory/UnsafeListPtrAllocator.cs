namespace BovineLabs.Core.Memory
{
    using System;
    using BovineLabs.Core.Collections;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct UnsafeListPtrAllocator<T>
        where T : unmanaged
    {
        private NativeList<IntPtr> ptrs;

        private readonly Allocator allocator;

        public UnsafeListPtrAllocator(Allocator allocator)
        {
            this.allocator = allocator;
            this.ptrs = new NativeList<IntPtr>(this.allocator);
        }

        public UnsafeListPtr<T> Alloc()
        {
            var p = new UnsafeListPtr<T>(Allocator.Persistent);
            this.ptrs.Add((IntPtr)p.GetUnsafeList());
            return p;
        }

        public void Dispose()
        {
            foreach (var p in this.ptrs.AsArray())
            {
                UnsafeList.Destroy((UnsafeList*)p);
            }

            this.ptrs.Dispose();
        }

        public int Count => this.ptrs.Length;
    }
}
