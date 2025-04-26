// <copyright file="NativeParallelMultiHashMapFallback.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Extensions;
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
        public NativeQueue<FallbackData> Fallback;

        public NativeParallelMultiHashMapFallback(int capacity, Allocator allocator)
        {
            this.HashMap = new NativeParallelMultiHashMap<TKey, TValue>(capacity, allocator);
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

        public JobHandle Apply(JobHandle jobHandle, out NativeParallelMultiHashMap<TKey, TValue>.ReadOnly reader, ApplyJob job = default)
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

        public JobHandle Clear(JobHandle dependency, ClearNativeParallelMultiHashMapJob<TKey, TValue> job = default)
        {
            job.HashMap = this.HashMap;
            return job.Schedule(dependency);
        }

        public readonly struct ParallelWriter
        {
            private readonly NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap;
            private readonly NativeQueue<FallbackData>.ParallelWriter fallback;

            internal ParallelWriter(NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter hashMap, NativeQueue<FallbackData>.ParallelWriter fallback)
            {
                this.hashMap = hashMap;
                this.fallback = fallback;
            }

            /// <summary> Adds a new key-value pair. </summary>
            /// <remarks> If a key-value pair with this key is already present, an additional separate key-value pair is added. </remarks>
            /// <param name="key"> The key to add. </param>
            /// <param name="item"> The value to add. </param>
            public void Add(TKey key, TValue item)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.hashMap.m_Safety);
#endif
                if (Hint.Likely(this.hashMap.TryReserve(1, out var idx)))
                {
                    var data = this.hashMap.m_Writer.m_Buffer;
                    UnsafeUtility.WriteArrayElement(data->keys, idx, key);
                    UnsafeUtility.WriteArrayElement(data->values, idx, item);
                    UnsafeUtility.WriteArrayElement(data->next, idx, item.GetHashCode());
                }
                else
                {
                    this.fallback.Enqueue(new FallbackData(key, item, key.GetHashCode()));
                }
            }

            public void AddBatch(NativeArray<TKey> keys, NativeArray<TValue> values)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.hashMap.m_Safety);
                Check.Assume(keys.Length == values.Length);
#endif
                this.AddBatch((TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
            }

            public void AddBatch(TKey* keys, TValue* values, int length)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.hashMap.m_Safety);
#endif
                if (Hint.Likely(this.hashMap.TryReserve(length, out var idx)))
                {
                    var data = this.hashMap.m_Writer.m_Buffer;
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
                        this.fallback.Enqueue(new FallbackData(keys[i], values[i], keys[i].GetHashCode()));
                    }
                }
            }

            /// <summary> Adds a new key-value pair. </summary>
            /// <remarks> If a key-value pair with this key is already present, an additional separate key-value pair is added. </remarks>
            /// <param name="key"> The key to add. </param>
            /// <param name="item"> The value to add. </param>
            /// <param name="hash"> A custom hash value. </param>
            public void Add(TKey key, TValue item, int hash)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.hashMap.m_Safety);
#endif
                if (Hint.Likely(this.hashMap.TryReserve(1, out var idx)))
                {
                    var data = this.hashMap.m_Writer.m_Buffer;
                    UnsafeUtility.WriteArrayElement(data->keys, idx, key);
                    UnsafeUtility.WriteArrayElement(data->values, idx, item);
                    UnsafeUtility.WriteArrayElement(data->next, idx, hash);
                }
                else
                {
                    this.fallback.Enqueue(new FallbackData(key, item, hash));
                }
            }

            public void AddBatch(NativeArray<TKey> keys, NativeArray<TValue> values, NativeArray<int> hashes)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.hashMap.m_Safety);
                Check.Assume(keys.Length == values.Length);
                Check.Assume(keys.Length == hashes.Length);
#endif
                this.AddBatch((TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), (int*)hashes.GetUnsafeReadOnlyPtr(), keys.Length);
            }

            public void AddBatch(TKey* keys, TValue* values, int* hashes, int length)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.hashMap.m_Safety);
#endif
                if (Hint.Likely(this.hashMap.TryReserve(length, out var idx)))
                {
                    var data = this.hashMap.m_Writer.m_Buffer;
                    var keyPtr = (TKey*)data->keys + idx;
                    var valuePtr = (TValue*)data->values + idx;
                    var nextPtr = (int*)data->next + idx;

                    UnsafeUtility.MemCpy(keyPtr, keys, length * UnsafeUtility.SizeOf<TKey>());
                    UnsafeUtility.MemCpy(valuePtr, values, length * UnsafeUtility.SizeOf<TValue>());
                    UnsafeUtility.MemCpy(nextPtr, hashes, length * UnsafeUtility.SizeOf<int>());
                }
                else
                {
                    for (var i = 0; i < length; i++)
                    {
                        this.fallback.Enqueue(new FallbackData(keys[i], values[i], hashes[i]));
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
                this.HashMap.RecalculateBucketsCached();

                while (this.Fallback.TryDequeue(out var item))
                {
                    this.HashMap.Add(item.Key, item.Value, item.Hash);
                }
            }
        }

        public readonly struct FallbackData
        {
            public readonly TKey Key;
            public readonly TValue Value;
            public readonly int Hash;

            public FallbackData(TKey key, TValue value, int hash)
            {
                this.Key = key;
                this.Value = value;
                this.Hash = hash;
            }
        }
    }
}
