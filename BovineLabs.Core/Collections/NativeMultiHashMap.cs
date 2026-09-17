// ReSharper disable once CheckNamespace

namespace Unity.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    /// <summary>
    /// Parallel writes are unsupported; use NativeParallelMultiHashMap instead.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerTypeProxy(typeof(NativeMultiHashMapDebuggerTypeProxy<,>))]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Exposed for others")]
    public unsafe struct NativeMultiHashMap<TKey, TValue> : INativeDisposable, IEnumerable<KVPair<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal HashMapHelper<TKey>* data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeMultiHashMap<TKey, TValue>>();
#endif

        public NativeMultiHashMap(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            this.data = HashMapHelper<TKey>.Alloc(initialCapacity, sizeof(TValue), 256, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<TKey>() || UnsafeUtility.IsNativeContainerType<TValue>())
            {
                AtomicSafetyHandle.SetNestedContainer(this.m_Safety, true);
            }

            CollectionHelper.SetStaticSafetyId<NativeMultiHashMap<TKey, TValue>>(ref this.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(this.m_Safety, true);
#endif
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data != null && this.data->IsCreated;
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!this.IsCreated)
                {
                    return true;
                }

                this.CheckRead();
                return this.data->IsEmpty;
            }
        }

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.CheckRead();
                return this.data->Count;
            }
        }

        /// <summary>
        /// Capacity cannot shrink.
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                this.CheckRead();
                return this.data->Capacity;
            }

            set
            {
                this.CheckWrite();
                this.data->Resize(value);
            }
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!AtomicSafetyHandle.IsDefaultValue(this.m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(this.m_Safety);
            }
#endif
            if (!this.IsCreated)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref this.m_Safety);
#endif

            HashMapHelper<TKey>.Free(this.data);
            this.data = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!AtomicSafetyHandle.IsDefaultValue(this.m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(this.m_Safety);
            }
#endif
            if (!this.IsCreated)
            {
                return inputDeps;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var jobHandle = new NativeHashMapDisposeJob
            {
                Data = new NativeHashMapDispose
                {
                    m_HashMapData = (UnsafeHashMap<int, int>*)this.data,
                    m_Safety = this.m_Safety,
                },
            }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(this.m_Safety);
#else
            var jobHandle =
                new NativeHashMapDisposeJob { Data = new NativeHashMapDispose { m_HashMapData = (UnsafeHashMap<int, int>*)this.data } }.Schedule(inputDeps);
#endif
            this.data = null;

            return jobHandle;
        }

        public void Clear()
        {
            this.CheckWrite();
            this.data->Clear();
        }

        public void Add(TKey key, TValue item)
        {
            this.CheckWrite();

            var idx = this.data->AddNoFind(key);
            UnsafeUtility.WriteArrayElement(this.data->Ptr, idx, item);
        }

        public int Remove(TKey key)
        {
            this.CheckWrite();
            return this.data->Remove(key);
        }

        public readonly bool TryGetFirstValue(TKey key, out TValue item, out HashMapIterator<TKey> it)
        {
            this.CheckRead();
            return this.data->TryGetFirstValue(key, out item, out it);
        }

        public readonly bool TryGetNextValue(out TValue item, ref HashMapIterator<TKey> it)
        {
            this.CheckRead();
            return this.data->TryGetNextValue(out item, ref it);
        }

        public readonly bool ContainsKey(TKey key)
        {
            this.CheckRead();
            return this.data->Find(key) != -1;
        }

        public void TrimExcess()
        {
            this.CheckWrite();
            this.data->TrimExcess();
        }

        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.CheckRead();
            return this.data->GetKeyArray(allocator);
        }

        public readonly void GetKeyArray(NativeList<TKey> keys)
        {
            this.CheckRead();
            this.data->GetKeyArray(keys);
        }

        public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.CheckRead();
            return this.data->GetValueArray<TValue>(allocator);
        }

        public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            this.CheckRead();
            return this.data->GetKeyValueArrays<TValue>(allocator);
        }

        public readonly NativeHashMap<TKey, TValue>.Enumerator GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(this.m_Safety);
            var ash = this.m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref ash);
