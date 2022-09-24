// <copyright file="NativeLookup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerTypeProxy(typeof(NativeLookupDebuggerTypeProxy<,>))]
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
    public struct NativeLookup<TKey, TValue> : INativeDisposable, IEnumerable<KeyValue<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal UnsafeLookup<TKey, TValue> lookupData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeLookup<TKey, TValue>>();

#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif
#endif

        /// <summary>Initializes a new instance of the <see cref="NativeLookup{TKey, TValue}"/> struct. </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeLookup(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            this.lookupData = new UnsafeLookup<TKey, TValue>(capacity, allocator.Handle);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
#else
            if (allocator.IsCustomAllocator)
            {
                this.m_Safety = AtomicSafetyHandle.Create();
                this.m_DisposeSentinel = null;
            }
            else
            {
                DisposeSentinel.Create(out this.m_Safety, out this.m_DisposeSentinel, 1, allocator.ToAllocator);
            }
#endif

            CollectionHelper.SetStaticSafetyId<NativeLookup<TKey, TValue>>(ref this.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(this.m_Safety, true);
#endif
        }

        /// <summary> Create a NativeLookup from a managed array, using a provided Allocator. </summary>
        /// <param name="length">The desired capacity of the NativeLookup.</param>
        /// <param name="allocator">The Allocator to use.</param>
        /// <returns>Returns the NativeLookup that was created.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int), typeof(AllocatorManager.AllocatorHandle) })]
        public static NativeLookup<TKey, TValue> Create<TU>(int length, ref TU allocator)
            where TU : unmanaged, AllocatorManager.IAllocator
        {
            var nativeLookup = default(NativeLookup<TKey, TValue>);

            nativeLookup.lookupData = new UnsafeLookup<TKey, TValue>(length, allocator.Handle);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            nativeLookup.m_Safety = CollectionHelper.CreateSafetyHandle(allocator.Handle);
#else
            if (allocator.IsCustomAllocator)
            {
                nativeLookup.m_Safety = AtomicSafetyHandle.Create();
                nativeLookup.m_DisposeSentinel = null;
            }
            else
            {
                DisposeSentinel.Create(out nativeLookup.m_Safety, out nativeLookup.m_DisposeSentinel, 1, allocator.ToAllocator);
            }
#endif

            CollectionHelper.SetStaticSafetyId<NativeLookup<TKey, TValue>>(
                ref nativeLookup.m_Safety, ref s_staticSafetyId.Data);
#endif

            return nativeLookup;
        }

        /// <summary> Whether this lookup is empty. </summary>
        /// <value> True if the lookup is empty or if the lookup has not been constructed. </value>
        public bool IsEmpty
        {
            get
            {
                this.CheckRead();
                return this.lookupData.IsEmpty;
            }
        }

        /// <summary> Whether this lookup has been allocated (and not yet deallocated). </summary>
        /// <value>True if this lookup has been allocated (and not yet deallocated).</value>
        public bool IsCreated => this.lookupData.IsCreated;

        /// <summary> Returns the current number of key-value pairs in this lookup. </summary>
        /// <remarks> Key-value pairs with matching keys are counted as separate, individual pairs. </remarks>
        /// <returns> The current number of key-value pairs in this lookup. </returns>
        public int Length
        {
            get
            {
                this.CheckRead();
                return this.lookupData.Length;
            }
        }

        /// <summary> Returns the number of key-value pairs that fit in the current allocation. </summary>
        /// <value>The number of key-value pairs that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        /// <exception cref="Exception">Thrown if `value` is less than the current capacity.</exception>
        public int Capacity
        {
            get
            {
                this.CheckRead();
                return this.lookupData.Capacity;
            }

            set
            {
                this.CheckWrite();
                this.lookupData.Capacity = value;
            }
        }

        /// <summary> Gets and sets values by key. </summary>
        /// <remarks>Getting a key that is not present will throw. Setting a key that is not already present will add the key.</remarks>
        /// <param name="key">The key to look up.</param>
        /// <value>The value associated with the key.</value>
        /// <exception cref="ArgumentException">For getting, thrown if the key was not present.</exception>
        public TValue this[TKey key]
        {
            get
            {
                this.CheckRead();
                if (this.lookupData.TryGetValue(key, out var res))
                {
                    return res;
                }

                this.ThrowKeyNotPresent(key);
                return default;
            }

            set
            {
                this.CheckWrite();
                this.lookupData[key] = value;
            }
        }


        /// <summary> Removes all key-value pairs. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public void Clear()
        {
            this.CheckWrite();
            this.lookupData.Clear();
        }

        /// <summary> Releases all resources (memory and safety handles). </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#else
            DisposeSentinel.Dispose(ref this.m_Safety, ref this.m_DisposeSentinel);
