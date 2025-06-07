// <copyright file="HashHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
    internal unsafe struct HashHelper<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        private readonly int keyOffset;
        private readonly int nextOffset;
        private readonly int bucketsOffset;

        public HashHelper(byte* parentPtr, HashHelper<TKey>* thisPtr, int hashMapDataSize, int keyOffset, int nextOffset, int bucketsOffset)
        {
            var ptr = (byte*)thisPtr;
            var offset = (int)(ptr - parentPtr);
            Check.Assume(offset < int.MaxValue);

            this.keyOffset = (hashMapDataSize + keyOffset) - offset;
            this.nextOffset = (hashMapDataSize + nextOffset) - offset;
            this.bucketsOffset = (hashMapDataSize + bucketsOffset) - offset;
        }

        public TKey* Keys => (TKey*)((byte*)UnsafeUtility.AddressOf(ref this) + this.keyOffset);

        public int* Next => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + this.nextOffset);

        public int* Buckets => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + this.bucketsOffset);

        public void Clear(int capacity, int bucketCapacity)
        {
            UnsafeUtility.MemSet(this.Next, 0xff, capacity * sizeof(int));
            UnsafeUtility.MemSet(this.Buckets, 0xff, bucketCapacity * sizeof(int));
        }

        public int Find(TKey key, int capacity, int bucketCapacityMask)
        {
            // First find the slot based on the hash
            var bucket = GetBucket(key, bucketCapacityMask);
            var entryIdx = this.Buckets[bucket];

            if ((uint)entryIdx < (uint)capacity)
            {
                var keys = this.Keys;
                var next = this.Next;

                while (!UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(key))
                {
                    entryIdx = next[entryIdx];
                    if ((uint)entryIdx >= (uint)capacity)
                    {
                        return -1;
                    }
                }

                return entryIdx;
            }

            return -1;
        }

        public void RemoveIndex(int entryIdx, int bucketCapacityMask)
        {
            // We need to iterate until we find the same index
            var index = UnsafeUtility.ReadArrayElement<TKey>(this.Keys, entryIdx);

            var indexBucket = GetBucket(index, bucketCapacityMask);
            var indexPrevEntry = -1;
            var indexEntryIdx = this.Buckets[indexBucket];

            while (entryIdx != indexEntryIdx)
            {
                indexPrevEntry = indexEntryIdx;
                indexEntryIdx = this.Next[indexEntryIdx];
            }

            // Found matching element, remove it
            if (indexPrevEntry < 0)
            {
                this.Buckets[indexBucket] = this.Next[indexEntryIdx];
            }
            else
            {
                this.Next[indexPrevEntry] = this.Next[indexEntryIdx];
            }

            // And free the index
            this.Next[indexEntryIdx] = this.Next[entryIdx]; // TODO we don't add this way so it shouldn't matter
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBucket(in TKey key, int bucketCapacityMask)
        {
            return (int)((uint)key.GetHashCode() & bucketCapacityMask);
        }

        public readonly struct Resize
        {
            public readonly int OldCapacity;
            public readonly int OldBucketCapacity;
            public readonly TKey* OldKeys;
            public readonly int* OldNext;
            public readonly int* OldBuckets;

            public Resize(ref HashHelper<TKey> helper, int oldCapacity, int oldBucketCapacity)
            {
                this.OldCapacity = oldCapacity;
                this.OldBucketCapacity = oldBucketCapacity;
                this.OldKeys = (TKey*)UnsafeUtility.Malloc(oldCapacity * sizeof(TKey), UnsafeUtility.AlignOf<TKey>(), Allocator.Temp);
                this.OldNext = (int*)UnsafeUtility.Malloc(oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
                this.OldBuckets = (int*)UnsafeUtility.Malloc(oldBucketCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);

                UnsafeUtility.MemCpy(this.OldKeys, helper.Keys, oldCapacity * sizeof(TKey));
                UnsafeUtility.MemCpy(this.OldNext, helper.Next, oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(this.OldBuckets, helper.Buckets, oldBucketCapacity * sizeof(int));
            }

            public void Increase(ref HashHelper<TKey> helper, int newCapacity, int newBucketCapacity)
            {
                var newBucketCapacityMask = newBucketCapacity - 1;

                Check.Assume(newCapacity > this.OldCapacity);

                var next = helper.Next;
                var buckets = helper.Buckets;

                UnsafeUtility.MemCpy(helper.Keys, this.OldKeys, this.OldCapacity * sizeof(TKey));

                UnsafeUtility.MemCpy(next, this.OldNext, this.OldCapacity * sizeof(int));
                UnsafeUtility.MemSet(next + this.OldCapacity, 0xff, (newCapacity - this.OldCapacity) * sizeof(int));

                // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
                UnsafeUtility.MemSet(buckets, 0xff, newBucketCapacity * sizeof(int));

                for (var bucket = 0; bucket < this.OldBucketCapacity; ++bucket)
                {
                    while (this.OldBuckets[bucket] >= 0)
                    {
                        var curEntry = this.OldBuckets[bucket];
                        this.OldBuckets[bucket] = next[curEntry];
                        var newBucket = GetBucket(this.OldKeys[curEntry], newBucketCapacityMask);
                        next[curEntry] = buckets[newBucket];
                        buckets[newBucket] = curEntry;
                    }
                }
            }
        }
    }
}
