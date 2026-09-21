namespace BovineLabs.Core.Collections
{
    using System;
    using System.Threading;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
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
            HashMap = new NativeParallelHashMap<TKey, TValue>(capacity, allocator);
            Fallback = new NativeQueue<FallbackData>(allocator);
        }

        public ParallelWriter AsWriter()
        {
            return new ParallelWriter(HashMap.AsParallelWriter(), Fallback.AsParallelWriter());
        }

        public void Dispose()
        {
            HashMap.Dispose();
            Fallback.Dispose();
        }

        public void Clear()
        {
            HashMap.Clear();
            Fallback.Clear();
        }

        public JobHandle Apply(JobHandle jobHandle, out NativeParallelHashMap<TKey, TValue>.ReadOnly reader, ApplyJob job = default)
        {
            job.HashMap = HashMap;
            job.Fallback = Fallback;
            jobHandle = job.Schedule(jobHandle);
            reader = HashMap.AsReadOnly();
            return jobHandle;
        }

        public JobHandle Dispose(JobHandle jobHandle)
        {
            var hashMapDispose = HashMap.Dispose(jobHandle);
            var fallbackDispose = Fallback.Dispose(jobHandle);
            return JobHandle.CombineDependencies(hashMapDispose, fallbackDispose);
        }

        public JobHandle Clear(JobHandle dependency, ClearNativeParallelHashMapJob<TKey, TValue> job = default, ClearFallbackJob fallbackJob = default)
        {
            job.HashMap = HashMap;
            dependency = job.Schedule(dependency);
            fallbackJob.Fallback = Fallback;
            return fallbackJob.Schedule(dependency);
        }

        [BurstCompile]
        public struct ClearFallbackJob : IJob
        {
            internal NativeQueue<FallbackData> Fallback;

            public void Execute()
            {
                Fallback.Clear();
            }
        }

        public readonly struct ParallelWriter
        {
            private readonly NativeParallelHashMap<TKey, TValue>.ParallelWriter _hashMap;
            private readonly NativeQueue<FallbackData>.ParallelWriter _fallback;

            internal ParallelWriter(NativeParallelHashMap<TKey, TValue>.ParallelWriter hashMap, NativeQueue<FallbackData>.ParallelWriter fallback)
            {
                _hashMap = hashMap;
                _fallback = fallback;
            }

            public bool TryAdd(TKey key, TValue item)
            {
                var hashMap = _hashMap;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(hashMap.GetSafety());
#endif
                var data = hashMap.GetWriter().GetBuffer();

                if (ContainsKey(data, key))
                {
                    return false;
                }

                if (!data->TryReserveParallel(1, out var idx))
                {
                    _fallback.Enqueue(new FallbackData(key, item));
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
                var hashMap = _hashMap;
                TryAdd(key, item);
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
                while (Fallback.TryDequeue(out var item))
                {
                    HashMap.TryAdd(item.Key, item.Value);
                }
            }
        }

        public readonly struct FallbackData
        {
            public readonly TKey Key;
            public readonly TValue Value;

            public FallbackData(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}
