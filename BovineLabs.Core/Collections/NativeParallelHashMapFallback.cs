// <copyright file="NativeParallelHashMapFallback.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Threading;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public unsafe struct NativeParallelHashMapFallback<TKey, TValue> : IDisposable
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public NativeParallelHashMap<TKey, TValue> HashMap;
        public NativeQueue<FallbackData> Fallback;

        public NativeParallelHashMapFallback(int capacity, Allocator allocator)
        {
            this.HashMap = new NativeParallelHashMap<TKey, TValue>(capacity, allocator);
            this.Fallback = new NativeQueue<FallbackData>(allocator);
        }

        public ParallelWriter AsWriter()
        {
            return new ParallelWriter(this.HashMap.AsParallelWriter(), this.Fallback.AsParallelWriter());
        }

        public void Dispose()
        {
            this.HashMap.Dispose();
            this.Fallback.Dispose();
        }

        public void Clear()
        {
            this.HashMap.Clear();
        }

        public JobHandle Apply(JobHandle jobHandle, out NativeParallelHashMap<TKey, TValue>.ReadOnly reader, ApplyJob job = default)
        {
            job.HashMap = this.HashMap;
            job.Fallback = this.Fallback;
            jobHandle = job.Schedule(jobHandle);
            reader = this.HashMap.AsReadOnly();
            return jobHandle;
        }

        public JobHandle Dispose(JobHandle jobHandle)
        {
            return this.Fallback.Dispose(jobHandle);
        }

        public JobHandle Clear(JobHandle dependency, ClearNativeParallelHashMapJob<TKey, TValue> job = default)
        {
            job.HashMap = this.HashMap;
            return job.Schedule(dependency);
        }

        public readonly struct ParallelWriter
        {
            private readonly NativeParallelHashMap<TKey, TValue>.ParallelWriter hashMap;
            private readonly NativeQueue<FallbackData>.ParallelWriter fallback;

            internal ParallelWriter(NativeParallelHashMap<TKey, TValue>.ParallelWriter hashMap, NativeQueue<FallbackData>.ParallelWriter fallback)
            {
                this.hashMap = hashMap;
                this.fallback = fallback;
            }

            public bool TryAdd(TKey key, TValue item)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.hashMap.m_Safety);
#endif
                var data = this.hashMap.m_Writer.m_Buffer;

                if (ContainsKey(data, key))
                {
                    return false;
                }

                if (!data->TryReserveParallel(1, out var idx))
                {
                    this.fallback.Enqueue(new FallbackData(key, item));
                    return true;
                }

                UnsafeUtility.WriteArrayElement(data->keys, idx, key);
                UnsafeUtility.WriteArrayElement(data->values, idx, item);

                var bucket = key.GetHashCode() & data->bucketCapacityMask;
                var buckets = (int*)data->buckets;

                if (Interlocked.CompareExchange(ref buckets[bucket], idx, -1) == -1)
                {
                    return true;
                }

                var nextPtrs = (int*)data->next;
                int next;

                do
                {
                    next = buckets[bucket];
                    nextPtrs[idx] = next;

                    if (ContainsKey(data, key))
                    {
                        return false;
                    }
                }
                while (Interlocked.CompareExchange(ref buckets[bucket], idx, next) != next);

                return true;
            }

            public void Add(TKey key, TValue item)
            {
                this.TryAdd(key, item);
            }

            private static bool ContainsKey(UnsafeParallelHashMapData* data, TKey key)
            {
                if (data->allocatedIndexLength <= 0)
                {
                    return false;
                }

                var buckets = (int*)data->buckets;
                var entryIdx = buckets[key.GetHashCode() & data->bucketCapacityMask];
                var nextPtrs = (int*)data->next;

                while (entryIdx >= 0 && entryIdx < data->keyCapacity)
                {
                    if (UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(key))
                    {
                        return true;
                    }

                    entryIdx = nextPtrs[entryIdx];
                }

                return false;
            }
        }

        [BurstCompile]
        public struct ApplyJob : IJob
        {
            internal NativeParallelHashMap<TKey, TValue> HashMap;
            internal NativeQueue<FallbackData> Fallback;

            public void Execute()
            {
                while (this.Fallback.TryDequeue(out var item))
                {
                    this.HashMap.TryAdd(item.Key, item.Value);
                }
            }
        }

        public readonly struct FallbackData
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