#endif
#endif
            this.lookupData.Dispose();
        }

        /// <summary> Creates and schedules a job that will dispose this lookup. </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this lookup.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
#else
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref this.m_DisposeSentinel);
#endif
            var dependency = this.lookupData.Dispose(inputDeps);
            AtomicSafetyHandle.Release(this.m_Safety);
#else
            var dependency = this.lookupData.Dispose(inputDeps);
#endif

            return dependency;
        }

        public void Add(TKey key, TValue value)
        {
            this.CheckWrite();
            this.lookupData.Add(key, value);
        }

        public void AddBatch(NativeArray<TKey> keys, NativeArray<TValue> values)
        {
            this.CheckWrite();
            this.lookupData.AddBatch(keys, values);
        }

        public unsafe void AddBatch(TKey* keys, TValue* values, int length)
        {
            this.CheckWrite();
            this.lookupData.AddBatch(keys, values, length);
        }

        public void AddBatch(NativeArray<TKey> keys)
        {
            this.CheckWrite();
            this.lookupData.AddBatch(keys);
        }

        public unsafe void AddBatch(TKey* keys, int length)
        {
            this.CheckWrite();
            this.lookupData.AddBatch(keys, length);
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            this.CheckRead();
            return this.lookupData.TryGetValue(key, out item);
        }

        /// <summary> Returns true if a given key is present in this lookup. </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present.</returns>
        public bool ContainsKey(TKey key)
        {
            this.CheckRead();
            return this.lookupData.ContainsKey(key);
        }

        /// <summary> Gets an iterator for a key. </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">Outputs the associated value represented by the iterator.</param>
        /// <param name="it">Outputs an iterator.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetFirstValue(TKey key, out TValue item, out NativeLookupIterator<TKey> it)
        {
            this.CheckRead();
            return this.lookupData.TryGetFirstValue(key, out item, out it);
        }

        /// <summary> Advances an iterator to the next value associated with its key. </summary>
        /// <param name="item">Outputs the next value.</param>
        /// <param name="it">A reference to the iterator to advance.</param>
        /// <returns>True if the key was present and had another value.</returns>
        public bool TryGetNextValue(out TValue item, ref NativeLookupIterator<TKey> it)
        {
            this.CheckRead();
            return this.lookupData.TryGetNextValue(out item, ref it);
        }

        /// <summary> Returns an array with a copy of all the keys (in no particular order). </summary>
        /// <remarks>A key with *N* values is included *N* times in the array.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all the keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.CheckRead();
            return this.lookupData.GetKeyArray(allocator);
        }

        /// <summary> Returns an array with a copy of all the values (in no particular order). </summary>
        /// <remarks>The values are not deduplicated. </remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all the values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.CheckRead();
            return this.lookupData.GetValueArray(allocator);
        }

        /// <summary> Returns a NativeKeyValueArrays with a copy of all the keys and values (in no particular order). </summary>
        /// <remarks>A key with *N* values is included *N* times in the array. </remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all the keys and values (in no particular order).</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            this.CheckRead();
            return this.lookupData.GetKeyValueArrays(allocator);
        }

        public void CalculateHashes()
        {
            this.CheckWrite();
            this.lookupData.CalculateHashes();
        }

        /// <summary> Returns a parallel writer for this lookup. </summary>
        /// <returns> A parallel writer for this lookup. </returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.writer = this.lookupData.AsParallelWriter();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            writer.m_Safety = this.m_Safety;
            CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref writer.m_Safety, ref s_staticSafetyId.Data);
#endif
            return writer;
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this lookup.
        /// </summary>
        /// <remarks>A key with *N* values is visited by the enumerator *N* times.</remarks>
        /// <returns>An enumerator over the key-value pairs of this lookup.</returns>
        public unsafe KeyValueEnumerator GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(this.m_Safety);
            var handle = this.m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref handle);
