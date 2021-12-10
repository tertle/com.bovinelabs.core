namespace BovineLabs.Core.Memory
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class Mem
    {
        public static T* Malloc<T>(Allocator allocator)
            where T : unmanaged
        {
            return Memory.Unmanaged.Allocate<T>(allocator);
        }

        public static T* MallocClear<T>(Allocator allocator)
            where T : unmanaged
        {
            var ptr = Malloc<T>(allocator);
            UnsafeUtility.MemClear(ptr, UnsafeUtility.SizeOf<T>());
            return ptr;
        }

        public static void Free(void* ptr, Allocator allocator)
        {
            Memory.Unmanaged.Free(ptr, allocator);
        }

        public static void Free<T>(T* ptr, Allocator allocator)
            where T : unmanaged
        {
            Memory.Unmanaged.Free(ptr, allocator);
        }
    }
}
