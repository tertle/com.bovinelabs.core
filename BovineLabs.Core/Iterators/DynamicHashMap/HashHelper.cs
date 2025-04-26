// <copyright file="HashHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public readonly unsafe ref struct HashHelper<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        private readonly int keysOffset;
        private readonly int nextOffset;
        private readonly int bucketsOffset;

        public HashHelper(byte* parentPtr, HashHelper<TKey>* thisPtr, int hashMapDataSize, int keyOffset, int nextOffset, int bucketOffset)
        {
            var ptr = (byte*)thisPtr;
            var offset = (int)(ptr - parentPtr);
            Check.Assume(offset < int.MaxValue);

            this.keysOffset = (hashMapDataSize + keyOffset) - offset;
            this.nextOffset = (hashMapDataSize + nextOffset) - offset;
            this.bucketsOffset = (hashMapDataSize + bucketOffset) - offset;
        }

        internal TKey* Keys
        {
            get
            {
                fixed (HashHelper<TKey>* data = &this)
                {
                    return (TKey*)((byte*)data + data->keysOffset);
                }
            }
        }

        internal int* Next
        {
            get
            {
                fixed (HashHelper<TKey>* data = &this)
                {
                    return (int*)((byte*)data + data->nextOffset);
                }
            }
        }

        internal int* Buckets
        {
            get
            {
                fixed (HashHelper<TKey>* data = &this)
                {
                    return (int*)((byte*)data + data->bucketsOffset);
                }
            }
        }

        public void Clear(int capacity, int bucketCapacity)
        {
            UnsafeUtility.MemSet(this.Next, 0xff, capacity * sizeof(int));
            UnsafeUtility.MemSet(this.Buckets, 0xff, bucketCapacity * sizeof(int));
        }

        internal int Find(TKey key, int capacity, int bucketCapacityMask)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetBucket(in TKey key, int bucketCapacityMask)
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
