namespace BovineLabs.Core.Internal
{
    using Unity.Collections;

    internal static unsafe class CollectionMemory
    {
        public static void* Allocate(long size, int alignment, AllocatorManager.AllocatorHandle allocator)
        {
            return Unity.Collections.Memory.Unmanaged.Allocate(size, alignment, allocator);
        }

        public static T* Allocate<T>(AllocatorManager.AllocatorHandle allocator) where T : unmanaged
        {
            return Unity.Collections.Memory.Unmanaged.Allocate<T>(allocator);
        }

        public static void Free(void* pointer, AllocatorManager.AllocatorHandle allocator)
        {
            Unity.Collections.Memory.Unmanaged.Free(pointer, allocator);
        }
    }
}
