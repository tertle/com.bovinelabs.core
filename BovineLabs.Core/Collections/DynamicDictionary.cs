// <copyright file="DynamicDictionary.cs" company="BovineLabs">
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
    /// Describes an entry stored in a <see cref="DynamicDictionary{TKey, TValue, TEntry}" /> buffer.
    /// </summary>
    /// <remarks>
    /// Implementations should keep the struct unmanaged and use 0 in <see cref="Tag" /> to represent empty slots.
    /// Odd tag values store hash fingerprints used to accelerate key comparisons, while even non-zero tags represent tombstones.
    /// </remarks>
    public interface IDynamicDictionaryEntry<TKey, TValue> : IBufferElementData
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        uint Tag { get; set; }

        TKey Key { get; set; }

        TValue Value { get; set; }
    }

    /// <summary>
    /// Entry-backed dictionary stored directly in a <see cref="DynamicBuffer{T}" /> with a reserved capacity-only header slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The dictionary stores entries directly in the dynamic buffer and reserves one extra capacity slot used as a header.
    /// Each element's <see cref="IDynamicDictionaryEntry{TKey, TValue}.Tag" /> is 0 for empty slots and otherwise
    /// stores a hash fingerprint (<c>hash << 1 | 1</c>) so that 0 is reserved; even, non-zero tags represent
    /// tombstones.
    /// </para>
    /// <para>
    /// Capacity equals the buffer length and must be a power of two; indices are computed with
    /// <c>hash & (capacity - 1)</c> and collisions resolve via linear probing. <see cref="TryAdd" /> scans for
    /// an empty slot or matching key, resizing or rehashing when load-factor thresholds are exceeded.
    /// </para>
    /// <para>
    /// Removal marks tombstones to keep probe chains intact, while <see cref="Clear" /> resets tags and
    /// header counters. <see cref="Resize" /> and <see cref="ReconstructAfterRemap" /> copy entries to
    /// temporary storage, clear tags, and reinsert entries with either stored or recomputed tags.
    /// </para>
    /// </remarks>
    public unsafe struct DynamicDictionary<TKey, TValue, TEntry>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TEntry : unmanaged, IDynamicDictionaryEntry<TKey, TValue>
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
        /// Initializes a new instance of the <see cref="DynamicDictionary{TKey, TValue, TEntry}"/> struct.
        /// </summary>
        /// <param name="buffer"> The buffer whose length defines the table size. </param>
        public DynamicDictionary(DynamicBuffer<TEntry> buffer)
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
        /// Gets a value indicating whether the dictionary has no entries.
        /// </summary>
        public readonly bool IsEmpty => this.Count == 0;

        /// <summary>
        /// Gets the number of key-value pairs in the dictionary.
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
        /// Gets or sets the value for the specified key.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <value> The value associated with the key. </value>
        /// <remarks> Setting the value updates an existing key or adds it when it is not present. </remarks>
        /// <exception cref="ArgumentException"> Thrown by the getter if the key was not present. </exception>
        public TValue this[TKey key]
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();

                if (this.TryGetValue(key, out var value))
                {
                    return value;
                }

                throw new ArgumentException($"Key not present in DynamicDictionary: {key}", nameof(key));
            }

            set => this.TryAddOrSet(key, value, true);
        }

        /// <summary>
        /// Gets the fixed capacity for this dictionary.
        /// </summary>
        public int Capacity
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.buffer.Length;
            }
        }

        /// <summary>
        /// Returns whether the dictionary contains the specified key.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <returns> True when the key exists in the map. </returns>
        public readonly bool ContainsKey(TKey key)
        {
            return this.TryGetValue(key, out _);
        }

        /// <summary>
        /// Ensures the dictionary can hold at least the requested capacity.
        /// </summary>
        /// <param name="capacity"> The minimum desired capacity (rounded up to power-of-two). </param>
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
                throw new InvalidOperationException("DynamicDictionary capacity overflow");
            }

            this.Resize(newCapacity);
        }

        /// <summary>
        /// Adds a key/value pair if the key does not already exist.
        /// </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="value"> The value to associate with the key. </param>
        /// <returns> True when the entry is inserted, false if the key exists. </returns>
        /// <remarks> Automatically grows or rehashes when load-factor thresholds are exceeded. </remarks>
        public bool TryAdd(TKey key, TValue value)
        {
            return this.TryAddOrSet(key, value, false);
        }

        private bool TryAddOrSet(TKey key, TValue value, bool setExisting)
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
                        }

                        ref var target = ref entries[index];
                        target.Tag = tag;
                        target.Key = key;
                        target.Value = value;
                        count++;
                        if (firstTombstone != -1)
                        {
                            tombstones--;
                        }

                        SetHeader(ref header, count, tombstones);
                        return true;
                    }

                    if (IsTombstone(entryTag))
                    {
                        if (firstTombstone == -1)
                        {
                            firstTombstone = index;
                        }
                    }
                    else if (entryTag == tag && entry.Key.Equals(key))
                    {
                        if (!setExisting)
                        {
                            return false;
                        }

                        entry.Value = value;
                        return true;
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
                    return true;
                }

                this.Resize(GrowCapacity(capacity));
            }
        }

        /// <summary>
        /// Adds a key/value pair.
        /// </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="value"> The value to associate with the key. </param>
        /// <exception cref="ArgumentException"> Thrown if the key already exists. </exception>
        public void Add(TKey key, TValue value)
        {
            if (!this.TryAdd(key, value))
            {
                throw new ArgumentException($"An item with the same key has already been added. Key: {key}", nameof(key));
            }
        }

        /// <summary>
        /// Tries to retrieve a value for the given key.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="value"> The value for the key when found. </param>
        /// <returns> True when the key exists in the map. </returns>
        public readonly bool TryGetValue(TKey key, out TValue value)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var capacity = this.buffer.Length;
            if (capacity == 0)
            {
                value = default;
                return false;
            }

            var entries = (TEntry*)this.buffer.GetUnsafeReadOnlyPtr();
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
                    value = entry.Value;
                    return true;
                }

                index = (index + 1) & mask;
            }

            value = default;
            return false;
        }

        /// <summary> Returns an array with a copy of all keys in no particular order. </summary>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> An array with a copy of all keys in the dictionary. </returns>
        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var result = CollectionHelper.CreateNativeArray<TKey>(this.GetCount(), allocator, NativeArrayOptions.UninitializedMemory);
            this.CopyKeysAndValues(result, default);
            return result;
        }

        /// <summary> Returns an array with a copy of all values in no particular order. </summary>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> An array with a copy of all values in the dictionary. </returns>
        public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var result = CollectionHelper.CreateNativeArray<TValue>(this.GetCount(), allocator, NativeArrayOptions.UninitializedMemory);
            this.CopyKeysAndValues(default, result);
            return result;
        }

        /// <summary> Returns arrays with a copy of all key-value pairs in no particular order. </summary>
        /// <remarks> For all indices, <c>Values[i]</c> is the value associated with <c>Keys[i]</c>. </remarks>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> A <see cref="NativeKeyValueArrays{TKey,TValue}" /> with a copy of all key-value pairs. </returns>
        public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var result = new NativeKeyValueArrays<TKey, TValue>(this.GetCount(), allocator, NativeArrayOptions.UninitializedMemory);
            this.CopyKeysAndValues(result.Keys, result.Values);
            return result;
        }

        /// <summary>
        /// Removes a key/value pair if present.
        /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <returns> True when the entry is removed. </returns>
        /// <remarks> Uses tombstones to keep probe chains intact. </remarks>
        public bool TryRemove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var capacity = this.buffer.Length;
            if (capacity == 0)
            {
                return false;
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
            var mask = capacity - 1;
            var tag = ComputeTag(key, out var hash);
            var index = (int)(hash & (uint)mask);

            for (var i = 0; i < capacity; i++)
            {
                ref var entry = ref entries[index];
                var entryTag = entry.Tag;

                if (entryTag == 0)
                {
                    return false;
                }

                if (entryTag == tag && entry.Key.Equals(key))
                {
                    entry = default;
                    entry.Tag = TombstoneTag;
                    count--;
                    tombstones++;
                    SetHeader(ref header, count, tombstones);
                    return true;
                }

                index = (index + 1) & mask;
            }

            return false;
        }

        /// <summary>
        /// Removes a key/value pair if present.
        /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <returns> True when the entry is removed. </returns>
        public bool Remove(TKey key)
        {
            return this.TryRemove(key);
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
                throw new InvalidOperationException("DynamicDictionary capacity overflow");
            }

            return newCapacity;
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

        private readonly void CopyKeysAndValues(NativeArray<TKey> keys, NativeArray<TValue> values)
        {
            var copyKeys = keys.IsCreated;
            var copyValues = values.IsCreated;
            var max = copyKeys ? keys.Length : values.Length;
            var capacity = this.buffer.Length;
            if (capacity == 0 || max == 0)
            {
                return;
            }

            var entries = (TEntry*)this.buffer.GetUnsafeReadOnlyPtr();
            for (int i = 0, count = 0; i < capacity && count < max; i++)
            {
                ref var entry = ref entries[i];
                if (!IsOccupied(entry.Tag))
                {
                    continue;
                }

                if (copyKeys)
                {
                    keys[count] = entry.Key;
                }

                if (copyValues)
                {
                    values[count] = entry.Value;
                }

                count++;
            }
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
                throw new InvalidOperationException("DynamicDictionary buffer length exceeds capacity");
            }

            CheckCapacity(this.buffer.Length);
            CheckEntrySize();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        internal static void CheckCapacity(int capacity)
        {
            if (capacity <= 1)
            {
                return;
            }

            if ((capacity & (capacity - 1)) != 0)
            {
                throw new InvalidOperationException("DynamicDictionary requires a power-of-two capacity");
            }
        }

        private static void CheckEntrySize()
        {
            if (UnsafeUtility.SizeOf<TEntry>() < UnsafeUtility.SizeOf<Header>())
            {
                throw new InvalidOperationException("DynamicDictionary entry size too small to store header data");
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
    public static class DynamicDictionaryExtensions
    {
        /// <summary>
        /// Creates a dictionary wrapper for an initialized entry-backed buffer.
        /// </summary>
        /// <param name="buffer"> The initialized buffer to wrap. </param>
        /// <returns> A dictionary wrapper for the buffer. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicDictionary<TKey, TValue, TEntry> AsDynamicDictionary<TKey, TValue, TEntry>(this DynamicBuffer<TEntry> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where TEntry : unmanaged, IDynamicDictionaryEntry<TKey, TValue>
        {
            return new DynamicDictionary<TKey, TValue, TEntry>(buffer);
        }

        /// <summary> Checks whether an entry accessed through its interface is occupied. </summary>
        /// <param name="buffer"> The entry to inspect. </param>
        /// <returns> <see langword="true" /> if the entry is occupied; otherwise, <see langword="false" />. </returns>
        /// <remarks> This overload may box value-type entries and is not Burst-compatible. </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOccupied<TKey, TValue>(this IDynamicDictionaryEntry<TKey, TValue> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return (buffer.Tag & 1u) != 0;
        }

        /// <summary> Checks whether a generic unmanaged entry is occupied without boxing it. </summary>
        /// <param name="buffer"> The entry to inspect. </param>
        /// <returns> <see langword="true" /> if the entry is occupied; otherwise, <see langword="false" />. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOccupied<TKey, TValue, TEntry>(this TEntry buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where TEntry : unmanaged, IDynamicDictionaryEntry<TKey, TValue>
        {
            return (buffer.Tag & 1u) != 0;
        }
    }
}
