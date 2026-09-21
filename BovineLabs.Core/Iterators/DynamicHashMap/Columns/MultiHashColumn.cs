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
        private int _keysOffset;
        private int _nextOffset;
        private int _bucketsOffset;
        private int _capacity;

        private T* Keys => (T*)((byte*)UnsafeUtility.AddressOf(ref this) + _keysOffset);

        private int* Next => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + _nextOffset);

        private int* Buckets => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + _bucketsOffset);

        public bool TryGetFirst(T column, out HashMapIterator<T> it)
        {
            it.Key = column;

            // First find the slot based on the hash
            var bucket = GetBucket(it.Key, GetBucketCapacityMask(_capacity));
            it.EntryIndex = it.NextEntryIndex = Buckets[bucket];

            return TryGetNext(ref it);
        }

        public bool TryGetNext(ref HashMapIterator<T> it)
        {
            var entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;

            if (entryIdx < 0 || entryIdx >= _capacity)
            {
                return false;
            }

            var next = Next;
            var keys = Keys;

            while (!UnsafeUtility.ReadArrayElement<T>(keys, entryIdx).Equals(it.Key))
            {
                entryIdx = next[entryIdx];
                if ((uint)entryIdx >= (uint)_capacity)
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
            _capacity = newCapacity;

            _keysOffset = offset;

            var nextOffset = CollectionHelper.Align(_keysOffset + (sizeof(T) * newCapacity), UnsafeUtility.AlignOf<int>());
            _nextOffset = nextOffset;

            var bucketsOffset = CollectionHelper.Align(_nextOffset + (sizeof(int) * newCapacity), UnsafeUtility.AlignOf<int>());
            _bucketsOffset = bucketsOffset;
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
            return UnsafeUtility.ReadArrayElement<T>(Keys, idx);
        }

        void IColumn<T>.Add(T key, int idx)
        {
            AddInternal(key, idx);
        }

        void IColumn<T>.Replace(T newKey, int idx)
        {
            var oldKey = Keys[idx];

            // If the value hasn't changed, nothing to do
            if (newKey.Equals(oldKey))
            {
                return;
            }

            var bucketCapacityMask = GetBucketCapacityMask(_capacity);
            var oldBucket = GetBucket(oldKey, bucketCapacityMask);
            var newBucket = GetBucket(newKey, bucketCapacityMask);

            if (oldBucket == newBucket)
            {
                // Optimization: just update the key in place since it hashes to the same bucket
                Keys[idx] = newKey;
            }
            else
            {
                // Need to move to different bucket: remove and re-add
                RemoveInternal(idx);
                AddInternal(newKey, idx);
            }
        }

        void IColumn<T>.Remove(int idx)
        {
            RemoveInternal(idx);
        }

        void IColumn<T>.Clear()
        {
            UnsafeUtility.MemClear(Keys, (long)_capacity * sizeof(T));
            UnsafeUtility.MemSet(Next, 0xff, _capacity * sizeof(int));
            UnsafeUtility.MemSet(Buckets, 0xff, GetBucketCapacity(_capacity) * sizeof(int));
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
            Keys[idx] = key;
            var bucketCapacityMask = GetBucketCapacityMask(_capacity);
            var bucket = GetBucket(key, bucketCapacityMask);
            Next[idx] = Buckets[bucket];
            Buckets[bucket] = idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveInternal(int idx)
        {
            var bucketCapacityMask = GetBucketCapacityMask(_capacity);

            // We need to iterate until we find the same index
            var index = UnsafeUtility.ReadArrayElement<T>(Keys, idx);

            var bucket = GetBucket(index, bucketCapacityMask);
            var prevEntry = -1;
            var entryIdx = Buckets[bucket];

            while (entryIdx != idx)
            {
                prevEntry = entryIdx;
                entryIdx = Next[entryIdx];

                Check.Assume(entryIdx != -1);
            }

            // Found matching element, remove it
            if (prevEntry < 0)
            {
                Buckets[bucket] = Next[entryIdx];
            }
            else
            {
                Next[prevEntry] = Next[entryIdx];
            }

            // And free the index
            Next[entryIdx] = -1;
            UnsafeUtility.MemClear(Keys + entryIdx, sizeof(T));
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
            private readonly int _oldCapacity;
            private readonly int _oldBucketCapacity;
            private readonly T* _oldKeys;
            private readonly int* _oldNext;
            private readonly int* _oldBuckets;

            public Resize(ref MultiHashColumn<T> column)
            {
                _oldCapacity = column._capacity;
                _oldBucketCapacity = GetBucketCapacity(column._capacity);
                _oldKeys = (T*)UnsafeUtility.Malloc(_oldCapacity * sizeof(T), UnsafeUtility.AlignOf<T>(), Allocator.Temp);
                _oldNext = (int*)UnsafeUtility.Malloc(_oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
                _oldBuckets = (int*)UnsafeUtility.Malloc(_oldBucketCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);

                UnsafeUtility.MemCpy(_oldKeys, column.Keys, _oldCapacity * sizeof(T));
                UnsafeUtility.MemCpy(_oldNext, column.Next, _oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(_oldBuckets, column.Buckets, _oldBucketCapacity * sizeof(int));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T GetKey(int idx)
            {
                return _oldKeys[idx];
            }

            public void Increase(ref MultiHashColumn<T> helper)
            {
                Check.Assume(helper._capacity > _oldCapacity);

                var newBucketCapacity = GetBucketCapacity(helper._capacity);
                var newBucketCapacityMask = newBucketCapacity - 1;

                var keys = helper.Keys;
                var next = helper.Next;
                var buckets = helper.Buckets;

                UnsafeUtility.MemClear(keys, (long)helper._capacity * sizeof(T));

                // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
                UnsafeUtility.MemSet(next, 0xff, helper._capacity * sizeof(int));
                UnsafeUtility.MemSet(buckets, 0xff, newBucketCapacity * sizeof(int));

                for (int i = 0; i < _oldBucketCapacity; i++)
                {
                    for (var idx = _oldBuckets[i]; idx != -1; idx = _oldNext[idx])
                    {
                        UnsafeUtility.MemCpy(keys + idx, _oldKeys + idx, sizeof(T));
                        var bucket = GetBucket(_oldKeys[idx], newBucketCapacityMask);

                        next[idx] = buckets[bucket];
                        buckets[bucket] = idx;
                    }
                }
            }
        }
    }
}
