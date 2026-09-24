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
    /// Entries must be unmanaged. Tag zero means empty, odd tags contain hash fingerprints, and nonzero even tags are tombstones.
    /// </summary>
    public interface IDynamicHashSetEntry<TKey> : IBufferElementData
        where TKey : unmanaged, IEquatable<TKey>
    {
        uint Tag { get; set; }

        TKey Key { get; set; }
    }

    /// <summary>
    /// Buffer length is the power-of-two table capacity; an extra capacity-only slot stores the header.
    /// </summary>
    public unsafe struct DynamicHashSet<TKey, TEntry>
        where TKey : unmanaged, IEquatable<TKey>
        where TEntry : unmanaged, IDynamicHashSetEntry<TKey>
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

        private DynamicBuffer<TEntry> _buffer;

        private struct Header
        {
            public int CountPlusOne;
            public int TombstonesPlusOne;
        }

        public DynamicHashSet(DynamicBuffer<TEntry> buffer)
        {
            buffer.CheckReadAccess();
            CheckCapacity(buffer.Length);
            CheckEntrySize();

            _buffer = buffer;
        }

        public readonly bool IsCreated => _buffer.IsCreated;

        public readonly bool IsEmpty => Count == 0;

        public readonly int Count
        {
            get
            {
                _buffer.CheckReadAccess();
                RefCheck();
                return GetCount();
            }
        }

        public int Capacity
        {
            get
            {
                _buffer.CheckReadAccess();
                RefCheck();
                return _buffer.Length;
            }
        }

        public readonly bool Contains(TKey key)
        {
            _buffer.CheckReadAccess();
            RefCheck();

            var capacity = _buffer.Length;
            if (capacity == 0)
            {
                return false;
            }

            var entries = (TEntry*)_buffer.GetUnsafeReadOnlyPtr();
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
                    return true;
                }

                index = (index + 1) & mask;
            }

            return false;
        }

        public void EnsureCapacity(int capacity)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "EnsureCapacity requires a non-negative capacity");
            }

            if (capacity == 0)
            {
                return;
            }

            var currentCapacity = _buffer.Length;
            if (capacity <= currentCapacity)
            {
                EnsureHeader(currentCapacity);
                return;
            }

            var newCapacity = math.ceilpow2(capacity);
            if (newCapacity <= 0)
            {
                throw new InvalidOperationException("DynamicHashSet capacity overflow");
            }

            Resize(newCapacity);
        }

        public bool TryAdd(TKey key)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            while (true)
            {
                var capacity = _buffer.Length;
                if (capacity == 0)
                {
                    Resize(1);
                    continue;
                }

                var headerSlotCreated = EnsureHeaderSlot(capacity);
                var entries = (TEntry*)_buffer.GetUnsafePtr();
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
                    Rehash(capacity);
                    continue;
                }

                if (ShouldGrow(count + tombstones, capacity))
                {
                    Resize(GrowCapacity(capacity));
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
                    count++;
                    tombstones--;
                    SetHeader(ref header, count, tombstones);
                    return true;
                }

                Resize(GrowCapacity(capacity));
            }
        }

        public void Add(TKey key)
        {
            if (!TryAdd(key))
            {
                throw new ArgumentException($"An item with the same key has already been added. Key: {key}", nameof(key));
            }
        }

        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            _buffer.CheckReadAccess();
            RefCheck();

            var result = CollectionHelper.CreateNativeArray<TKey>(GetCount(), allocator, NativeArrayOptions.UninitializedMemory);
            CopyKeys(result);
            return result;
        }

        public bool TryRemove(TKey key)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var capacity = _buffer.Length;
            if (capacity == 0)
            {
                return false;
            }

            var headerSlotCreated = EnsureHeaderSlot(capacity);
            var entries = (TEntry*)_buffer.GetUnsafePtr();
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

        public bool Remove(TKey key)
        {
            return TryRemove(key);
        }

        public void Clear()
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var capacity = _buffer.Length;
            if (capacity == 0)
            {
                return;
            }

            EnsureHeaderSlot(capacity);
            var entries = (TEntry*)_buffer.GetUnsafePtr();
            ClearEntries(entries, capacity);
            ref var header = ref GetHeader(entries, capacity);
            InitializeHeader(entries, capacity, ref header, 0, 0);
        }

        /// <summary>
        /// Call after remapping keys such as Entity or BlobAssetReference to rebuild hash positions.
        /// </summary>
        public void ReconstructAfterRemap()
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var capacity = _buffer.Length;
            if (capacity == 0)
            {
                return;
            }

            EnsureHeaderSlot(capacity);
            var sizeOfEntry = UnsafeUtility.SizeOf<TEntry>();
            var entries = (TEntry*)_buffer.GetUnsafePtr();
            var oldEntries = (TEntry*)UnsafeUtility.Malloc(
                (long)sizeOfEntry * capacity,
                UnsafeUtility.AlignOf<TEntry>(),
                Allocator.Temp);

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
            // Capacity-only memory may be uninitialized after a buffer copy; validate without overflowing its contents.
            return header.CountPlusOne > 0 && header.TombstonesPlusOne > 0 && count >= 0 && tombstones >= 0 && (long)count + tombstones <= capacity;
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
                throw new InvalidOperationException("DynamicHashSet capacity overflow");
            }

            return newCapacity;
        }

        private readonly int GetCount()
        {
            var capacity = _buffer.Length;
            if (capacity == 0)
            {
                return 0;
            }

            var entries = (TEntry*)_buffer.GetUnsafeReadOnlyPtr();
            if (_buffer.Capacity > capacity)
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

        private readonly void CopyKeys(NativeArray<TKey> keys)
        {
            var capacity = _buffer.Length;
            if (capacity == 0 || keys.Length == 0)
            {
                return;
            }

            var entries = (TEntry*)_buffer.GetUnsafeReadOnlyPtr();
            for (int i = 0, count = 0; i < capacity && count < keys.Length; i++)
            {
                ref var entry = ref entries[i];
                if (!IsOccupied(entry.Tag))
                {
                    continue;
                }

                keys[count++] = entry.Key;
            }
        }

        private void Resize(int newCapacity)
        {
            CheckCapacity(newCapacity);
            CheckEntrySize();

            var oldCapacity = _buffer.Length;
            if (oldCapacity == newCapacity)
            {
                EnsureHeader(newCapacity);
                return;
            }

            TEntry* oldEntries = null;
            if (oldCapacity > 0)
            {
                var sizeOfEntry = UnsafeUtility.SizeOf<TEntry>();
                oldEntries = (TEntry*)UnsafeUtility.Malloc(
                    (long)sizeOfEntry * oldCapacity,
                    UnsafeUtility.AlignOf<TEntry>(),
                    Allocator.Temp);
                UnsafeUtility.MemCpy(oldEntries, _buffer.GetUnsafePtr(), (long)sizeOfEntry * oldCapacity);
                _buffer.Clear();
            }

            if (newCapacity == 0)
            {
                return;
            }

            _buffer.ResizeUninitialized(newCapacity);
            EnsureHeaderSlot(newCapacity);

            var entries = (TEntry*)_buffer.GetUnsafePtr();
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
            EnsureHeaderSlot(capacity);
            var sizeOfEntry = UnsafeUtility.SizeOf<TEntry>();
            var entries = (TEntry*)_buffer.GetUnsafePtr();
            var oldEntries = (TEntry*)UnsafeUtility.Malloc(
                (long)sizeOfEntry * capacity,
                UnsafeUtility.AlignOf<TEntry>(),
                Allocator.Temp);

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

            if (_buffer.Capacity > capacity)
            {
                return false;
            }

            _buffer.EnsureCapacity(capacity + 1);
            return true;
        }

        private void EnsureHeader(int capacity)
        {
            if (capacity == 0)
            {
                return;
            }

            var headerSlotCreated = EnsureHeaderSlot(capacity);
            var entries = (TEntry*)_buffer.GetUnsafePtr();
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
            if (_buffer.Length > _buffer.Capacity)
            {
                throw new InvalidOperationException("DynamicHashSet buffer length exceeds capacity");
            }

            CheckCapacity(_buffer.Length);
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
                throw new InvalidOperationException("DynamicHashSet requires a power-of-two capacity");
            }
        }

        private static void CheckEntrySize()
        {
            if (UnsafeUtility.SizeOf<TEntry>() < UnsafeUtility.SizeOf<Header>())
            {
                throw new InvalidOperationException("DynamicHashSet entry size too small to store header data");
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

    public static class DynamicHashSetExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicHashSet<TKey, TEntry> AsDynamicHashSet<TKey, TEntry>(this DynamicBuffer<TEntry> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TEntry : unmanaged, IDynamicHashSetEntry<TKey>
        {
            return new DynamicHashSet<TKey, TEntry>(buffer);
        }
    }
}
