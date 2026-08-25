// <copyright file="DynamicMultiDictionary.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Describes an entry stored in a <see cref="DynamicMultiDictionary{TKey, TValue, TEntry}" /> buffer.
    /// </summary>
    /// <remarks>
    /// Implementations should keep the struct unmanaged and use 0 in <see cref="Tag" /> to represent empty slots.
    /// Odd tag values store hash fingerprints used to accelerate key comparisons, while even non-zero tags represent tombstones.
    /// </remarks>
    public interface IDynamicMultiDictionaryEntry<TKey, TValue> : IBufferElementData
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        uint Tag { get; set; }

        TKey Key { get; set; }

        TValue Value { get; set; }
    }

    /// <summary>
    /// Entry-backed multi dictionary stored directly in a <see cref="DynamicBuffer{T}" /> with a reserved capacity-only header slot.
    /// </summary>
    /// <remarks>
    /// The multi dictionary stores entries directly in the dynamic buffer and allows multiple entries to share the same key. Empty slots use a tag of 0,
    /// occupied slots use an odd hash fingerprint, and even non-zero tags represent tombstones.
    /// </remarks>
    public unsafe struct DynamicMultiDictionary<TKey, TValue, TEntry>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TEntry : unmanaged, IDynamicMultiDictionaryEntry<TKey, TValue>
    {
        private const uint TombstoneTag = 2u;
#if DYNAMIC_DICTIONARY_LOAD_60
        private const int MaxLoadFactorNumerator = 6;
#else
        private const int MaxLoadFactorNumerator = 7;
#endif
        private const int MaxLoadFactorDenominator = 10;
        private const int MaxTombstoneFactorNumerator = 2;
        private const int MaxTombstoneFactorDenominator = 10;

        private DynamicBuffer<TEntry> buffer;

        private struct Header
        {
            public int CountPlusOne;
            public int TombstonesPlusOne;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicMultiDictionary{TKey, TValue, TEntry}"/> struct.
        /// </summary>
        /// <param name="buffer"> The buffer whose length defines the table size. </param>
        public DynamicMultiDictionary(DynamicBuffer<TEntry> buffer)
        {
            buffer.CheckReadAccess();
            CheckCapacity(buffer.Length);
            CheckEntrySize();

            this.buffer = buffer;
        }

        /// <summary>
        /// Gets a value indicating whether the underlying buffer is created.
        /// </summary>
        public readonly bool IsCreated => this.buffer.IsCreated;

        /// <summary>
        /// Gets a value indicating whether the multi dictionary has no entries.
        /// </summary>
        public readonly bool IsEmpty => this.Count == 0;

        /// <summary>
        /// Gets the number of key-value pairs in the map.
        /// </summary>
        public readonly int Count
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.GetCount();
            }
        }

        /// <summary>
        /// Gets the fixed capacity for this multi dictionary.
        /// </summary>
        public readonly int Capacity
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.buffer.Length;
            }
        }

        /// <summary>
        /// Ensures the multi dictionary table has at least the requested capacity.
        /// </summary>
        /// <param name="capacity"> The minimum desired table capacity, rounded up to a power of two. </param>
        /// <remarks> The map only grows; smaller values have no effect. </remarks>
        public void EnsureCapacity(int capacity)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "EnsureCapacity requires a non-negative capacity");
            }

            if (capacity == 0)
            {
                return;
            }

            var currentCapacity = this.buffer.Length;
            if (capacity <= currentCapacity)
            {
                this.EnsureHeader(currentCapacity);
                return;
            }

            var newCapacity = math.ceilpow2(capacity);
            if (newCapacity <= 0)
            {
                throw new InvalidOperationException("DynamicMultiDictionary capacity overflow");
            }

            this.Resize(newCapacity);
        }

        /// <summary>
        /// Adds a key-value pair.
        /// </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="value"> The value to associate with the key. </param>
        /// <remarks> Duplicate keys and duplicate key-value pairs are allowed. </remarks>
        public void Add(TKey key, TValue value)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            while (true)
            {
                var capacity = this.buffer.Length;
                if (capacity == 0)
                {
                    this.Resize(1);
                    continue;
                }

                var headerSlotCreated = this.EnsureHeaderSlot(capacity);
                var entries = (TEntry*)this.buffer.GetUnsafePtr();
                ref var header = ref GetHeader(entries, capacity);
                if (headerSlotCreated)
                {
                    RebuildHeader(entries, capacity, ref header);
                }
                else
                {
                    EnsureHeaderValid(entries, capacity, ref header);
                }

                var count = GetCount(ref header);
                var tombstones = GetTombstones(ref header);

                if (ShouldRehash(tombstones, capacity))
                {
                    this.Rehash(capacity);
                    continue;
                }

                if (ShouldGrow(count + tombstones, capacity))
                {
                    this.Resize(GrowCapacity(capacity));
                    continue;
                }

                var mask = capacity - 1;
                var tag = ComputeTag(key, out var hash);
                var index = (int)(hash & (uint)mask);
                var firstTombstone = -1;

                for (var i = 0; i < capacity; i++)
                {
                    ref var entry = ref entries[index];
                    var entryTag = entry.Tag;

                    if (entryTag == 0)
                    {
                        if (firstTombstone != -1)
                        {
                            index = firstTombstone;
                            tombstones--;
                        }

                        ref var target = ref entries[index];
                        target.Tag = tag;
                        target.Key = key;
                        target.Value = value;
                        count++;
                        SetHeader(ref header, count, tombstones);
                        return;
                    }

                    if (IsTombstone(entryTag) && firstTombstone == -1)
                    {
                        firstTombstone = index;
                    }

                    index = (index + 1) & mask;
                }

                if (firstTombstone != -1)
                {
                    ref var target = ref entries[firstTombstone];
                    target.Tag = tag;
                    target.Key = key;
                    target.Value = value;
                    count++;
                    tombstones--;
                    SetHeader(ref header, count, tombstones);
                    return;
                }

                this.Resize(GrowCapacity(capacity));
            }
        }

        /// <summary>
        /// Adds a key-value pair when that exact pair is not already present.
        /// </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="value"> The value to add. The type should match <typeparamref name="TValue" />. </param>
        /// <typeparam name="T"> The value type used for equality. This should match <typeparamref name="TValue" />. </typeparam>
        /// <returns> True when the pair was added; false when the exact pair already exists. </returns>
        public bool TryAddUniquePair<T>(TKey key, T value)
            where T : unmanaged, IEquatable<TValue>
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            CheckValueSize<T>();

            if (this.Contains(key, value))
            {
                return false;
            }

            this.Add(key, UnsafeUtility.As<T, TValue>(ref value));
            return true;
        }

        /// <summary>
        /// Returns the first value associated with a key.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="value"> The first value for the key when found. </param>
        /// <param name="it"> The iterator to use with <see cref="TryGetNextValue" />. </param>
        /// <returns> True when at least one value exists for the key. </returns>
        public readonly bool TryGetFirstValue(TKey key, out TValue value, out HashMapIterator<TKey> it)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            it = default;
            it.Key = key;

            var capacity = this.buffer.Length;
            if (capacity == 0)
            {
                InvalidateIterator(ref it);
                value = default;
                return false;
            }

            var entries = (TEntry*)this.buffer.GetUnsafeReadOnlyPtr();
            var mask = capacity - 1;
            var tag = ComputeTag(key, out var hash);
            var start = (int)(hash & (uint)mask);
            var index = start;

            for (var i = 0; i < capacity; i++)
            {
                ref var entry = ref entries[index];
                var entryTag = entry.Tag;

                if (entryTag == 0)
                {
                    break;
                }

                if (entryTag == tag && entry.Key.Equals(key))
                {
                    SetIterator(ref it, start, index, mask);
                    value = entry.Value;
                    return true;
                }

                index = (index + 1) & mask;
            }

            InvalidateIterator(ref it);
            value = default;
            return false;
        }

        /// <summary>
        /// Advances an iterator to the next value associated with its key.
        /// </summary>
        /// <param name="value"> The next value when found. </param>
        /// <param name="it"> The iterator returned by <see cref="TryGetFirstValue" />. </param>
        /// <returns> True when another value exists for the iterator key. </returns>
        public readonly bool TryGetNextValue(out TValue value, ref HashMapIterator<TKey> it)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var capacity = this.buffer.Length;
            if (capacity == 0 || it.NextEntryIndex < 0)
            {
                InvalidateIterator(ref it);
                value = default;
                return false;
            }

            var entries = (TEntry*)this.buffer.GetUnsafeReadOnlyPtr();
            var mask = capacity - 1;
            var tag = ComputeTag(it.Key, out var hash);
            var start = (int)(hash & (uint)mask);
            var index = it.NextEntryIndex;

            while (index != start)
            {
                ref var entry = ref entries[index];
                var entryTag = entry.Tag;

                if (entryTag == 0)
                {
                    break;
                }

                if (entryTag == tag && entry.Key.Equals(it.Key))
                {
                    SetIterator(ref it, start, index, mask);
                    value = entry.Value;
                    return true;
                }

                index = (index + 1) & mask;
            }

            InvalidateIterator(ref it);
            value = default;
            return false;
        }

        /// <summary>
        /// Returns true if a given key is present in this map.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <returns> True when at least one value exists for the key. </returns>
        public readonly bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.TryGetFirstValue(key, out _, out _);
        }

        /// <summary>
        /// Returns true if a given key-value pair is present in this map.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="value"> The value to look up. The type should match <typeparamref name="TValue" />. </param>
        /// <typeparam name="T"> The value type used for equality. This should match <typeparamref name="TValue" />. </typeparam>
        /// <returns> True when the key-value pair exists. </returns>
        public readonly bool Contains<T>(TKey key, T value)
            where T : unmanaged, IEquatable<TValue>
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            CheckValueSize<T>();

            if (!this.TryGetFirstValue(key, out var item, out var it))
            {
                return false;
            }

            do
            {
                if (value.Equals(item))
                {
                    return true;
                }
            }
            while (this.TryGetNextValue(out item, ref it));

            return false;
        }

        /// <summary>
        /// Counts values associated with a key.
        /// </summary>
        /// <param name="key"> The key to count values for. </param>
        /// <returns> The number of values associated with the key. </returns>
        public readonly int CountValuesForKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var count = 0;
            if (!this.TryGetFirstValue(key, out _, out var it))
            {
                return count;
            }

            count++;
            while (this.TryGetNextValue(out _, ref it))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Removes all values associated with a key.
        /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <returns> The number of removed key-value pairs. </returns>
        public int Remove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var capacity = this.buffer.Length;
            if (capacity == 0)
            {
                return 0;
            }

            var headerSlotCreated = this.EnsureHeaderSlot(capacity);
            var entries = (TEntry*)this.buffer.GetUnsafePtr();
            ref var header = ref GetHeader(entries, capacity);
            if (headerSlotCreated)
            {
                RebuildHeader(entries, capacity, ref header);
            }
            else
            {
                EnsureHeaderValid(entries, capacity, ref header);
            }

            var count = GetCount(ref header);
            var tombstones = GetTombstones(ref header);
            var removed = 0;
            var mask = capacity - 1;
            var tag = ComputeTag(key, out var hash);
            var index = (int)(hash & (uint)mask);

            for (var i = 0; i < capacity; i++)
            {
                ref var entry = ref entries[index];
                var entryTag = entry.Tag;

                if (entryTag == 0)
                {
                    break;
                }

                if (entryTag == tag && entry.Key.Equals(key))
                {
                    entry = default;
                    entry.Tag = TombstoneTag;
                    count--;
                    tombstones++;
                    removed++;
                }

                index = (index + 1) & mask;
            }

            if (removed != 0)
            {
                SetHeader(ref header, count, tombstones);
            }

            return removed;
        }

        /// <summary>
        /// Removes one exact key-value pair.
        /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <param name="value"> The value to remove. The type should match <typeparamref name="TValue" />. </param>
        /// <typeparam name="T"> The value type used for equality. This should match <typeparamref name="TValue" />. </typeparam>
        /// <returns> True when a matching pair was removed. </returns>
        public bool Remove<T>(TKey key, T value)
            where T : unmanaged, IEquatable<TValue>
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            CheckValueSize<T>();

            if (!this.TryGetFirstValue(key, out var item, out var it))
            {
                return false;
            }

            do
            {
                if (!value.Equals(item))
                {
                    continue;
                }

                this.Remove(it);
                return true;
            }
            while (this.TryGetNextValue(out item, ref it));

            return false;
        }

        /// <summary>
        /// Removes a single key-value pair represented by an iterator.
        /// </summary>
        /// <param name="it"> The iterator representing the key-value pair to remove. </param>
        /// <exception cref="ArgumentException"> Thrown if the iterator is invalid. </exception>
        public void Remove(HashMapIterator<TKey> it)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var capacity = this.buffer.Length;
            if (capacity == 0 || (uint)it.EntryIndex >= (uint)capacity)
            {
                throw new ArgumentException("DynamicMultiDictionary iterator is invalid", nameof(it));
            }

            var headerSlotCreated = this.EnsureHeaderSlot(capacity);
            var entries = (TEntry*)this.buffer.GetUnsafePtr();
            ref var header = ref GetHeader(entries, capacity);
            if (headerSlotCreated)
            {
                RebuildHeader(entries, capacity, ref header);
            }
            else
            {
                EnsureHeaderValid(entries, capacity, ref header);
            }

            ref var entry = ref entries[it.EntryIndex];
            if (!IsOccupied(entry.Tag) || !entry.Key.Equals(it.Key))
            {
                throw new ArgumentException("DynamicMultiDictionary iterator is invalid", nameof(it));
            }

            entry = default;
            entry.Tag = TombstoneTag;
            SetHeader(ref header, GetCount(ref header) - 1, GetTombstones(ref header) + 1);
        }

        /// <summary>
        /// Clears the map by resetting all entry tags.
        /// </summary>
        /// <remarks> Does not resize the buffer. </remarks>
        public void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var capacity = this.buffer.Length;
            if (capacity == 0)
            {
                return;
            }

            this.EnsureHeaderSlot(capacity);
            var entries = (TEntry*)this.buffer.GetUnsafePtr();
            ClearEntries(entries, capacity);
            ref var header = ref GetHeader(entries, capacity);
            InitializeHeader(entries, capacity, ref header, 0, 0);
        }

        /// <summary>
        /// Rehashes the map after key values are remapped.
        /// </summary>
        /// <remarks>
        /// Call this after remapping keys (such as Entity or BlobAssetReference values) to rebuild tags and table positions.
        /// </remarks>
        public void ReconstructAfterRemap()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var capacity = this.buffer.Length;
            if (capacity == 0)
            {
                return;
            }

            this.EnsureHeaderSlot(capacity);
            var sizeOfEntry = UnsafeUtility.SizeOf<TEntry>();
            var entries = (TEntry*)this.buffer.GetUnsafePtr();
            var oldEntries = (TEntry*)UnsafeUtility.Malloc((long)sizeOfEntry * capacity, UnsafeUtility.AlignOf<TEntry>(), Allocator.Temp);

            UnsafeUtility.MemCpy(oldEntries, entries, (long)sizeOfEntry * capacity);
            ClearEntries(entries, capacity);
            var count = RehashEntries(oldEntries, capacity, entries, capacity, true);
            ref var header = ref GetHeader(entries, capacity);
            InitializeHeader(entries, capacity, ref header, count, 0);
            UnsafeUtility.Free(oldEntries, Allocator.Temp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ComputeTag(TKey key, out uint hash)
        {
#if DYNAMIC_DICTIONARY_HASH_MIX
            hash = MixHash((uint)key.GetHashCode());
#else
            hash = (uint)key.GetHashCode();
#endif
            return (hash << 1) | 1u;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MixHash(uint hash)
        {
            hash ^= hash >> 16;
            hash *= 0x7feb352d;
            hash ^= hash >> 15;
            hash *= 0x846ca68b;
            hash ^= hash >> 16;
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOccupied(uint tag)
        {
            return (tag & 1u) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTombstone(uint tag)
        {
            return (tag & 1u) == 0 && tag != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Header GetHeader(TEntry* entries, int capacity)
        {
            return ref UnsafeUtility.AsRef<Header>(entries + capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetCount(ref Header header)
        {
            return header.CountPlusOne - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetTombstones(ref Header header)
        {
            return header.TombstonesPlusOne - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetHeader(ref Header header, int count, int tombstones)
        {
            header.CountPlusOne = count + 1;
            header.TombstonesPlusOne = tombstones + 1;
        }

        private static void InitializeHeader(TEntry* entries, int capacity, ref Header header, int count, int tombstones)
        {
            UnsafeUtility.MemClear(entries + capacity, UnsafeUtility.SizeOf<TEntry>());
            SetHeader(ref header, count, tombstones);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsHeaderValid(ref Header header, int capacity)
        {
            var count = header.CountPlusOne - 1;
            var tombstones = header.TombstonesPlusOne - 1;
            return header.CountPlusOne > 0 && header.TombstonesPlusOne > 0 && count >= 0 && tombstones >= 0 && count + tombstones <= capacity;
        }

        private static void EnsureHeaderValid(TEntry* entries, int capacity, ref Header header)
        {
            if (IsHeaderValid(ref header, capacity))
            {
                return;
            }

            RebuildHeader(entries, capacity, ref header);
        }

        private static void RebuildHeader(TEntry* entries, int capacity, ref Header header)
        {
            var count = 0;
            var tombstones = 0;

            for (var i = 0; i < capacity; i++)
            {
                var tag = entries[i].Tag;
                if (IsOccupied(tag))
                {
                    count++;
                }
                else if (IsTombstone(tag))
                {
                    tombstones++;
                }
            }

            InitializeHeader(entries, capacity, ref header, count, tombstones);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldGrow(int filledSlots, int capacity)
        {
            return (long)filledSlots * MaxLoadFactorDenominator >= (long)capacity * MaxLoadFactorNumerator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldRehash(int tombstones, int capacity)
        {
            return (long)tombstones * MaxTombstoneFactorDenominator > (long)capacity * MaxTombstoneFactorNumerator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InsertEntry(ref TEntry entry, TEntry* entries, int mask, uint hash)
        {
            var index = (int)(hash & (uint)mask);

            while (true)
            {
                ref var target = ref entries[index];
                if (target.Tag == 0)
                {
                    entries[index] = entry;
                    return;
                }

                index = (index + 1) & mask;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GrowCapacity(int capacity)
        {
            if (capacity == 0)
            {
                return 1;
            }

            var newCapacity = capacity << 1;
            if (newCapacity <= 0)
            {
                throw new InvalidOperationException("DynamicMultiDictionary capacity overflow");
            }

            return newCapacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InvalidateIterator(ref HashMapIterator<TKey> it)
        {
            it.EntryIndex = -1;
            it.NextEntryIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetIterator(ref HashMapIterator<TKey> it, int start, int index, int mask)
        {
            it.EntryIndex = index;

            var nextIndex = (index + 1) & mask;
            it.NextEntryIndex = nextIndex == start ? -1 : nextIndex;
        }

        private readonly int GetCount()
        {
            var capacity = this.buffer.Length;
            if (capacity == 0)
            {
                return 0;
            }

            var entries = (TEntry*)this.buffer.GetUnsafeReadOnlyPtr();
            if (this.buffer.Capacity > capacity)
            {
                ref var header = ref GetHeader(entries, capacity);
                if (IsHeaderValid(ref header, capacity))
                {
                    return GetCount(ref header);
                }
            }

            var count = 0;
            for (var i = 0; i < capacity; i++)
            {
                if (IsOccupied(entries[i].Tag))
                {
                    count++;
                }
            }

            return count;
        }

        private void Resize(int newCapacity)
        {
            CheckCapacity(newCapacity);
            CheckEntrySize();

            var oldCapacity = this.buffer.Length;
            if (oldCapacity == newCapacity)
            {
                this.EnsureHeader(newCapacity);
                return;
            }

            TEntry* oldEntries = null;
            if (oldCapacity > 0)
            {
                var sizeOfEntry = UnsafeUtility.SizeOf<TEntry>();
                oldEntries = (TEntry*)UnsafeUtility.Malloc((long)sizeOfEntry * oldCapacity, UnsafeUtility.AlignOf<TEntry>(), Allocator.Temp);
                UnsafeUtility.MemCpy(oldEntries, this.buffer.GetUnsafePtr(), (long)sizeOfEntry * oldCapacity);
                this.buffer.Clear();
            }

            if (newCapacity == 0)
            {
                return;
            }

            this.buffer.ResizeUninitialized(newCapacity);
            this.EnsureHeaderSlot(newCapacity);

            var entries = (TEntry*)this.buffer.GetUnsafePtr();
            ClearEntries(entries, newCapacity);
            ref var header = ref GetHeader(entries, newCapacity);
            InitializeHeader(entries, newCapacity, ref header, 0, 0);

            if (oldCapacity > 0)
            {
                var count = RehashEntries(oldEntries, oldCapacity, entries, newCapacity, false);
                SetHeader(ref header, count, 0);
                UnsafeUtility.Free(oldEntries, Allocator.Temp);
            }
        }

        private static int RehashEntries(TEntry* oldEntries, int oldCapacity, TEntry* entries, int capacity, bool recomputeTags)
        {
            var mask = capacity - 1;
            var count = 0;

            for (var i = 0; i < oldCapacity; i++)
            {
                var entry = oldEntries[i];
                var entryTag = entry.Tag;

                if (!IsOccupied(entryTag))
                {
                    continue;
                }

                uint hash;
                if (recomputeTags)
                {
                    entry.Tag = ComputeTag(entry.Key, out hash);
                }
                else
                {
                    hash = entryTag >> 1;
                }

                InsertEntry(ref entry, entries, mask, hash);
                count++;
            }

            return count;
        }

        private void Rehash(int capacity)
        {
            this.EnsureHeaderSlot(capacity);
            var sizeOfEntry = UnsafeUtility.SizeOf<TEntry>();
            var entries = (TEntry*)this.buffer.GetUnsafePtr();
            var oldEntries = (TEntry*)UnsafeUtility.Malloc((long)sizeOfEntry * capacity, UnsafeUtility.AlignOf<TEntry>(), Allocator.Temp);

            UnsafeUtility.MemCpy(oldEntries, entries, (long)sizeOfEntry * capacity);
            ClearEntries(entries, capacity);
            var count = RehashEntries(oldEntries, capacity, entries, capacity, false);
            ref var header = ref GetHeader(entries, capacity);
            InitializeHeader(entries, capacity, ref header, count, 0);
            UnsafeUtility.Free(oldEntries, Allocator.Temp);
        }

        private bool EnsureHeaderSlot(int capacity)
        {
            if (capacity == 0)
            {
                return false;
            }

            if (this.buffer.Capacity > capacity)
            {
                return false;
            }

            this.buffer.EnsureCapacity(capacity + 1);
            return true;
        }

        private void EnsureHeader(int capacity)
        {
            if (capacity == 0)
            {
                return;
            }

            var headerSlotCreated = this.EnsureHeaderSlot(capacity);
            var entries = (TEntry*)this.buffer.GetUnsafePtr();
            ref var header = ref GetHeader(entries, capacity);
            if (headerSlotCreated)
            {
                RebuildHeader(entries, capacity, ref header);
            }
            else
            {
                EnsureHeaderValid(entries, capacity, ref header);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            if (this.buffer.Length > this.buffer.Capacity)
            {
                throw new InvalidOperationException("DynamicMultiDictionary buffer length exceeds capacity");
            }

            CheckCapacity(this.buffer.Length);
            CheckEntrySize();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckCapacity(int capacity)
        {
            if (capacity <= 1)
            {
                return;
            }

            if ((capacity & (capacity - 1)) != 0)
            {
                throw new InvalidOperationException("DynamicMultiDictionary requires a power-of-two capacity");
            }
        }

        private static void CheckEntrySize()
        {
            if (UnsafeUtility.SizeOf<TEntry>() < UnsafeUtility.SizeOf<Header>())
            {
                throw new InvalidOperationException("DynamicMultiDictionary entry size too small to store header data");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckValueSize<T>()
            where T : unmanaged
        {
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<TValue>())
            {
                throw new InvalidOperationException("DynamicMultiDictionary exact-pair operation value type must match the map value type size.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClearEntries(TEntry* entries, int capacity)
        {
            if (capacity == 0)
            {
                return;
            }

            UnsafeUtility.MemClear(entries, (long)UnsafeUtility.SizeOf<TEntry>() * capacity);
        }
    }

    /// <summary>
    /// Extension helpers for building and accessing entry-backed dynamic dictionary buffers.
    /// </summary>
    public static class DynamicMultiDictionaryExtensions
    {
        /// <summary>
        /// Creates a multi-dictionary wrapper for an initialized entry-backed buffer.
        /// </summary>
        /// <param name="buffer"> The initialized buffer to wrap. </param>
        /// <returns> A multi-dictionary wrapper for the buffer. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicMultiDictionary<TKey, TValue, TEntry> AsDynamicMultiDictionary<TKey, TValue, TEntry>(this DynamicBuffer<TEntry> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where TEntry : unmanaged, IDynamicMultiDictionaryEntry<TKey, TValue>
        {
            return new DynamicMultiDictionary<TKey, TValue, TEntry>(buffer);
        }
    }
}
