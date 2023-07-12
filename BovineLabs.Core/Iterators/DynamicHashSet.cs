// <copyright file="DynamicHashSet.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct DynamicHashSet<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        private DynamicBuffer<byte> buffer;

        internal DynamicHashSet(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks> Containers capacity remains unchanged. </remarks>
        public void Clear()
        {
            DynamicHashSetData.Clear<TKey>(this.buffer);
        }

        /// <summary> Try adding an element with the specified key into the container. </summary>
        /// <param name="key"> The key of the element to add. </param>
        /// <returns> Returns true if value is added into the container, otherwise returns false. </returns>
        public bool TryAdd(TKey key)
        {
            var data = this.buffer.AsSetData<TKey>();

            if (Contains(data, key))
            {
                return false;
            }

            // Allocate an entry from the free list
            if (data->AllocatedIndexLength >= data->KeyCapacity && data->FirstFreeIDX < 0)
            {
                var newCap = DynamicHashSetData.GrowCapacity(data->KeyCapacity);
                DynamicHashSetData.ReallocateHashSet<TKey>(this.buffer, newCap, DynamicHashSetData.GetBucketSize(newCap), out data);
            }

            var idx = data->FirstFreeIDX;

            if (idx >= 0)
            {
                data->FirstFreeIDX = ((int*)DynamicHashSetData.GetNexts(data))[idx];
            }
            else
            {
                idx = data->AllocatedIndexLength++;
            }

            CheckIndexOutOfBounds(data, idx);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(DynamicHashSetData.GetKeys(data), idx, key);

            var bucket = key.GetHashCode() & data->BucketCapacityMask;

            // Add the index to the hash-map
            var buckets = (int*)DynamicHashSetData.GetBuckets(data);
            var nextPtrs = (int*)DynamicHashSetData.GetNexts(data);

            nextPtrs[idx] = buckets[bucket];
            buckets[bucket] = idx;

            return true;
        }

        /// <summary>
        /// Add an element with the specified key into the container.
        /// </summary>
        /// <param name="key"> The key of the element to add. </param>
        public void Add(TKey key)
        {
            this.TryAdd(key);
        }

        public bool Contains(TKey key)
        {
            var data = this.buffer.AsSetData<TKey>();

            return Contains(data, key);
        }

        private void Allocate()
        {
            DynamicHashSetData.AllocateHashSet<TKey>(this.buffer, 0, 0);
            this.Clear();
        }

        private static bool Contains(DynamicHashSetData* data, TKey key)
        {
            if (data->AllocatedIndexLength <= 0)
            {
                return false;
            }

            // First find the slot based on the hash
            var buckets = (int*)DynamicHashSetData.GetBuckets(data);
            var bucket = key.GetHashCode() & data->BucketCapacityMask;
            var entryIdx = buckets[bucket];

            if (entryIdx < 0 || entryIdx >= data->KeyCapacity)
            {
                return false;
            }

            var keys = DynamicHashSetData.GetKeys(data);
            int* nextPtrs = (int*)DynamicHashSetData.GetNexts(data);
            while (!UnsafeUtility.ArrayElementAsRef<TKey>(keys, entryIdx).Equals(key))
            {
                entryIdx = nextPtrs[entryIdx];
                if (entryIdx < 0 || entryIdx >= data->KeyCapacity)
                {
                    return false;
                }
            }

            return true;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicHashSetData>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckIndexOutOfBounds(DynamicHashSetData* data, int idx)
        {
            if (idx < 0 || idx >= data->KeyCapacity)
            {
                throw new InvalidOperationException("Internal Map error");
            }
        }
    }
}
