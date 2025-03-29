// <copyright file="NativeMultiHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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

    /// <summary> An unordered, expandable associative multi array. </summary>
    /// <remarks> Not suitable for parallel write access. Use <see cref="NativeParallelMultiHashMap{TKey, TValue}" /> instead. </remarks>
    /// <typeparam name="TKey"> The type of the keys. </typeparam>
    /// <typeparam name="TValue"> The type of the values. </typeparam>
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

        /// <summary> Initializes a new instance of the <see cref="NativeMultiHashMap{TKey, TValue}" /> struct. </summary>
        /// <param name="initialCapacity"> The number of key-value pairs that should fit in the initial allocation. </param>
        /// <param name="allocator"> The allocator to use. </param>
        public NativeMultiHashMap(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            this.data = HashMapHelper<TKey>.Alloc(initialCapacity, sizeof(TValue), HashMapHelper<TKey>.kMinimumCapacity, allocator);

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

        /// <summary> Gets a value indicating whether whether this hash map has been allocated (and not yet deallocated). </summary>
        /// <value> True if this hash map has been allocated (and not yet deallocated). </value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data != null && this.data->IsCreated;
        }

        /// <summary> Gets a value indicating whether whether this hash map is empty. </summary>
        /// <value> True if this hash map is empty or if the map has not been constructed. </value>
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

        /// <summary> Gets the current number of key-value pairs in this hash map. </summary>
        /// <returns> The current number of key-value pairs in this hash map. </returns>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.CheckRead();
                return this.data->Count;
            }
        }

        /// <summary> Gets or sets the number of key-value pairs that fit in the current allocation. </summary>
        /// <param name="value"> A new capacity. Must be larger than the current capacity. </param>
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

        /// <summary>
        /// Releases all resources (memory).
        /// </summary>
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

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps"> A job handle. The newly scheduled job will depend upon this handle. </param>
        /// <returns> The handle of a new job that will dispose this hash map. </returns>
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

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public void Clear()
        {
            this.CheckWrite();
            this.data->Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        public void Add(TKey key, TValue item)
        {
            this.CheckWrite();

            var idx = this.data->AddNoFind(key);
            UnsafeUtility.WriteArrayElement(this.data->Ptr, idx, item);
        }

        /// <summary>
        /// Removes a key and its associated value(s).
        /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <returns> The number of removed key-value pairs. If the key was not present, returns 0. </returns>
        public int Remove(TKey key)
        {
            this.CheckWrite();
            return this.data->Remove(key);
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <param name="it"> A reference to the iterator to advance. </param>
        /// <returns> True if the key was present. </returns>
        public readonly bool TryGetFirstValue(TKey key, out TValue item, out HashMapIterator<TKey> it)
        {
            this.CheckRead();
            return this.data->TryGetFirstValue(key, out item, out it);
        }

        /// <summary> Advances an iterator to the next value associated with its key. </summary>
        /// <param name="item"> Outputs the next value. </param>
        /// <param name="it"> A reference to the iterator to advance. </param>
        /// <returns> True if the key was present and had another value. </returns>
        public readonly bool TryGetNextValue(out TValue item, ref HashMapIterator<TKey> it)
        {
            this.CheckRead();
            return this.data->TryGetNextValue(out item, ref it);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <returns> True if the key was present. </returns>
        public readonly bool ContainsKey(TKey key)
        {
            this.CheckRead();
            return this.data->Find(key) != -1;
        }

        /// <summary>
        /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess()
        {
            this.CheckWrite();
            this.data->TrimExcess();
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's keys (in no particular order).
        /// </summary>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> An array with a copy of all this hash map's keys (in no particular order). </returns>
        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.CheckRead();
            return this.data->GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's keys (in no particular order).
        /// </summary>
        /// <param name="keys"> The list to store the keys in. </param>
        public readonly void GetKeyArray(NativeList<TKey> keys)
        {
            this.CheckRead();
            this.data->GetKeyArray(keys);
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's values (in no particular order).
        /// </summary>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> An array with a copy of all this hash map's values (in no particular order). </returns>
        public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.CheckRead();
            return this.data->GetValueArray<TValue>(allocator);
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
        /// </summary>
        /// <remarks> The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`. </remarks>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> A NativeKeyValueArrays with a copy of all this hash map's keys and values. </returns>
        public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            this.CheckRead();
            return this.data->GetKeyValueArrays<TValue>(allocator);
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns> An enumerator over the key-value pairs of this hash map. </returns>
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
        /// This method is not implemented. Use <see cref="GetEnumerator" /> instead.
        /// </summary>
        /// <returns> Throws NotImplementedException. </returns>
        /// <exception cref="NotImplementedException"> Method is not implemented. </exception>
        IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator" /> instead.
        /// </summary>
        /// <returns> Throws NotImplementedException. </returns>
        /// <exception cref="NotImplementedException"> Method is not implemented. </exception>
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
        /// Returns a readonly version of this NativeHashMap instance.
        /// </summary>
        /// <remarks> ReadOnly containers point to the same underlying data as the NativeHashMap it is made from. </remarks>
        /// <returns> ReadOnly instance for this. </returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(ref this);
        }

        /// <summary>
        /// A read-only alias for the value of a NativeMultiHashMap. Does not have its own allocated storage.
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

            /// <summary>
            /// Whether this hash map has been allocated (and not yet deallocated).
            /// </summary>
            /// <value> True if this hash map has been allocated (and not yet deallocated). </value>
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    this.CheckRead();
                    return this.data->IsCreated;
                }
            }

            /// <summary>
            /// Whether this hash map is empty.
            /// </summary>
            /// <value> True if this hash map is empty or if the map has not been constructed. </value>
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

            /// <summary>
            /// The current number of key-value pairs in this hash map.
            /// </summary>
            /// <returns> The current number of key-value pairs in this hash map. </returns>
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
            /// The number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value> The number of key-value pairs that fit in the current allocation. </value>
            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    this.CheckRead();
                    return this.data->Capacity;
                }
            }

            /// <summary> Returns the value associated with a key. </summary>
            /// <param name="key"> The key to look up. </param>
            /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
            /// <param name="it"> A reference to the iterator to advance. </param>
            /// <returns> True if the key was present. </returns>
            public bool TryGetFirstValue(TKey key, out TValue item, out HashMapIterator<TKey> it)
            {
                this.CheckRead();
                return this.data->TryGetFirstValue(key, out item, out it);
            }

            /// <summary> Advances an iterator to the next value associated with its key. </summary>
            /// <param name="item"> Outputs the next value. </param>
            /// <param name="it"> A reference to the iterator to advance. </param>
            /// <returns> True if the key was present and had another value. </returns>
            public bool TryGetNextValue(out TValue item, ref HashMapIterator<TKey> it)
            {
                this.CheckRead();
                return this.data->TryGetNextValue(out item, ref it);
            }

            /// <summary>
            /// Returns true if a given key is present in this hash map.
            /// </summary>
            /// <param name="key"> The key to look up. </param>
            /// <returns> True if the key was present. </returns>
            public readonly bool ContainsKey(TKey key)
            {
                this.CheckRead();
                return this.data->Find(key) != -1;
            }

            /// <summary>
            /// Gets values by key.
            /// </summary>
            /// <remarks> Getting a key that is not present will throw. </remarks>
            /// <param name="key"> The key to look up. </param>
            /// <value> The value associated with the key. </value>
            /// <exception cref="ArgumentException"> For getting, thrown if the key was not present. </exception>
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

            /// <summary>
            /// Returns an array with a copy of all this hash map's keys (in no particular order).
            /// </summary>
            /// <param name="allocator"> The allocator to use. </param>
            /// <returns> An array with a copy of all this hash map's keys (in no particular order). </returns>
            public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
            {
                this.CheckRead();
                return this.data->GetKeyArray(allocator);
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's values (in no particular order).
            /// </summary>
            /// <param name="allocator"> The allocator to use. </param>
            /// <returns> An array with a copy of all this hash map's values (in no particular order). </returns>
            public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
            {
                this.CheckRead();
                return this.data->GetValueArray<TValue>(allocator);
            }

            /// <summary>
            /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
            /// </summary>
            /// <remarks> The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`. </remarks>
            /// <param name="allocator"> The allocator to use. </param>
            /// <returns> A NativeKeyValueArrays with a copy of all this hash map's keys and values. </returns>
            public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
            {
                this.CheckRead();
                return this.data->GetKeyValueArrays<TValue>(allocator);
            }

            /// <summary>
            /// Returns an enumerator over the key-value pairs of this hash map.
            /// </summary>
            /// <returns> An enumerator over the key-value pairs of this hash map. </returns>
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
            /// This method is not implemented. Use <see cref="GetEnumerator" /> instead.
            /// </summary>
            /// <returns> Throws NotImplementedException. </returns>
            /// <exception cref="NotImplementedException"> Method is not implemented. </exception>
            IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator" /> instead.
            /// </summary>
            /// <returns> Throws NotImplementedException. </returns>
            /// <exception cref="NotImplementedException"> Method is not implemented. </exception>
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

        /// <remarks> Note this does not bump Count and should be set separately. </remarks>
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