#endif
            return new NativeHashMap<TKey, TValue>.Enumerator
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = ash,
#endif
                m_Enumerator = new HashMapHelper<TKey>.Enumerator(this.data),
            };
        }

        /// <summary>
        /// Not implemented; use the concrete GetEnumerator instead.
        /// </summary>
        IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented; use the concrete GetEnumerator instead.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
#endif
        }

        /// <summary>
        /// Aliases the original map's storage; does not allocate or own a copy.
        /// </summary>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(ref this);
        }

        /// <summary>
        /// Aliases the original map's storage; does not allocate or own a copy.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public readonly struct ReadOnly : IEnumerable<KVPair<TKey, TValue>>
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly HashMapHelper<TKey>* data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal readonly AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ReadOnly>();
#endif

            internal ReadOnly(ref NativeMultiHashMap<TKey, TValue> data)
            {
                this.data = data.data;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                this.m_Safety = data.m_Safety;
                CollectionHelper.SetStaticSafetyId<ReadOnly>(ref this.m_Safety, ref s_staticSafetyId.Data);
#endif
            }

            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    this.CheckRead();
                    return this.data->IsCreated;
                }
            }

            public readonly bool IsEmpty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    this.CheckRead();
                    if (!this.data->IsCreated)
                    {
                        return true;
                    }

                    return this.data->IsEmpty;
                }
            }

            public readonly int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    this.CheckRead();
                    return this.data->Count;
                }
            }

            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    this.CheckRead();
                    return this.data->Capacity;
                }
            }

            public bool TryGetFirstValue(TKey key, out TValue item, out HashMapIterator<TKey> it)
            {
                this.CheckRead();
                return this.data->TryGetFirstValue(key, out item, out it);
            }

            public bool TryGetNextValue(out TValue item, ref HashMapIterator<TKey> it)
            {
                this.CheckRead();
                return this.data->TryGetNextValue(out item, ref it);
            }

            public readonly bool ContainsKey(TKey key)
            {
                this.CheckRead();
                return this.data->Find(key) != -1;
            }

            public readonly TValue this[TKey key]
            {
                get
                {
                    this.CheckRead();

                    TValue result;
                    if (!this.data->TryGetValue(key, out result))
                    {
                        this.ThrowKeyNotPresent(key);
                    }

                    return result;
                }
            }

            public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
            {
                this.CheckRead();
                return this.data->GetKeyArray(allocator);
            }

            public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
            {
                this.CheckRead();
                return this.data->GetValueArray<TValue>(allocator);
            }

            public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
            {
                this.CheckRead();
                return this.data->GetKeyValueArrays<TValue>(allocator);
            }

            public readonly NativeHashMap<TKey, TValue>.Enumerator GetEnumerator()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(this.m_Safety);
                var ash = this.m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref ash);
