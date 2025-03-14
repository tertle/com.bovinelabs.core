// <copyright file="UnsafeMultiHashMap.cs" company="BovineLabs">
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
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    /// <summary> An unordered, expandable associative multi array. </summary>
    /// <remarks> Not suitable for parallel write access. Use <see cref="NativeParallelMultiHashMap{TKey, TValue}" /> instead. </remarks>
    /// <typeparam name="TKey"> The type of the keys. </typeparam>
    /// <typeparam name="TValue"> The type of the values. </typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeMultiHashMapDebuggerTypeProxy<,>))]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Exposed for others")]
    public unsafe struct UnsafeMultiHashMap<TKey, TValue> : INativeDisposable, IEnumerable<KVPair<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal HashMapHelper<TKey> data;

        /// <summary> Initializes a new instance of the <see cref="UnsafeMultiHashMap{TKey, TValue}" /> struct. </summary>
        /// <param name="initialCapacity"> The number of key-value pairs that should fit in the initial allocation. </param>
        /// <param name="allocator"> The allocator to use. </param>
        public UnsafeMultiHashMap(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            this.data = default;
            this.data.Init(initialCapacity, sizeof(TValue), HashMapHelper<TKey>.kMinimumCapacity, allocator);
        }

        /// <summary> Gets a value indicating whether whether this hash map has been allocated (and not yet deallocated). </summary>
        /// <value> True if this hash map has been allocated (and not yet deallocated). </value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data.IsCreated;
        }

        /// <summary> Gets a value indicating whether whether this hash map is empty. </summary>
        /// <value> True if this hash map is empty or if the map has not been constructed. </value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !this.IsCreated || this.data.IsEmpty;
        }

        public UnsafeHashMapBucketData<TKey, TValue> UnsafeBucketData => new((TValue*)this.data.Ptr, this.data.Keys, this.data.Next, this.data.Buckets);

        /// <summary> Gets the current number of key-value pairs in this hash map. </summary>
        /// <returns> The current number of key-value pairs in this hash map. </returns>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data.Count;
        }

        /// <summary> Gets or sets the number of key-value pairs that fit in the current allocation. </summary>
        /// <param name="value"> A new capacity. Must be larger than the current capacity. </param>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => this.data.Capacity;

            set => this.data.Resize(value);
        }

        /// <summary>
        /// Releases all resources (memory).
        /// </summary>
        public void Dispose()
        {
            if (!this.IsCreated)
            {
                return;
            }

            this.data.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps"> A job handle. The newly scheduled job will depend upon this handle. </param>
        /// <returns> The handle of a new job that will dispose this hash map. </returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!this.IsCreated)
            {
                return inputDeps;
            }

            var jobHandle = new UnsafeDisposeJob
            {
                Ptr = this.data.Ptr,
                Allocator = this.data.Allocator,
            }.Schedule(inputDeps);

            this.data = default;

            return jobHandle;
        }

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public void Clear()
        {
            this.data.Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        public void Add(TKey key, TValue item)
        {
            var idx = this.data.AddNoFind(key);
            UnsafeUtility.WriteArrayElement(this.data.Ptr, idx, item);
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        public void AddNoResize(TKey key, TValue item)
        {
            var idx = this.data.AddNoFindNoResize(key);
            UnsafeUtility.WriteArrayElement(this.data.Ptr, idx, item);
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        public void AddLinear(TKey key, TValue item)
        {
            var idx = this.data.AddLinearNoResize(key);
            UnsafeUtility.WriteArrayElement(this.data.Ptr, idx, item);
        }

        /// <summary>
        /// Removes a key and its associated value(s).
        /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <returns> The number of removed key-value pairs. If the key was not present, returns 0. </returns>
        public int Remove(TKey key)
        {
            return this.data.Remove(key);
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <param name="it"> A reference to the iterator to advance. </param>
        /// <returns> True if the key was present. </returns>
        public bool TryGetFirstValue(TKey key, out TValue item, out HashMapIterator<TKey> it)
        {
            return this.data.TryGetFirstValue(key, out item, out it);
        }

        /// <summary> Advances an iterator to the next value associated with its key. </summary>
        /// <param name="item"> Outputs the next value. </param>
        /// <param name="it"> A reference to the iterator to advance. </param>
        /// <returns> True if the key was present and had another value. </returns>
        public bool TryGetNextValue(out TValue item, ref HashMapIterator<TKey> it)
        {
            return this.data.TryGetNextValue(out item, ref it);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <returns> True if the key was present. </returns>
        public bool ContainsKey(TKey key)
        {
            return this.data.Find(key) != -1;
        }

        /// <summary>
        /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess()
        {
            this.data.TrimExcess();
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's keys (in no particular order).
        /// </summary>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> An array with a copy of all this hash map's keys (in no particular order). </returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            return this.data.GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's values (in no particular order).
        /// </summary>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> An array with a copy of all this hash map's values (in no particular order). </returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            return this.data.GetValueArray<TValue>(allocator);
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
        /// </summary>
        /// <remarks> The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`. </remarks>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> A NativeKeyValueArrays with a copy of all this hash map's keys and values. </returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            return this.data.GetKeyValueArrays<TValue>(allocator);
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns> An enumerator over the key-value pairs of this hash map. </returns>
        public UnsafeHashMap<TKey, TValue>.Enumerator GetEnumerator()
        {
            fixed (HashMapHelper<TKey>* data = &this.data)
            {
                return new UnsafeHashMap<TKey, TValue>.Enumerator { m_Enumerator = new HashMapHelper<TKey>.Enumerator(data) };
            }
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
    }

    public static unsafe class UnsafeMultiHashMapExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearLengthBuckets<TKey, TValue>(this ref UnsafeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.data.ClearLengthBuckets();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RecalculateBuckets<TKey, TValue>(this ref UnsafeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.data.RecalculateBuckets();
        }

        /// <remarks> Note this does not bump Count and should be set separately. </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReserveAtomicNoResize<TKey, TValue>(this ref UnsafeMultiHashMap<TKey, TValue> hashMap, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.data.ReserveAtomicNoResize(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCount<TKey, TValue>(this ref UnsafeMultiHashMap<TKey, TValue> hashMap, int count)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.data.SetCount(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TKey* GetKeys<TKey, TValue>(this in UnsafeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.data.Keys;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue* GetValues<TKey, TValue>(this in UnsafeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return (TValue*)hashMap.data.Ptr;
        }
    }

    internal sealed class UnsafeMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private HashMapHelper<TKey> Data;

        public UnsafeMultiHashMapDebuggerTypeProxy(UnsafeMultiHashMap<TKey, TValue> target)
        {
            this.Data = target.data;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();
                using var kva = this.Data.GetKeyValueArrays<TValue>(Allocator.Temp);

                for (var i = 0; i < kva.Length; ++i)
                {
                    result.Add(new Pair<TKey, TValue>(kva.Keys[i], kva.Values[i]));
                }

                return result;
            }
        }
    }
}
