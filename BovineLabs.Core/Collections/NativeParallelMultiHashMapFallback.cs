namespace BovineLabs.Core.Collections
{
    using System;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public unsafe struct NativeParallelMultiHashMapFallback<TKey, TValue> : IDisposable
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public NativeParallelMultiHashMap<TKey, TValue> HashMap;
        internal NativeQueue<FallbackData> Fallback;

        public NativeParallelMultiHashMapFallback(int capacity, Allocator allocator)
        {
            HashMap = new NativeParallelMultiHashMap<TKey, TValue>(capacity, allocator);
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

        public JobHandle Apply(JobHandle jobHandle, out NativeParallelMultiHashMap<TKey, TValue>.ReadOnly reader, ApplyJob job = default)
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

        public JobHandle Clear(JobHandle dependency, ClearNativeParallelMultiHashMapJob<TKey, TValue> job = default, ClearFallbackJob fallbackJob = default)
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
            private readonly NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter _hashMap;
            private readonly NativeQueue<FallbackData>.ParallelWriter _fallback;

            internal ParallelWriter(NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, NativeQueue<FallbackData>.ParallelWriter fallback)
            {
                _hashMap = hashMap;
                _fallback = fallback;
            }

            public void Add(TKey key, TValue item)
            {
                var hashMap = _hashMap;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(hashMap.GetSafety());
#endif
                if (Hint.Likely(_hashMap.TryReserve(1, out var idx)))
                {
                    var data = hashMap.GetWriter().GetBuffer();
                    UnsafeUtility.WriteArrayElement(data->keys, idx, key);
                    UnsafeUtility.WriteArrayElement(data->values, idx, item);
                    UnsafeUtility.WriteArrayElement(data->next, idx, key.GetHashCode());
                }
                else
                {
                    _fallback.Enqueue(new FallbackData(key, item, key.GetHashCode()));
                }
            }

            public void AddBatch(NativeArray<TKey> keys, NativeArray<TValue> values)
            {
                var hashMap = _hashMap;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(hashMap.GetSafety());
                Check.Assume(keys.Length == values.Length);
#endif
                AddBatch((TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
            }

            public void AddBatch(TKey* keys, TValue* values, int length)
            {
                var hashMap = _hashMap;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(hashMap.GetSafety());
#endif
                if (Hint.Likely(_hashMap.TryReserve(length, out var idx)))
                {
                    var data = hashMap.GetWriter().GetBuffer();
                    var keyPtr = (TKey*)data->keys + idx;
                    var valuePtr = (TValue*)data->values + idx;
                    var nextPtr = (int*)data->next + idx;

                    UnsafeUtility.MemCpy(keyPtr, keys, length * UnsafeUtility.SizeOf<TKey>());
                    UnsafeUtility.MemCpy(valuePtr, values, length * UnsafeUtility.SizeOf<TValue>());

                    for (var i = 0; i < length; i++)
                    {
                        nextPtr[i] = keys[i].GetHashCode();
                    }
                }
                else
                {
                    for (var i = 0; i < length; i++)
                    {
                        _fallback.Enqueue(new FallbackData(keys[i], values[i], keys[i].GetHashCode()));
                    }
                }
            }
        }

        [BurstCompile]
        public struct ApplyJob : IJob
        {
            internal NativeParallelMultiHashMap<TKey, TValue> HashMap;
            internal NativeQueue<FallbackData> Fallback;

            public void Execute()
            {
                var requiredCapacity = HashMap.Count() + Fallback.Count;
                if (requiredCapacity > HashMap.Capacity)
                {
                    HashMap.Capacity = requiredCapacity;
                }

                HashMap.RecalculateBucketsCached();

                while (Fallback.TryDequeue(out var item))
                {
                    HashMap.Add(item.Key, item.Value, item.Hash);
                }
            }
        }

        internal readonly struct FallbackData
        {
            internal readonly TKey Key;
            internal readonly TValue Value;
            internal readonly int Hash;

            internal FallbackData(TKey key, TValue value, int hash)
            {
                Key = key;
                Value = value;
                Hash = hash;
            }
        }
    }
}
