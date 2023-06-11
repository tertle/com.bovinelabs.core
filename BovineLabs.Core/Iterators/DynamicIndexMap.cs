// <copyright file="DynamicIndexMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct DynamicIndexMap<TValue>
        where TValue : unmanaged
    {
        private DynamicBuffer<byte> buffer;

        internal DynamicIndexMap(DynamicBuffer<byte> buffer)
        {
            CheckAllocated(buffer);
            this.buffer = buffer;
        }

        internal DynamicIndexMap(DynamicBuffer<byte> buffer, int bucketLength, int length = 0)
        {
            CheckUnallocated(buffer);
            this.buffer = buffer;
            DynamicIndexMapData.AllocateIndexMap<TValue>(buffer, length, bucketLength);
            this.Clear();
        }

        public void Add(int key, TValue item)
        {
            var data = this.buffer.AsIndexData<TValue>();

            CheckIndex(data, key);

            // Allocate an entry from the free list
            if (data->AllocatedIndexLength >= data->ValueCapacity && data->FirstFreeIDX < 0)
            {
                var newCap = DynamicIndexMapData.GrowCapacity(data->ValueCapacity);
                DynamicIndexMapData.ReallocateHashMap<TValue>(this.buffer, newCap, out data);
            }

            var idx = data->FirstFreeIDX;

            if (idx >= 0)
            {
                data->FirstFreeIDX = DynamicIndexMapData.GetNexts(data)[idx];
            }
            else
            {
                idx = data->AllocatedIndexLength++;
            }

            CheckIdxOutOfBounds(data, idx);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(DynamicIndexMapData.GetValues(data), idx, item);

            // Add the index to the hash-map
            var buckets = DynamicIndexMapData.GetBuckets(data);
            var nextPtrs = DynamicIndexMapData.GetNexts(data);

            nextPtrs[idx] = buckets[key];
            buckets[key] = idx;
        }

        private void Clear()
        {
            var data = this.buffer.AsIndexData<TValue>();

            UnsafeUtility.MemSet(DynamicIndexMapData.GetBuckets(data), 0xff, (data->BucketLength + 1) * 4);
            UnsafeUtility.MemSet(DynamicIndexMapData.GetNexts(data), 0xff, data->ValueCapacity * 4);

            data->FirstFreeIDX = -1;
            data->AllocatedIndexLength = 0;
        }

        private static void CheckIndex(DynamicIndexMapData* data, int index)
        {
            if (index < 0 || index >= data->BucketLength)
            {
                throw new IndexOutOfRangeException("Trying to add out of range");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckIdxOutOfBounds(DynamicIndexMapData* data, int idx)
        {
            if (idx < 0 || idx >= data->ValueCapacity)
            {
                throw new InvalidOperationException("Internal Map error");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocated(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer unallocated.");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicIndexMapData>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckUnallocated(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length != 0)
            {
                throw new InvalidOperationException("Buffer already allocated");
            }
        }
    }
}
