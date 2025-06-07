// <copyright file="OrderedListColumn.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators.Columns
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> Iterator for traversing elements in an OrderedListColumn in sorted order. </summary>
    /// <remarks>
    /// This struct is used with TryGetFirst() and TryGetNext() methods to iterate through
    /// the ordered list. The EntryIndex field contains the current storage index being visited,
    /// while NextEntryIndex is used internally to track the next element in the sorted chain.
    /// </remarks>
    public struct OrderedListIterator
    {
        /// <summary>The storage index of the current element being visited.</summary>
        public int EntryIndex;

        /// <summary>Internal field used to track the next element in the iteration chain.</summary>
        internal int NextEntryIndex;
    }

    public unsafe struct OrderedListColumn<T> : IColumn<T>
        where T : unmanaged, IEquatable<T>, IComparable<T>
    {
        private int keysOffset;
        private int nextOffset;
        private int prevOffset;
        private int head;
        private int capacity;

        private T* Keys => (T*)((byte*)UnsafeUtility.AddressOf(ref this) + this.keysOffset);
        private int* Next => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + this.nextOffset);
        private int* Prev => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + this.prevOffset);

        /// <summary> Gets the value stored at the specified index. </summary>
        /// <param name="idx">The index of the element to retrieve. This is the storage index, not the sorted position.</param>
        /// <returns>The value stored at the specified index.</returns>
        /// <remarks>
        /// This method retrieves values by their storage index, not by their position in the sorted order.
        /// To iterate through values in sorted order, use GetFirst() and GetNext() methods.
        /// The index must be valid (0 &lt;= idx &lt; capacity) and should correspond to an element that was previously added.
        /// </remarks>
        public T GetValue(int idx)
        {
            return UnsafeUtility.ReadArrayElement<T>(this.Keys, idx);
        }

        /// <summary> Gets the index of the first element in sorted order. </summary>
        /// <returns>Index of first element, or -1 if list is empty.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetFirst()
        {
            return this.head;
        }

        /// <summary> Gets the index of the next element after the given index in sorted order. </summary>
        /// <param name="current">Current index in the iteration.</param>
        /// <returns>Index of next element, or -1 if at end of list.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNext(int current)
        {
            return this.Next[current];
        }

        /// <summary> Gets the first element in sorted order using iterator pattern. </summary>
        /// <param name="value"> The value of the first element, or default if the list is empty. </param>
        /// <param name="it"> Iterator that will be initialized to point to the first element. </param>
        /// <returns> True if there is a first element, false if the list is empty. </returns>
        /// <remarks>
        /// This method initializes the iterator and retrieves the first element in one operation.
        /// Use with TryGetNext() to iterate through all elements in ascending sorted order.
        /// </remarks>
        public bool TryGetFirst(out T value, out OrderedListIterator it)
        {
            it.EntryIndex = -1;
            it.NextEntryIndex = this.head;
            return this.TryGetNext(out value, ref it);
        }

        /// <summary> Gets the next element in sorted order using iterator pattern. </summary>
        /// <param name="value"> The value of the next element, or default if at the end of the list. </param>
        /// <param name="it"> Iterator that will be advanced to the next element. </param>
        /// <returns> True if there is a next element, false if at the end of the list. </returns>
        /// <remarks>
        /// This method advances the iterator to the next element and retrieves its value.
        /// The iterator must have been initialized by TryGetFirst() or a previous successful TryGetNext() call.
        /// </remarks>
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

            it.NextEntryIndex = this.Next[entryIdx];
            it.EntryIndex = entryIdx;
            value = this.GetValue(entryIdx);
            return true;
        }

        void IColumn<T>.Initialize(int offset, int newCapacity)
        {
            this.capacity = newCapacity;

            this.keysOffset = offset;
            this.nextOffset = this.keysOffset + sizeof(T) * newCapacity;
            this.prevOffset = this.nextOffset + sizeof(int) * newCapacity;
            this.head = -1;
        }

        int IColumn<T>.CalculateDataSize(int newCapacity)
        {
            var keySize = sizeof(T) * newCapacity;
            var nextSize = sizeof(int) * newCapacity;
            var prevSize = sizeof(int) * newCapacity;
            return keySize + nextSize + prevSize;
        }

        void IColumn<T>.Add(T key, int idx)
        {
            this.AddInternal(key, idx);
        }

        void IColumn<T>.Remove(int idx)
        {
            this.RemoveInternal(idx);
        }

        void IColumn<T>.Replace(T newKey, int idx)
        {
            var keys = this.Keys;
            var prev = this.Prev;
            var next = this.Next;
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
                this.RemoveInternal(idx);
                this.AddInternal(newKey, idx);
            }
        }

        void IColumn<T>.Clear()
        {
            UnsafeUtility.MemSet(this.Next, 0xff, this.capacity * sizeof(int));
            UnsafeUtility.MemSet(this.Prev, 0xff, this.capacity * sizeof(int));
            this.head = -1;
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
            var keys = this.Keys;
            var next = this.Next;
            var prev = this.Prev;

            keys[idx] = key;
            next[idx] = -1;
            prev[idx] = -1;

            // If list is empty, make this the head
            if (this.head == -1)
            {
                this.head = idx;
                return;
            }

            // If new value should be the new head
            if (key.CompareTo(keys[this.head]) < 0)
            {
                next[idx] = this.head;
                prev[this.head] = idx;
                this.head = idx;
                return;
            }

            // Find the correct position to insert
            var current = this.head;
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
            var next = this.Next;
            var prev = this.Prev;

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
                this.head = nextNode;
            }

            // Update next node's prev pointer
            if (nextNode != -1)
            {
                prev[nextNode] = prevNode;
            }
        }

        private readonly struct Resize
        {
            private readonly int oldCapacity;
            private readonly int oldHead;
            private readonly T* oldKeys;
            private readonly int* oldNext;
            private readonly int* oldPrev;

            public Resize(ref OrderedListColumn<T> column)
            {
                this.oldCapacity = column.capacity;
                this.oldHead = column.head;
                this.oldKeys = (T*)UnsafeUtility.Malloc(this.oldCapacity * sizeof(T), UnsafeUtility.AlignOf<T>(), Allocator.Temp);
                this.oldNext = (int*)UnsafeUtility.Malloc(this.oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
                this.oldPrev = (int*)UnsafeUtility.Malloc(this.oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);

                UnsafeUtility.MemCpy(this.oldKeys, column.Keys, this.oldCapacity * sizeof(T));
                UnsafeUtility.MemCpy(this.oldNext, column.Next, this.oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(this.oldPrev, column.Prev, this.oldCapacity * sizeof(int));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T GetValue(int idx)
            {
                return this.oldKeys[idx];
            }

            public void Increase(ref OrderedListColumn<T> helper)
            {
                Check.Assume(helper.capacity > this.oldCapacity);

                var keys = helper.Keys;
                var next = helper.Next;
                var prev = helper.Prev;
                helper.head = this.oldHead;

                UnsafeUtility.MemCpy(keys, this.oldKeys, this.oldCapacity * sizeof(T));
                UnsafeUtility.MemCpy(next, this.oldNext, this.oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(prev, this.oldPrev, this.oldCapacity * sizeof(int));

                UnsafeUtility.MemSet(next + this.oldCapacity, 0xff, (helper.capacity - this.oldCapacity) * sizeof(int));
                UnsafeUtility.MemSet(prev + this.oldCapacity, 0xff, (helper.capacity - this.oldCapacity) * sizeof(int));

                // Clean up internal temporary allocations
                UnsafeUtility.Free(this.oldKeys, Allocator.Temp);
                UnsafeUtility.Free(this.oldNext, Allocator.Temp);
                UnsafeUtility.Free(this.oldPrev, Allocator.Temp);
            }
        }
    }
}