// <copyright file="MultiColumn.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators.Columns
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct MultiHashColumn<T> : IColumn<T>
        where T : unmanaged, IEquatable<T>
    {
        private int keysOffset;
        private int nextOffset;
        private int bucketsOffset;
        private int capacity;

        private T* Keys => (T*)((byte*)UnsafeUtility.AddressOf(ref this) + this.keysOffset);

        private int* Next => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + this.nextOffset);

        private int* Buckets => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + this.bucketsOffset);

        public bool TryGetFirst(T column, out HashMapIterator<T> it)
        {
            it.Key = column;

            // First find the slot based on the hash
            var bucket = GetBucket(it.Key, GetBucketCapacityMask(this.capacity));
            it.EntryIndex = it.NextEntryIndex = this.Buckets[bucket];

            return this.TryGetNext(ref it);
        }

        public bool TryGetNext(ref HashMapIterator<T> it)
        {
            var entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;

            if (entryIdx < 0 || entryIdx >= this.capacity)
            {
                return false;
            }

            var next = this.Next;
            var keys = this.Keys;

            while (!UnsafeUtility.ReadArrayElement<T>(keys, entryIdx).Equals(it.Key))
            {
                entryIdx = next[entryIdx];
                if ((uint)entryIdx >= (uint)this.capacity)
                {
                    return false;
                }
            }

            it.NextEntryIndex = next[entryIdx];
            it.EntryIndex = entryIdx;
            return true;
        }

        void IColumn<T>.Initialize(int offset, int newCapacity)
        {
            this.capacity = newCapacity;

            this.keysOffset = offset;

            var nextOffset = CollectionHelper.Align(this.keysOffset + (sizeof(T) * newCapacity), UnsafeUtility.AlignOf<int>());
            this.nextOffset = nextOffset;

            var bucketsOffset = CollectionHelper.Align(this.nextOffset + (sizeof(int) * newCapacity), UnsafeUtility.AlignOf<int>());
            this.bucketsOffset = bucketsOffset;
        }

        int IColumn<T>.CalculateDataSize(int newCapacity)
        {
            var keySize = sizeof(T) * newCapacity;

            var nextOffset = CollectionHelper.Align(keySize, UnsafeUtility.AlignOf<int>());
            var nextSize = sizeof(int) * newCapacity;

            var bucketsOffset = CollectionHelper.Align(nextOffset + nextSize, UnsafeUtility.AlignOf<int>());
            var bucketSize = sizeof(int) * GetBucketCapacity(newCapacity);

            return bucketsOffset + bucketSize;
        }

        T IColumn<T>.GetValue(int idx)
        {
            return UnsafeUtility.ReadArrayElement<T>(this.Keys, idx);
        }

        void IColumn<T>.Add(T key, int idx)
        {
            this.AddInternal(key, idx);
        }

        void IColumn<T>.Replace(T newKey, int idx)
        {
            var oldKey = this.Keys[idx];

            // If the value hasn't changed, nothing to do
            if (newKey.Equals(oldKey))
            {
                return;
            }

            var bucketCapacityMask = GetBucketCapacityMask(this.capacity);
            var oldBucket = GetBucket(oldKey, bucketCapacityMask);
            var newBucket = GetBucket(newKey, bucketCapacityMask);

            if (oldBucket == newBucket)
            {
                // Optimization: just update the key in place since it hashes to the same bucket
                this.Keys[idx] = newKey;
            }
            else
            {
                // Need to move to different bucket: remove and re-add
                this.RemoveInternal(idx);
                this.AddInternal(newKey, idx);
            }
        }

        void IColumn<T>.Remove(int idx)
        {
            this.RemoveInternal(idx);
        }

        void IColumn<T>.Clear()
        {
            UnsafeUtility.MemSet(this.Next, 0xff, this.capacity * sizeof(int));
            UnsafeUtility.MemSet(this.Buckets, 0xff, GetBucketCapacity(this.capacity) * sizeof(int));
        }

        void* IColumn<T>.StartResize()
        {
            var resize = (Resize*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<Resize>(), UnsafeUtility.AlignOf<Resize>(), Allocator.Temp);
            *resize = new Resize(ref this);
            return resize;
        }

        void IColumn<T>.ApplyResize(void* resizePtr)
        {
            var resize = (Resize*)resizePtr;
            resize->Increase(ref this);
        }

        T IColumn<T>.GetValueOld(void* resizePtr, int idx)
        {
            var resize = (Resize*)resizePtr;
            return resize->GetKey(idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddInternal(T key, int idx)
        {
            this.Keys[idx] = key;
            var bucketCapacityMask = GetBucketCapacityMask(this.capacity);
            var bucket = GetBucket(key, bucketCapacityMask);
            this.Next[idx] = this.Buckets[bucket];
            this.Buckets[bucket] = idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveInternal(int idx)
        {
            var bucketCapacityMask = GetBucketCapacityMask(this.capacity);

            // We need to iterate until we find the same index
            var index = UnsafeUtility.ReadArrayElement<T>(this.Keys, idx);

            var bucket = GetBucket(index, bucketCapacityMask);
            var prevEntry = -1;
            var entryIdx = this.Buckets[bucket];

            while (entryIdx != idx)
            {
                prevEntry = entryIdx;
                entryIdx = this.Next[entryIdx];

                Check.Assume(entryIdx != -1);
            }

            // Found matching element, remove it
            if (prevEntry < 0)
            {
                this.Buckets[bucket] = this.Next[entryIdx];
            }
            else
            {
                this.Next[prevEntry] = this.Next[entryIdx];
            }

            // And free the index
            this.Next[entryIdx] = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBucket(in T key, int bucketCapacityMask)
        {
            return (int)((uint)key.GetHashCode() & bucketCapacityMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBucketCapacity(int capacity)
        {
            return capacity * 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBucketCapacityMask(int capacity)
        {
            return GetBucketCapacity(capacity) - 1;
        }

        private readonly struct Resize
        {
            private readonly int oldCapacity;
            private readonly int oldBucketCapacity;
            private readonly T* oldKeys;
            private readonly int* oldNext;
            private readonly int* oldBuckets;

            public Resize(ref MultiHashColumn<T> column)
            {
                this.oldCapacity = column.capacity;
                this.oldBucketCapacity = GetBucketCapacity(column.capacity);
                this.oldKeys = (T*)UnsafeUtility.Malloc(this.oldCapacity * sizeof(T), UnsafeUtility.AlignOf<T>(), Allocator.Temp);
                this.oldNext = (int*)UnsafeUtility.Malloc(this.oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
                this.oldBuckets = (int*)UnsafeUtility.Malloc(this.oldBucketCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);

                UnsafeUtility.MemCpy(this.oldKeys, column.Keys, this.oldCapacity * sizeof(T));
                UnsafeUtility.MemCpy(this.oldNext, column.Next, this.oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(this.oldBuckets, column.Buckets, this.oldBucketCapacity * sizeof(int));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T GetKey(int idx)
            {
                return this.oldKeys[idx];
            }

            public void Increase(ref MultiHashColumn<T> helper)
            {
                Check.Assume(helper.capacity > this.oldCapacity);

                var newBucketCapacity = GetBucketCapacity(helper.capacity);
                var newBucketCapacityMask = newBucketCapacity - 1;

                var keys = helper.Keys;
                var next = helper.Next;
                var buckets = helper.Buckets;

                UnsafeUtility.MemCpy(keys, this.oldKeys, this.oldCapacity * sizeof(T));

                // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
                UnsafeUtility.MemSet(next, 0xff, helper.capacity * sizeof(int));
                UnsafeUtility.MemSet(buckets, 0xff, newBucketCapacity * sizeof(int));

                for (int i = 0; i < this.oldBucketCapacity; i++)
                {
                    for (var idx = this.oldBuckets[i]; idx != -1; idx = this.oldNext[idx])
                    {
                        var bucket = GetBucket(this.oldKeys[idx], newBucketCapacityMask);

                        next[idx] = buckets[bucket];
                        buckets[bucket] = idx;
                    }
                }
            }
        }
    }
}
