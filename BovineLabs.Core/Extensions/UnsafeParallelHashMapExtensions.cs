namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    public static unsafe class UnsafeParallelHashMapExtensions
    {
        public static UnsafeParallelHashMap<TKey, TValue>.ParallelWriter AsParallelWriter<TKey, TValue>(
            this UnsafeParallelHashMap<TKey, TValue> hashMap, int threadIndex)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var writer = hashMap.AsParallelWriter();
            writer.GetThreadIndex() = threadIndex;

            return writer;
        }

        /// <summary>
        /// The returned reference aliases map storage. Consume immediately; any later map write or capacity change invalidates it.
        /// </summary>
        public static ref TValue GetOrAddRefUnsafe<TKey, TValue>(this UnsafeParallelHashMap<TKey, TValue> hashMap, TKey key, TValue defaultValue = default)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var data = hashMap.GetBuffer();

            if (UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(data, key, out _, out var tempIt))
            {
                return ref UnsafeUtility.ArrayElementAsRef<TValue>(data->values, tempIt.GetEntryIndex());
            }

            // Allocate an entry from the free list
            int idx;
            int* nextPtrs;

            if (data->allocatedIndexLength >= data->keyCapacity && data->firstFreeTLS[0] < 0)
            {
                var maxThreadCount = JobsUtility.ThreadIndexCount;

                for (var tls = 1; tls < maxThreadCount; ++tls)
                {
                    if (data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine] >= 0)
                    {
                        idx = data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine];
                        nextPtrs = (int*)data->next;
                        data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine] = nextPtrs[idx];
                        nextPtrs[idx] = -1;
                        data->firstFreeTLS[0] = idx;
                        break;
                    }
                }

                if (data->firstFreeTLS[0] < 0)
                {
                    var newCap = UnsafeParallelHashMapData.GrowCapacity(data->keyCapacity);
                    UnsafeParallelHashMapData.ReallocateHashMap<TKey, TValue>(data, newCap, UnsafeParallelHashMapData.GetBucketSize(newCap),
                        hashMap.GetAllocator());
                }
            }

            idx = data->firstFreeTLS[0];

            if (idx >= 0)
            {
                data->firstFreeTLS[0] = ((int*)data->next)[idx];
            }
            else
            {
                idx = data->allocatedIndexLength++;
            }

            CheckIndexOutOfBounds(data, idx);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, defaultValue);

            var bucket = key.GetHashCode() & data->bucketCapacityMask;

            // Add the index to the hash-map
            var buckets = (int*)data->buckets;
            nextPtrs = (int*)data->next;
            nextPtrs[idx] = buckets[bucket];
            buckets[bucket] = idx;

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(data->values, idx);
        }

        public static ref TValue GetOrAddRefUnsafe<TKey, TValue>(
            this UnsafeParallelHashMap<TKey, TValue>.ParallelWriter hashMap, TKey key, TValue defaultValue = default)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var data = hashMap.GetBuffer();

            if (UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(data, key, out _, out var tempIt))
            {
                return ref UnsafeUtility.ArrayElementAsRef<TValue>(data->values, tempIt.GetEntryIndex());
            }

            // Allocate an entry from the free list
            var idx = UnsafeParallelHashMapBase<TKey, TValue>.AllocEntry(data, hashMap.ThreadIndex);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, defaultValue);

            var bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            var buckets = (int*)data->buckets;

            // Make the bucket's head idx. If the exchange returns something other than -1, then the bucket had
            // a non-null head which means we need to do more checks...
            if (Interlocked.CompareExchange(ref buckets[bucket], idx, -1) != -1)
            {
                var nextPtrs = (int*)data->next;
                int next;

                do
                {
                    // Link up this entry with the rest of the bucket under the assumption that this key
                    // doesn't already exist in the bucket. This assumption could be wrong, which will be
                    // checked later.
                    next = buckets[bucket];
                    nextPtrs[idx] = next;

                    // If the key already exists then we should free the entry we took earlier.
                    if (UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(data, key, out _, out tempIt))
                    {
                        // Put back the entry in the free list if someone else added it while trying to add
                        UnsafeParallelHashMapBase<TKey, TValue>.FreeEntry(data, idx, hashMap.ThreadIndex);
                        return ref UnsafeUtility.ArrayElementAsRef<TValue>(data->values, tempIt.GetEntryIndex());
                    }
                }
                while (Interlocked.CompareExchange(ref buckets[bucket], idx, next) != next);
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(data->values, idx);
        }

        public static ref TValue GetRef<TKey, TValue>(this UnsafeParallelHashMap<TKey, TValue>.ParallelWriter hashMap, TKey key)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var data = hashMap.GetBuffer();

            if (UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(data, key, out _, out var tempIt))
            {
                return ref UnsafeUtility.ArrayElementAsRef<TValue>(data->values, tempIt.GetEntryIndex());
            }

            throw new NullReferenceException();
        }

        public static void ClearAndAddBatchUnsafe<TKey, TValue>(
            [NoAlias] ref this UnsafeParallelHashMap<TKey, TValue> hashMap, [NoAlias] TKey* keys, [NoAlias] TValue* values, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.Clear();

            if (hashMap.Capacity < length)
            {
                hashMap.Capacity = length;
            }

            UnsafeUtility.MemCpy(hashMap.GetBuffer()->keys, keys, length * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(hashMap.GetBuffer()->values, values, length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)hashMap.GetBuffer()->buckets;
            var nextPtrs = (int*)hashMap.GetBuffer()->next;

            var bucketCapacityMask = hashMap.GetBuffer()->bucketCapacityMask;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }

            hashMap.GetBuffer()->allocatedIndexLength = length;
        }

        /// <summary>
        /// Keys must be unique; duplicates are not checked.
        /// </summary>
        public static void ClearAndAddBatchUnsafe<TKey, TValue>(
            [NoAlias] ref this UnsafeParallelHashMap<TKey, TValue> hashMap, [NoAlias] NativeSlice<TKey> keys, [NoAlias] NativeArray<TValue> values)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckLengthsMatch(keys.Length, values.Length);

            var length = keys.Length;

            hashMap.Clear();

            if (hashMap.Capacity < length)
            {
                hashMap.Capacity = length;
            }

            UnsafeUtility.MemCpyStride(hashMap.GetBuffer()->keys, UnsafeUtility.SizeOf<TKey>(), keys.GetUnsafeReadOnlyPtr(), keys.Stride,
                UnsafeUtility.SizeOf<TKey>(), length);

            UnsafeUtility.MemCpy(hashMap.GetBuffer()->values, values.GetUnsafeReadOnlyPtr(), length * UnsafeUtility.SizeOf<TValue>());

            var buckets = (int*)hashMap.GetBuffer()->buckets;
            var nextPtrs = (int*)hashMap.GetBuffer()->next;
            var bucketCapacityMask = hashMap.GetBuffer()->bucketCapacityMask;

            for (var idx = 0; idx < length; idx++)
            {
                var bucket = keys[idx].GetHashCode() & bucketCapacityMask;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;
            }

            hashMap.GetBuffer()->allocatedIndexLength = length;
        }

        public static bool TryGetFirstKeyValue<TKey, TValue>(ref this UnsafeParallelHashMap<TKey, TValue> map, out TKey key, out TValue value, ref int index)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return map.GetBuffer()->TryGetFirstKeyValue<TKey, TValue>(out key, out value, ref index);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckLengthsMatch(int keys, int values)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (keys != values)
            {
                throw new ArgumentException("Key and value array don't match");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckIndexOutOfBounds(UnsafeParallelHashMapData* data, int idx)
        {
            if (idx < 0 || idx >= data->keyCapacity)
            {
                throw new InvalidOperationException("Internal HashMap error");
            }
        }
    }
}
