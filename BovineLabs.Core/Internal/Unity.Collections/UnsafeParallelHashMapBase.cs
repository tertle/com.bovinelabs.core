namespace BovineLabs.Core.Internal
{
    using System;
    using Unity.Collections;

    internal static unsafe class UnsafeParallelHashMapBase<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal static int AllocEntry(UnsafeParallelHashMapData* data, int threadIndex)
        {
            return Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapBase<TKey,
            TValue>.AllocEntry((Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData*)data, threadIndex);
        }

        internal static void FreeEntry(UnsafeParallelHashMapData* data, int index, int threadIndex)
        {
            Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapBase<TKey,
            TValue>.FreeEntry((Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData*)data, index, threadIndex);
        }

        internal static bool TryGetFirstValueAtomic(
            UnsafeParallelHashMapData* data, TKey key, out TValue item, out NativeParallelMultiHashMapIterator<TKey> iterator)
        {
            return Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapBase<TKey,
            TValue>.TryGetFirstValueAtomic((Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData*)data, key, out item, out iterator);
        }
    }
}
