// <copyright file="AosHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

// #define AOS_HASHMAP_LOAD_60

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
    /// Describes an AOS entry stored in an <see cref="AosHashMap{TKey, TValue, TEntry}" /> buffer.
    /// </summary>
    /// <remarks>
    /// Implementations should keep the struct unmanaged and use 0 in <see cref="Tag" /> to represent empty slots.
    /// Odd tag values store hash fingerprints used to accelerate key comparisons, while even non-zero tags represent tombstones.
    /// </remarks>
    public interface IAosHashMapEntry<TKey, TValue> : IBufferElementData
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        uint Tag { get; set; }

        TKey Key { get; set; }

        TValue Value { get; set; }
    }

    /// <summary>
    /// AOS hash map backed directly by a <see cref="DynamicBuffer{T}" /> with a reserved capacity-only header slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The map stores entries directly in the dynamic buffer and reserves one extra capacity slot used as a header.
    /// Each element's <see cref="IAosHashMapEntry{TKey, TValue}.Tag" /> is 0 for empty slots and otherwise
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
    public unsafe struct AosHashMap<TKey, TValue, TEntry>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TEntry : unmanaged, IAosHashMapEntry<TKey, TValue>
    {
        private const uint TombstoneTag = 2u;
#if AOS_HASHMAP_LOAD_60
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
        /// Initializes a new instance of the <see cref="AosHashMap{TKey, TValue, TEntry}"/> struct.
        /// </summary>
        /// <param name="buffer"> The buffer whose length defines the table size. </param>
        public AosHashMap(DynamicBuffer<TEntry> buffer)
        {
            buffer.CheckReadAccess();
            CheckCapacity(buffer.Length);
            CheckEntrySize();

            this.buffer = buffer;
        }

        /// <summary>
        /// Gets a value indicating whether the underlying buffer is created.
        /// </summary>
        public bool IsCreated => this.buffer.IsCreated;

        /// <summary>
        /// Gets the fixed capacity for this map.
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
        /// Ensures the map can hold at least the requested capacity.
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
                throw new InvalidOperationException("AosHashMap capacity overflow");
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
                        return false;
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
        /// Tries to retrieve a value for the given key.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="value"> The value for the key when found. </param>
        /// <returns> True when the key exists in the map. </returns>
        public bool TryGetValue(TKey key, out TValue value)
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
#if AOS_HASH_MIX
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
                throw new InvalidOperationException("AosHashMap capacity overflow");
            }

            return newCapacity;
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
                throw new InvalidOperationException("AosHashMap buffer length exceeds capacity");
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
                throw new InvalidOperationException("AosHashMap requires a power-of-two capacity");
            }
        }

        private static void CheckEntrySize()
        {
            if (UnsafeUtility.SizeOf<TEntry>() < UnsafeUtility.SizeOf<Header>())
            {
                throw new InvalidOperationException("AosHashMap entry size too small to store header data");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ClearEntries(TEntry* entries, int capacity)
        {
            if (capacity == 0)
            {
                return;
            }

            UnsafeUtility.MemClear(entries, (long)UnsafeUtility.SizeOf<TEntry>() * capacity);
        }
    }

    /// <summary>
    /// Extension helpers for building and accessing AOS hash map buffers.
    /// </summary>
    public static unsafe class AosHashMapExtensions
    {
        /// <summary>
        /// Creates a map wrapper for an initialized AOS hash map buffer.
        /// </summary>
        /// <param name="buffer"> The initialized buffer to wrap. </param>
        /// <returns> A map wrapper for the buffer. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AosHashMap<TKey, TValue, TEntry> AsAosHashMap<TKey, TValue, TEntry>(this DynamicBuffer<TEntry> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where TEntry : unmanaged, IAosHashMapEntry<TKey, TValue>
        {
            return new AosHashMap<TKey, TValue, TEntry>(buffer);
        }
    }
}
