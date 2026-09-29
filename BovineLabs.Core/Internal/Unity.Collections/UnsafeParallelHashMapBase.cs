namespace BovineLabs.Core.Internal
{
    using System;
    using System.Threading;
    using Unity.Collections;

    internal static unsafe class UnsafeParallelHashMapBase<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private const int SentinelRefilling = -2;
        private const int SentinelSwapInProgress = -3;
        private const int AllocationBatchSize = 16;

        internal static int AllocEntry(UnsafeParallelHashMapData* data, int threadIndex)
        {
            return Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapBase<TKey,
            TValue>.AllocEntry((Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData*)data, threadIndex);
        }

        internal static bool TryAllocEntry(UnsafeParallelHashMapData* data, int threadIndex, out int index)
        {
            var nextPtrs = (int*)data->next;
            ref var firstFree = ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine];

            do
            {
                do
                {
                    index = Volatile.Read(ref firstFree);
                }
                while (index == SentinelSwapInProgress);

                if (index < 0)
                {
                    Interlocked.Exchange(ref firstFree, SentinelRefilling);

                    if (data->allocatedIndexLength < data->keyCapacity)
                    {
                        index = Interlocked.Add(ref data->allocatedIndexLength, AllocationBatchSize) - AllocationBatchSize;
                        if (index < data->keyCapacity)
                        {
                            var count = Math.Min(AllocationBatchSize, data->keyCapacity - index);
                            for (var i = 1; i < count; i++)
                            {
                                nextPtrs[index + i] = index + i + 1;
                            }

                            nextPtrs[index + count - 1] = -1;
                            nextPtrs[index] = -1;
                            Interlocked.Exchange(ref firstFree, count > 1 ? index + 1 : -1);
                            return true;
                        }
                    }

                    // Other workers may retain free entries. Leave them local and let the fallback's Apply job reclaim them.
                    Interlocked.Exchange(ref firstFree, -1);
                    index = -1;
                    return false;
                }
            }
            while (Interlocked.CompareExchange(ref firstFree, SentinelSwapInProgress, index) != index);

            Interlocked.Exchange(ref firstFree, nextPtrs[index]);
            nextPtrs[index] = -1;
            return true;
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
