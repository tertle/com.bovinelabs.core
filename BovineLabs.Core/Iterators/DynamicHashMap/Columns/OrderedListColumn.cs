namespace BovineLabs.Core.Iterators.Columns
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public struct OrderedListIterator
    {
        public int EntryIndex;

        internal int NextEntryIndex;
    }

    public unsafe struct OrderedListColumn<T> : IColumn<T>
        where T : unmanaged, IEquatable<T>, IComparable<T>
    {
        private int _keysOffset;
        private int _nextOffset;
        private int _prevOffset;
        private int _head;
        private int _capacity;

        private T* Keys => (T*)((byte*)UnsafeUtility.AddressOf(ref this) + _keysOffset);
        private int* Next => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + _nextOffset);
        private int* Prev => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + _prevOffset);

        /// <summary>
        /// Indexes storage, not sorted position. Use GetFirst/GetNext for sorted traversal.
        /// </summary>
        public T GetValue(int idx)
        {
            return UnsafeUtility.ReadArrayElement<T>(Keys, idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetFirst()
        {
            return _head;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNext(int current)
        {
            return Next[current];
        }

        public bool TryGetFirst(out T value, out OrderedListIterator it)
        {
            it.EntryIndex = -1;
            it.NextEntryIndex = _head;
            return TryGetNext(out value, ref it);
        }

        public bool TryGetNext(out T value, ref OrderedListIterator it)
        {
            var entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;

            if (entryIdx < 0)
            {
                value = default;
                return false;
            }

            it.NextEntryIndex = Next[entryIdx];
            it.EntryIndex = entryIdx;
            value = GetValue(entryIdx);
            return true;
        }

        void IColumn<T>.Initialize(int offset, int newCapacity)
        {
            _capacity = newCapacity;

            _keysOffset = offset;
            _nextOffset = CollectionHelper.Align(_keysOffset + (sizeof(T) * newCapacity), UnsafeUtility.AlignOf<int>());
            _prevOffset = CollectionHelper.Align(_nextOffset + (sizeof(int) * newCapacity), UnsafeUtility.AlignOf<int>());
            _head = -1;
        }

        int IColumn<T>.CalculateDataSize(int newCapacity)
        {
            var keySize = sizeof(T) * newCapacity;

            var nextOffset = CollectionHelper.Align(keySize, UnsafeUtility.AlignOf<int>());
            var nextSize = sizeof(int) * newCapacity;

            var prevOffset = CollectionHelper.Align(nextOffset + nextSize, UnsafeUtility.AlignOf<int>());
            var prevSize = sizeof(int) * newCapacity;

            return prevOffset + prevSize;
        }

        void IColumn<T>.Add(T key, int idx)
        {
            AddInternal(key, idx);
        }

        void IColumn<T>.Remove(int idx)
        {
            RemoveInternal(idx);
        }

        void IColumn<T>.Replace(T newKey, int idx)
        {
            var keys = Keys;
            var prev = Prev;
            var next = Next;
            var oldKey = keys[idx];

            // If the value hasn't changed, nothing to do
            if (newKey.Equals(oldKey))
            {
                return;
            }

            var prevNode = prev[idx];
            var nextNode = next[idx];

            // Check if the new value can stay in the same position
            // Check against previous element (must be <= newKey)
            // Check against next element (must be >= newKey)
            var canStayInPlace = (prevNode == -1 || keys[prevNode].CompareTo(newKey) <= 0) &&
                (nextNode == -1 || newKey.CompareTo(keys[nextNode]) <= 0);

            if (canStayInPlace)
            {
                // Optimization: just update the value in place
                keys[idx] = newKey;
            }
            else
            {
                // Need to reposition: remove and re-add
                RemoveInternal(idx);
                AddInternal(newKey, idx);
            }
        }

        void IColumn<T>.Clear()
        {
            UnsafeUtility.MemClear(Keys, (long)_capacity * sizeof(T));
            UnsafeUtility.MemSet(Next, 0xff, _capacity * sizeof(int));
            UnsafeUtility.MemSet(Prev, 0xff, _capacity * sizeof(int));
            _head = -1;
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
            UnsafeUtility.Free(resizePtr, Allocator.Temp);
        }

        T IColumn<T>.GetValueOld(void* resizePtr, int idx)
        {
            var resize = (Resize*)resizePtr;
            return resize->GetValue(idx);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddInternal(T key, int idx)
        {
            var keys = Keys;
            var next = Next;
            var prev = Prev;

            keys[idx] = key;
            next[idx] = -1;
            prev[idx] = -1;

            // If list is empty, make this the head
            if (_head == -1)
            {
                _head = idx;
                return;
            }

            // If new value should be the new head
            if (key.CompareTo(keys[_head]) < 0)
            {
                next[idx] = _head;
                prev[_head] = idx;
                _head = idx;
                return;
            }

            // Find the correct position to insert
            var current = _head;
            while (next[current] != -1 && keys[next[current]].CompareTo(key) < 0)
            {
                current = next[current];
            }

            // Insert after current
            var nextNode = next[current];
            next[idx] = nextNode;
            next[current] = idx;
            prev[idx] = current;

            if (nextNode != -1)
            {
                prev[nextNode] = idx;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveInternal(int idx)
        {
            var next = Next;
            var prev = Prev;

            var prevNode = prev[idx];
            var nextNode = next[idx];

            // Update previous node's next pointer
            if (prevNode != -1)
            {
                next[prevNode] = nextNode;
            }
            else
            {
                // idx was the head
                _head = nextNode;
            }

            // Update next node's prev pointer
            if (nextNode != -1)
            {
                prev[nextNode] = prevNode;
            }

            UnsafeUtility.MemClear(Keys + idx, sizeof(T));
            next[idx] = -1;
            prev[idx] = -1;
        }

        private readonly struct Resize
        {
            private readonly int _oldCapacity;
            private readonly int _oldHead;
            private readonly T* _oldKeys;
            private readonly int* _oldNext;
            private readonly int* _oldPrev;

            public Resize(ref OrderedListColumn<T> column)
            {
                _oldCapacity = column._capacity;
                _oldHead = column._head;
                _oldKeys = (T*)UnsafeUtility.Malloc(_oldCapacity * sizeof(T), UnsafeUtility.AlignOf<T>(), Allocator.Temp);
                _oldNext = (int*)UnsafeUtility.Malloc(_oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
                _oldPrev = (int*)UnsafeUtility.Malloc(_oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);

                UnsafeUtility.MemCpy(_oldKeys, column.Keys, _oldCapacity * sizeof(T));
                UnsafeUtility.MemCpy(_oldNext, column.Next, _oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(_oldPrev, column.Prev, _oldCapacity * sizeof(int));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T GetValue(int idx)
            {
                return _oldKeys[idx];
            }

            public void Increase(ref OrderedListColumn<T> helper)
            {
                Check.Assume(helper._capacity > _oldCapacity);

                var keys = helper.Keys;
                var next = helper.Next;
                var prev = helper.Prev;
                helper._head = _oldHead;

                UnsafeUtility.MemClear(keys, (long)helper._capacity * sizeof(T));
                for (var idx = _oldHead; idx != -1; idx = _oldNext[idx])
                {
                    UnsafeUtility.MemCpy(keys + idx, _oldKeys + idx, sizeof(T));
                }

                UnsafeUtility.MemCpy(next, _oldNext, _oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(prev, _oldPrev, _oldCapacity * sizeof(int));

                UnsafeUtility.MemSet(next + _oldCapacity, 0xff, (helper._capacity - _oldCapacity) * sizeof(int));
                UnsafeUtility.MemSet(prev + _oldCapacity, 0xff, (helper._capacity - _oldCapacity) * sizeof(int));

                // Clean up internal temporary allocations
                UnsafeUtility.Free(_oldKeys, Allocator.Temp);
                UnsafeUtility.Free(_oldNext, Allocator.Temp);
                UnsafeUtility.Free(_oldPrev, Allocator.Temp);
            }
        }
    }
}