#endif
            return new KeyValueEnumerator
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = handle,
#endif
                m_Enumerator = new UnsafeLookupDataEnumerator(this.lookupData.buffer),
            };
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KeyValue<TKey, TValue>> IEnumerable<KeyValue<TKey, TValue>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present in the NativeHashMap.");
        }

        /// <summary> An enumerator over the key-value pairs of a lookup. </summary>
        /// <remarks>A key with *N* values is visited by the enumerator *N* times.
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first key-value pair.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct KeyValueEnumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            internal UnsafeLookupDataEnumerator m_Enumerator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next key-value pair.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
            public bool MoveNext()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Enumerator.MoveNext();
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                m_Enumerator.Reset();
            }

            /// <summary>
            /// The current key-value pair.
            /// </summary>
            /// <value>The current key-value pair.</value>
            public KeyValue<TKey, TValue> Current
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return m_Enumerator.GetCurrent<TKey, TValue>();
                }
            }

            object IEnumerator.Current => Current;
        }

        /// <summary> A parallel writer for a NativeLookup. </summary>
        /// <remarks> Use <see cref="AsParallelWriter"/> to create a parallel writer for a NativeLookup. </remarks>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public struct ParallelWriter
        {
            internal UnsafeLookup<TKey, TValue>.ParallelWriter writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();
#endif
            /// <summary> Returns the index of the current thread. </summary>
            /// <remarks>In a job, each thread gets its own copy of the ParallelWriter struct, and the job system assigns
            /// each copy the index of its thread.</remarks>
            /// <value>The index of the current thread.</value>
            public int ThreadIndex => this.writer.threadIndex;

            /// <summary> Returns the number of key-value pairs that fit in the current allocation. </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public int Capacity
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
                    return this.writer.Capacity;
                }
            }

            public void Add(TKey key, TValue value)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
#endif
                this.writer.Add(key, value);
            }

            public void AddBatch(NativeArray<TKey> keys, NativeArray<TValue> values)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
#endif
                this.writer.AddBatch(keys, values);
            }

            public unsafe void AddBatch(TKey* keys, TValue* values, int length)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
#endif
                this.writer.AddBatch(keys, values, length);
            }

            public void AddKeyValues(NativeArray<TKey> keys, NativeArray<TValue> values)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
#endif
                this.writer.AddKeyValues(keys, values);
            }

            public unsafe void AddKeyValues(TKey* keys, TValue* values, int length)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
#endif
                this.writer.AddKeyValues(keys, values, length);
            }
        }
    }

    internal sealed class NativeLookupDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
        where TValue : unmanaged
    {
        private NativeLookup<TKey, TValue> target;

        public NativeLookupDebuggerTypeProxy(NativeLookup<TKey, TValue> target)
        {
            this.target = target;
        }

        private static (NativeArray<TKey> WithDuplicates, int Uniques) GetUniqueKeyArray(ref NativeLookup<TKey, TValue> lookup, AllocatorManager.AllocatorHandle allocator)
        {
            var withDuplicates = lookup.GetKeyArray(allocator);
            withDuplicates.Sort();
            int uniques = withDuplicates.Unique();
            return (withDuplicates, uniques);
        }

        public List<ListPair<TKey, List<TValue>>> Items
        {
            get
            {
                var result = new List<ListPair<TKey, List<TValue>>>();
                var keys = GetUniqueKeyArray(ref this.target, Allocator.Temp);

                using (keys.WithDuplicates)
                {
                    for (var k = 0; k < keys.Uniques; ++k)
                    {
                        var values = new List<TValue>();
                        if (this.target.TryGetFirstValue(keys.Item1[k], out var value, out var iterator))
                        {
                            do
                            {
                                values.Add(value);
                            }
                            while (this.target.TryGetNextValue(out value, ref iterator));
                        }

                        result.Add(new ListPair<TKey, List<TValue>>(keys.Item1[k], values));
                    }
                }

                return result;
            }
        }
    }

    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    public struct NativeLookupIterator<TKey>
        where TKey : struct
    {
        internal TKey key;
        internal int NextEntryIndex;
        internal int EntryIndex;

        /// <summary>
        /// Returns the entry index.
        /// </summary>
        /// <returns>The entry index.</returns>
        public int GetEntryIndex() => EntryIndex;
    }
}
