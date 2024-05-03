// <copyright file="UnsafePartialKeyedMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
    public unsafe struct UnsafePartialKeyedMap<TValue> : INativeDisposable
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private TValue* values;

        [NativeDisableUnsafePtrRestriction]
        private int* keys;

        [NativeDisableUnsafePtrRestriction]
        private int* next;

        [NativeDisableUnsafePtrRestriction]
        private int* buckets;

        private int count;
        private int nextCapacity;
        private int bucketCapacity;
        private AllocatorManager.AllocatorHandle allocator;

        public UnsafePartialKeyedMap(int* keys, TValue* values, int length, int bucketCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            CheckAllKeysOutOfBounds(keys, length, bucketCapacity);

            this.keys = keys;
            this.values = values;
            this.count = length;
            this.nextCapacity = length;
            this.bucketCapacity = bucketCapacity;
            this.allocator = allocator;

            // Allocated separate because buckets are fixed but next can change
            this.next = (int*)Memory.Unmanaged.Allocate(sizeof(int) * length, JobsUtility.CacheLineSize, allocator);
            this.buckets = (int*)Memory.Unmanaged.Allocate(sizeof(int) * bucketCapacity, JobsUtility.CacheLineSize, allocator);

            this.RecalculateBuckets();
        }

        public bool IsCreated => this.buckets != null;

        internal int* Next => this.next;

        internal int* Buckets => this.buckets;

        internal AllocatorManager.AllocatorHandle Allocator => this.allocator;

        public TValue this[int i]
        {
            get
            {
                CheckIndexOutOfBounds(i, this.count);
                return this.values[i];
            }
        }

        public static UnsafePartialKeyedMap<TValue>* Create(int* keys, TValue* values, int length, int bucketCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            var data = AllocatorManager.Allocate<UnsafePartialKeyedMap<TValue>>(allocator);
            *data = new UnsafePartialKeyedMap<TValue>(keys, values, length, bucketCapacity, allocator);
            return data;
        }

        public static void Destroy(UnsafePartialKeyedMap<TValue>* listData)
        {
            var allocator = listData->Allocator;
            listData->Dispose();
            AllocatorManager.Free(allocator, listData);
        }

        public void Dispose()
        {
            // Don't rewrite allocator
            Memory.Unmanaged.Free(this.buckets, this.allocator);
            Memory.Unmanaged.Free(this.next, this.allocator);
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps"> A job handle. The newly scheduled job will depend upon this handle. </param>
        /// <returns> The handle of a new job that will dispose this hash map. </returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafePartialKeyedMapDisposeJob
                {
                    Next = this.next,
                    Buckets = this.buckets,
                    Allocator = this.allocator,
                }
                .Schedule(inputDeps);

            this.buckets = null;

            return jobHandle;
        }

        public void Update(int* newKeys, TValue* newValues, int newLength)
        {
            CheckAllKeysOutOfBounds(newKeys, newLength, this.bucketCapacity);

            this.keys = newKeys;
            this.values = newValues;
            this.count = newLength;

            // Check if we need more capacity
            if (this.nextCapacity < newLength)
            {
                Memory.Unmanaged.Free(this.next, this.allocator);
                this.nextCapacity = newLength;
                this.next = (int*)Memory.Unmanaged.Allocate(sizeof(int) * this.nextCapacity, JobsUtility.CacheLineSize, this.allocator);
            }

            this.RecalculateBuckets();
        }

        /// <summary>
        /// Gets an iterator for a key.
        /// </summary>
        /// <param name="key"> The key. </param>
        /// <param name="item"> Outputs the associated value represented by the iterator. </param>
        /// <param name="it"> Outputs an iterator. </param>
        /// <returns> True if the key was present. </returns>
        public bool TryGetFirstValue(int key, out TValue item, out UnsafeKeyedMapIterator it)
        {
            CheckKeyOutOfBounds(key, this.bucketCapacity);

            it = default;

            if (this.count <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            it.NextEntryIndex = this.buckets[key];
            return this.TryGetNextValue(out item, ref it);
        }

        /// <summary> Advances an iterator to the next value associated with its key. </summary>
        /// <param name="item"> Outputs the next value. </param>
        /// <param name="it"> A reference to the iterator to advance. </param>
        /// <returns> True if the key was present and had another value. </returns>
        public bool TryGetNextValue(out TValue item, ref UnsafeKeyedMapIterator it)
        {
            it.EntryIndex = it.NextEntryIndex;
            if (it.EntryIndex < 0)
            {
                it.NextEntryIndex = -1;
                item = default;

                return false;
            }

            var nextPtrs = this.next;
            it.NextEntryIndex = nextPtrs[it.EntryIndex];

            // Read the value
            item = UnsafeUtility.ReadArrayElement<TValue>(this.values, it.EntryIndex);

            return true;
        }

        private void RecalculateBuckets()
        {
            UnsafeUtility.MemSet(this.buckets, 0xff, sizeof(int) * this.bucketCapacity);

            for (var idx = 0; idx < this.count; idx++)
            {
                var key = this.keys[idx];
                this.next[idx] = this.buckets[key];
                this.buckets[key] = idx;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllKeysOutOfBounds(int* keys, int length, int bucketCapacity)
        {
            for (var i = 0; i < length; i++)
            {
                CheckKeyOutOfBounds(keys[i], bucketCapacity);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckKeyOutOfBounds(int key, int bucketCapacity)
        {
            if ((key < 0) || (key >= bucketCapacity))
            {
                throw new InvalidOperationException($"{nameof(key)} < 0 || {nameof(key)} >= {nameof(bucketCapacity)}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckIndexOutOfBounds(int index, int count)
        {
            if ((index < 0) || (index >= count))
            {
                throw new InvalidOperationException($"{nameof(index)} < 0 || {nameof(index)} >= {nameof(count)}");
            }
        }
    }

    [BurstCompile]
    internal unsafe struct UnsafePartialKeyedMapDisposeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public int* Next;

        [NativeDisableUnsafePtrRestriction]
        public int* Buckets;

        public AllocatorManager.AllocatorHandle Allocator;

        public void Execute()
        {
            Memory.Unmanaged.Free(this.Buckets, this.Allocator);
            Memory.Unmanaged.Free(this.Next, this.Allocator);
        }
    }
}
