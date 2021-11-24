namespace BovineLabs.Core.Memory
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class Mem
    {
        public static T* Malloc<T>(Allocator allocator)
            where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
        }

        public static T* MallocClear<T>(Allocator allocator)
            where T : unmanaged
        {
            var ptr = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
            UnsafeUtility.MemClear(ptr, UnsafeUtility.SizeOf<T>());
            return ptr;
        }

        public static void Free(void* ptr, Allocator allocator)
        {
            UnsafeUtility.Free(ptr, allocator);
        }
    }
}
