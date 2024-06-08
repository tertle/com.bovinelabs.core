// <copyright file="NativeParallelMultiHashMapFallback.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Threading;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    public static class NativeParallelMultiHashMapFallbackExtensions
    {
        public static NativeParallelMultiHashMapFallback<TKey, TValue> WithFallback<TKey, TValue>(
            this NativeParallelMultiHashMap<TKey, TValue> hashMap, Allocator allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return new NativeParallelMultiHashMapFallback<TKey, TValue>(hashMap, allocator);
        }
    }

    public unsafe struct NativeParallelMultiHashMapFallback<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private NativeParallelMultiHashMap<TKey, TValue> hashMap;
        private NativeQueue<FallbackData> fallback;

        public NativeParallelMultiHashMapFallback(NativeParallelMultiHashMap<TKey, TValue> hashMap, Allocator allocator)
        {
            this.hashMap = hashMap;
            this.fallback = new NativeQueue<FallbackData>(allocator);
        }

        public Writer AsWriter()
        {
            return new Writer(this.hashMap.AsParallelWriter(), this.fallback.AsParallelWriter());
        }

        public JobHandle ApplyAndDispose(JobHandle jobHandle, Job job = default)
        {
            job.HashMap = this.hashMap;
            job.Fallback = this.fallback;
            jobHandle = job.Schedule(jobHandle);
            this.fallback.Dispose(jobHandle);
            return jobHandle;
        }

        [BurstCompile]
        public struct Job : IJob
        {
            internal NativeParallelMultiHashMap<TKey, TValue> HashMap;
            internal NativeQueue<FallbackData> Fallback;

            public void Execute()
            {
                while (this.Fallback.TryDequeue(out var item))
                {
                    this.HashMap.Add(item.Key, item.Value);
                }
            }
        }

        public struct Writer
        {
            private const int SentinelRefilling = -2;
            private const int SentinelSwapInProgress = -3;

            private NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap;
            private NativeQueue<FallbackData>.ParallelWriter fallback;

            internal Writer(NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, NativeQueue<FallbackData>.ParallelWriter fallback)
            {
                this.hashMap = hashMap;
                this.fallback = fallback;
            }

            /// <summary>
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>
            /// If a key-value pair with this key is already present, an additional separate key-value pair is added.
            /// </remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            public void Add(TKey key, TValue item)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.hashMap.m_Safety);
#endif
                var result = AddAtomicMulti(this.hashMap.m_Writer.m_Buffer, key, item, this.hashMap.m_Writer.m_ThreadIndex);
                if (Hint.Unlikely(!result))
                {
                    this.fallback.Enqueue(new FallbackData(key, item));
                }
            }

            internal static bool AddAtomicMulti(UnsafeParallelHashMapData* data, TKey key, TValue item, int threadIndex)
            {
                // Allocate an entry from the free list
                int idx = AllocEntry(data, threadIndex);

                if (idx == -1)
                {
                    return false;
                }

                // Write the new value to the entry
                UnsafeUtility.WriteArrayElement(data->keys, idx, key);
                UnsafeUtility.WriteArrayElement(data->values, idx, item);

                int bucket = key.GetHashCode() & data->bucketCapacityMask;
                // Add the index to the hash-map
                int* buckets = (int*)data->buckets;

                int nextPtr;
                int* nextPtrs = (int*)data->next;
                do
                {
                    nextPtr = buckets[bucket];
                    nextPtrs[idx] = nextPtr;
                }
                while (Interlocked.CompareExchange(ref buckets[bucket], idx, nextPtr) != nextPtr);

                return true;
            }

            internal static int AllocEntry(UnsafeParallelHashMapData* data, int threadIndex)
            {
                int idx;
                int* nextPtrs = (int*)data->next;

                do
                {
                    do
                    {
                        idx = Volatile.Read(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine]);
                    }
                    while (idx == SentinelSwapInProgress);

                    // Check if this thread has a free entry. Negative value means there is nothing free.
                    if (idx < 0)
                    {
                        // Try to refill local cache. The local cache is a linked list of 16 free entries.

                        // Indicate to other threads that we are refilling the cache.
                        // -2 means refilling cache.
                        // -1 means nothing free on this thread.
                        Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], SentinelRefilling);

                        // If it failed try to get one from the never-allocated array
                        if (data->allocatedIndexLength < data->keyCapacity)
                        {
                            idx = Interlocked.Add(ref data->allocatedIndexLength, 16) - 16;

                            if (idx < data->keyCapacity - 1)
                            {
                                int count = math.min(16, data->keyCapacity - idx);

                                // Set up a linked list of free entries.
                                for (int i = 1; i < count; ++i)
                                {
                                    nextPtrs[idx + i] = idx + i + 1;
                                }

                                // Last entry points to null.
                                nextPtrs[idx + count - 1] = -1;

                                // The first entry is going to be allocated to someone so it also points to null.
                                nextPtrs[idx] = -1;

                                // Set the TLS first free to the head of the list, which is the one after the entry we are returning.
                                Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], idx + 1);

                                return idx;
                            }

                            if (idx == data->keyCapacity - 1)
                            {
                                // We tried to allocate more entries for this thread but we've already hit the key capacity,
                                // so we are in fact out of space. Record that this thread has no more entries.
                                Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], -1);

                                return idx;
                            }
                        }

                        // If we reach here, then we couldn't allocate more entries for this thread, so it's completely empty.
                        Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], -1);

                        int maxThreadCount = JobsUtility.ThreadIndexCount;

                        // Failed to get any, try to get one from another free list
                        bool again = true;
                        while (again)
                        {
                            again = false;
                            for (int other = (threadIndex + 1) % maxThreadCount; other != threadIndex; other = (other + 1) % maxThreadCount)
                            {
                                // Attempt to grab a free entry from another thread and switch the other thread's free head
                                // atomically.
                                do
                                {
                                    do
                                    {
                                        idx = Volatile.Read(ref data->firstFreeTLS[other * UnsafeParallelHashMapData.IntsPerCacheLine]);
                                    }
                                    while (idx == SentinelSwapInProgress);

                                    if (idx < 0)
                                    {
                                        break;
                                    }
                                }
                                while (Interlocked.CompareExchange(
                                           ref data->firstFreeTLS[other * UnsafeParallelHashMapData.IntsPerCacheLine], SentinelSwapInProgress, idx) != idx);

                                if (idx == -2)
                                {
                                    // If the thread was refilling the cache, then try again.
                                    again = true;
                                }
                                else if (idx >= 0)
                                {
                                    // We succeeded in getting an entry from another thread so remove this entry from the
                                    // linked list.
                                    Interlocked.Exchange(ref data->firstFreeTLS[other * UnsafeParallelHashMapData.IntsPerCacheLine], nextPtrs[idx]);
                                    nextPtrs[idx] = -1;
                                    return idx;
                                }
                            }
                        }

                        return -1;
                    }

                    if (idx >= data->keyCapacity)
                    {
                        return -1;
                    }
                }
                while (Interlocked.CompareExchange(
                           ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], SentinelSwapInProgress, idx) != idx);

                Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], nextPtrs[idx]);
                nextPtrs[idx] = -1;
                return idx;
            }
        }

        internal readonly struct FallbackData
        {
            public readonly TKey Key;
            public readonly TValue Value;

            public FallbackData(TKey key, TValue value)
            {
                this.Key = key;
                this.Value = value;
            }
        }
    }
}