#endif
                return new NativeHashMap<TKey, TValue>.Enumerator
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_Safety = ash,
#endif
                    m_Enumerator = new HashMapHelper<TKey>.Enumerator(this.data),
                };
            }

            /// <summary>
            /// Not implemented; use the concrete GetEnumerator instead.
            /// </summary>
            IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Not implemented; use the concrete GetEnumerator instead.
            /// </summary>
            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private readonly void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private readonly void ThrowKeyNotPresent(TKey key)
            {
                throw new ArgumentException($"Key: {key} is not present.");
            }
        }
    }

    public struct HashMapIterator<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        public int EntryIndex;
        internal TKey Key;
        internal int NextEntryIndex;
    }

    public static unsafe class NativeMultiHashMapExtensions
    {
        public static int Remove<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> hashMap, TKey key, TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged, IEquatable<TValue>
        {
            hashMap.CheckWrite();
            return hashMap.data->Remove(key, value);
        }

        public static bool RemoveFirst<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> hashMap, TKey key, TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged, IEquatable<TValue>
        {
            hashMap.CheckWrite();
            return hashMap.data->RemoveFirst(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearLengthBuckets<TKey, TValue>(this ref NativeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.CheckWrite();
            hashMap.data->ClearLengthBuckets();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RecalculateBuckets<TKey, TValue>(this ref NativeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.CheckWrite();
            hashMap.data->RecalculateBuckets();
        }

        /// <summary>
        /// Does not update Count; set it separately.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReserveAtomicNoResize<TKey, TValue>(this ref NativeMultiHashMap<TKey, TValue> hashMap, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.data->ReserveAtomicNoResize(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCount<TKey, TValue>(this ref NativeMultiHashMap<TKey, TValue> hashMap, int count)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.data->SetCount(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TKey* GetKeys<TKey, TValue>(this in NativeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.data->Keys;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue* GetValues<TKey, TValue>(this in NativeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return (TValue*)hashMap.data->Ptr;
        }

        public static void GetUniqueKeyArray<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> container, NativeList<TKey> keys)
            where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
            where TValue : unmanaged
        {
            container.GetKeyArray(keys);
            keys.Sort();
            var uniques = keys.AsArray().Unique();
            keys.ResizeUninitialized(uniques);
        }
    }

    internal static unsafe class HashMapHelperExtensions
    {
        internal static int Remove<TKey>(this ref HashMapHelper<TKey> hashMapHelper, in TKey key)
            where TKey : unmanaged, IEquatable<TKey>
        {
            if (hashMapHelper.Capacity != 0)
            {
                var removed = 0;

                // First find the slot based on the hash
                var bucket = hashMapHelper.GetBucket(key);

                var prevEntry = -1;
                var entryIdx = hashMapHelper.Buckets[bucket];

                while (entryIdx >= 0 && entryIdx < hashMapHelper.Capacity)
                {
                    if (UnsafeUtility.ReadArrayElement<TKey>(hashMapHelper.Keys, entryIdx).Equals(key))
                    {
                        ++removed;

                        // Found matching element, remove it
                        if (prevEntry < 0)
                        {
                            hashMapHelper.Buckets[bucket] = hashMapHelper.Next[entryIdx];
                        }
                        else
                        {
                            hashMapHelper.Next[prevEntry] = hashMapHelper.Next[entryIdx];
                        }

                        // And free the index
                        var nextIdx = hashMapHelper.Next[entryIdx];
                        hashMapHelper.Next[entryIdx] = hashMapHelper.FirstFreeIdx;
                        hashMapHelper.FirstFreeIdx = entryIdx;
                        entryIdx = nextIdx;
                    }
                    else
                    {
                        prevEntry = entryIdx;
                        entryIdx = hashMapHelper.Next[entryIdx];
                    }
                }

                hashMapHelper.Count -= removed;
                return removed;
            }

            return 0;
        }

        internal static int Remove<TKey, TValue>(this ref HashMapHelper<TKey> hashMapHelper, in TKey key, in TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged, IEquatable<TValue>
        {
            if (hashMapHelper.Capacity != 0)
            {
                var removed = 0;

                // First find the slot based on the hash
                var bucket = hashMapHelper.GetBucket(key);

                var prevEntry = -1;
                var entryIdx = hashMapHelper.Buckets[bucket];

                while (entryIdx >= 0 && entryIdx < hashMapHelper.Capacity)
                {
                    if (UnsafeUtility.ReadArrayElement<TKey>(hashMapHelper.Keys, entryIdx).Equals(key) &&
                        UnsafeUtility.ReadArrayElement<TValue>(hashMapHelper.Ptr, entryIdx).Equals(value))
                    {
                        ++removed;

                        // Found matching element, remove it
                        if (prevEntry < 0)
                        {
                            hashMapHelper.Buckets[bucket] = hashMapHelper.Next[entryIdx];
                        }
                        else
                        {
                            hashMapHelper.Next[prevEntry] = hashMapHelper.Next[entryIdx];
                        }

                        // And free the index
                        var nextIdx = hashMapHelper.Next[entryIdx];
                        hashMapHelper.Next[entryIdx] = hashMapHelper.FirstFreeIdx;
                        hashMapHelper.FirstFreeIdx = entryIdx;
                        entryIdx = nextIdx;
                    }
                    else
                    {
                        prevEntry = entryIdx;
                        entryIdx = hashMapHelper.Next[entryIdx];
                    }
                }

                hashMapHelper.Count -= removed;
                return removed;
            }

            return 0;
        }

        internal static bool RemoveFirst<TKey, TValue>(this ref HashMapHelper<TKey> hashMapHelper, in TKey key, in TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged, IEquatable<TValue>
        {
            if (hashMapHelper.Capacity != 0)
            {
                var removed = 0;

                // First find the slot based on the hash
                var bucket = hashMapHelper.GetBucket(key);

                var prevEntry = -1;
                var entryIdx = hashMapHelper.Buckets[bucket];

                while (entryIdx >= 0 && entryIdx < hashMapHelper.Capacity)
                {
                    if (UnsafeUtility.ReadArrayElement<TKey>(hashMapHelper.Keys, entryIdx).Equals(key) &&
                        UnsafeUtility.ReadArrayElement<TValue>(hashMapHelper.Ptr, entryIdx).Equals(value))
                    {
                        ++removed;

                        // Found matching element, remove it
                        if (prevEntry < 0)
                        {
                            hashMapHelper.Buckets[bucket] = hashMapHelper.Next[entryIdx];
                        }
                        else
                        {
                            hashMapHelper.Next[prevEntry] = hashMapHelper.Next[entryIdx];
                        }

                        // And free the index
                        hashMapHelper.Next[entryIdx] = hashMapHelper.FirstFreeIdx;
                        hashMapHelper.FirstFreeIdx = entryIdx;
                        break;
                    }

                    prevEntry = entryIdx;
                    entryIdx = hashMapHelper.Next[entryIdx];
                }

                hashMapHelper.Count -= removed;
                return removed > 0;
            }

            return false;
        }

        internal static bool TryGetFirstValue<TKey, TValue>(this in HashMapHelper<TKey> hashMapHelper, TKey key, out TValue item, out HashMapIterator<TKey> it)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            it.Key = key;

            if (hashMapHelper.AllocatedIndex <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            var bucket = hashMapHelper.GetBucket(it.Key);
            it.EntryIndex = it.NextEntryIndex = hashMapHelper.Buckets[bucket];

            return hashMapHelper.TryGetNextValue(out item, ref it);
        }

        internal static bool TryGetNextValue<TKey, TValue>(this in HashMapHelper<TKey> hashMapHelper, out TValue item, ref HashMapIterator<TKey> it)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var entryIdx = it.NextEntryIndex;
            it.EntryIndex = -1;
            it.NextEntryIndex = -1;

            if (entryIdx < 0 || entryIdx >= hashMapHelper.Capacity)
            {
                item = default;
                return false;
            }

            var nextPtrs = hashMapHelper.Next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(hashMapHelper.Keys, entryIdx).Equals(it.Key))
            {
                entryIdx = nextPtrs[entryIdx];
                if ((uint)entryIdx >= (uint)hashMapHelper.Capacity)
                {
                    item = default;
                    return false;
                }
            }

            it.NextEntryIndex = nextPtrs[entryIdx];
            it.EntryIndex = entryIdx;
            item = UnsafeUtility.ReadArrayElement<TValue>(hashMapHelper.Ptr, entryIdx);
            return true;
        }

        internal static void GetKeyArray<TKey>(this in HashMapHelper<TKey> hashMapHelper, NativeList<TKey> result)
            where TKey : unmanaged, IEquatable<TKey>
        {
            result.ResizeUninitialized(hashMapHelper.Count);

            for (int i = 0, count = 0, max = result.Length, capacity = hashMapHelper.BucketCapacity; i < capacity && count < max; ++i)
            {
                var bucket = hashMapHelper.Buckets[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TKey>(hashMapHelper.Keys, bucket);
                    bucket = hashMapHelper.Next[bucket];
                }
            }
        }
    }

    internal sealed unsafe class NativeMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly HashMapHelper<TKey>* data;

        public NativeMultiHashMapDebuggerTypeProxy(NativeMultiHashMap<TKey, TValue> target)
        {
            this.data = target.data;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();

                if (this.data == null)
                {
                    return result;
                }

                using var kva = this.data->GetKeyValueArrays<TValue>(Allocator.Temp);

                for (var i = 0; i < kva.Length; ++i)
                {
                    result.Add(new Pair<TKey, TValue>(kva.Keys[i], kva.Values[i]));
                }

                return result;
            }
        }
    }
}
