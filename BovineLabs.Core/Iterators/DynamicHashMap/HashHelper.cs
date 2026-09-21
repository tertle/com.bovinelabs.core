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
        private readonly int _keyOffset;
        private readonly int _nextOffset;
        private readonly int _bucketsOffset;

        public HashHelper(byte* parentPtr, HashHelper<TKey>* thisPtr, int hashMapDataSize, int keyOffset, int nextOffset, int bucketsOffset)
        {
            var ptr = (byte*)thisPtr;
            var offset = (int)(ptr - parentPtr);
            Check.Assume(offset < int.MaxValue);

            _keyOffset = (hashMapDataSize + keyOffset) - offset;
            _nextOffset = (hashMapDataSize + nextOffset) - offset;
            _bucketsOffset = (hashMapDataSize + bucketsOffset) - offset;
        }

        public TKey* Keys => (TKey*)((byte*)UnsafeUtility.AddressOf(ref this) + _keyOffset);

        public int* Next => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + _nextOffset);

        public int* Buckets => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + _bucketsOffset);

        public void Clear(int capacity, int bucketCapacity)
        {
            UnsafeUtility.MemClear(Keys, (long)capacity * sizeof(TKey));
            UnsafeUtility.MemSet(Next, 0xff, capacity * sizeof(int));
            UnsafeUtility.MemSet(Buckets, 0xff, bucketCapacity * sizeof(int));
        }

        public int Find(TKey key, int capacity, int bucketCapacityMask)
        {
            // First find the slot based on the hash
            var bucket = GetBucket(key, bucketCapacityMask);
            var entryIdx = Buckets[bucket];

            if ((uint)entryIdx < (uint)capacity)
            {
                var keys = Keys;
                var next = Next;

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
            var index = UnsafeUtility.ReadArrayElement<TKey>(Keys, entryIdx);

            var indexBucket = GetBucket(index, bucketCapacityMask);
            var indexPrevEntry = -1;
            var indexEntryIdx = Buckets[indexBucket];

            while (entryIdx != indexEntryIdx)
            {
                indexPrevEntry = indexEntryIdx;
                indexEntryIdx = Next[indexEntryIdx];
            }

            // Found matching element, remove it
            if (indexPrevEntry < 0)
            {
                Buckets[indexBucket] = Next[indexEntryIdx];
            }
            else
            {
                Next[indexPrevEntry] = Next[indexEntryIdx];
            }

            // And free the index
            Next[indexEntryIdx] = Next[entryIdx]; // TODO we don't add this way so it shouldn't matter
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
                OldCapacity = oldCapacity;
                OldBucketCapacity = oldBucketCapacity;
                OldKeys = (TKey*)UnsafeUtility.Malloc(oldCapacity * sizeof(TKey), UnsafeUtility.AlignOf<TKey>(), Allocator.Temp);
                OldNext = (int*)UnsafeUtility.Malloc(oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
                OldBuckets = (int*)UnsafeUtility.Malloc(oldBucketCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);

                UnsafeUtility.MemCpy(OldKeys, helper.Keys, oldCapacity * sizeof(TKey));
                UnsafeUtility.MemCpy(OldNext, helper.Next, oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(OldBuckets, helper.Buckets, oldBucketCapacity * sizeof(int));
            }

            public void Increase(ref HashHelper<TKey> helper, int newCapacity, int newBucketCapacity)
            {
                var newBucketCapacityMask = newBucketCapacity - 1;

                Check.Assume(newCapacity > OldCapacity);

                var next = helper.Next;
                var buckets = helper.Buckets;

                UnsafeUtility.MemClear(helper.Keys, (long)newCapacity * sizeof(TKey));

                UnsafeUtility.MemCpy(next, OldNext, OldCapacity * sizeof(int));
                UnsafeUtility.MemSet(next + OldCapacity, 0xff, (newCapacity - OldCapacity) * sizeof(int));

                // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
                UnsafeUtility.MemSet(buckets, 0xff, newBucketCapacity * sizeof(int));

                for (var bucket = 0; bucket < OldBucketCapacity; ++bucket)
                {
                    while (OldBuckets[bucket] >= 0)
                    {
                        var curEntry = OldBuckets[bucket];
                        UnsafeUtility.MemCpy(helper.Keys + curEntry, OldKeys + curEntry, sizeof(TKey));
                        OldBuckets[bucket] = next[curEntry];
                        var newBucket = GetBucket(OldKeys[curEntry], newBucketCapacityMask);
                        next[curEntry] = buckets[newBucket];
                        buckets[newBucket] = curEntry;
                    }
                }
            }
        }
    }
}
