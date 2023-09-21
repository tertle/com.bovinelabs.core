// <copyright file="DynamicMultiHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicMultiHashMapDebuggerTypeProxy<,>))]
    public unsafe struct DynamicMultiHashMap<TKey, TValue> : IEnumerable<KVPair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly bool readOnly;
#endif

        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<TKey>* helper;

        internal DynamicMultiHashMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.readOnly = this.buffer.m_IsReadOnly == 1;
            this.helper = this.readOnly ? buffer.AsHelperReadOnly<TKey>() : buffer.AsHelper<TKey>();
#else
            this.helper = buffer.AsHelper<TKey>();
#endif
        }

        /// <summary> Gets a value indicating whether whether this hash map has been allocated (and not yet deallocated). </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated). </value>
        public readonly bool IsCreated => this.buffer.IsCreated;

        /// <summary> Gets a value indicating whether whether this hash map is empty. </summary>
        /// <value> True if this hash map is empty or if the map has not been constructed. </value>
        public readonly bool IsEmpty => !this.IsCreated || this.helper->IsEmpty;

        /// <summary> Gets the current number of key-value pairs in this hash map. </summary>
        /// <returns> The current number of key-value pairs in this hash map. </returns>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.helper->Count;
        }

        /// <summary> Gets or sets the number of key-value pairs that fit in the current allocation. </summary>
        /// <value> The number of key-value pairs that fit in the current allocation. </value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => this.helper->Capacity;

            set
            {
                this.CheckWrite();
                DynamicHashMapHelper<TKey>.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal DynamicHashMapHelper<TKey>* Helper => this.helper;

        /// <summary> Removes all key-value pairs. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public void Clear()
        {
            this.CheckWrite();
            this.helper->Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>If the key is already present, this method throws without modifying the hash map.</remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        /// <exception cref="ArgumentException">Thrown if the key was already present.</exception>
        public void Add(TKey key, TValue item)
        {
            this.CheckWrite();
            var idx = DynamicHashMapHelper<TKey>.AddMulti(this.buffer, ref this.helper, key);
            UnsafeUtility.WriteArrayElement(this.helper->Values, idx, item);
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>The number of elements removed. </returns>
        public int Remove(TKey key)
        {
            return this.helper->Remove(key);
        }

        /*/// <summary> Returns the value associated with a key. </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>*/
        public bool TryGetFirstValue(TKey key, out TValue item, out NativeMultiHashMapIterator<TKey> it)
        {
            return this.helper->TryGetFirstValue(key, out item, out it);
        }

        public bool TryGetNextValue(out TValue item, ref NativeMultiHashMapIterator<TKey> it)
        {
            return this.helper->TryGetNextValue(out item, ref it);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present.</returns>
        public bool ContainsKey(TKey key)
        {
            return this.helper->Find(key) != -1;
        }

        /// <summary>
        /// Returns true if a given key and value combination is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">The value to look up.</param>
        /// <returns>True if the key and value combination was present.</returns>
        public bool Contains<T>(TKey key, T value)
            where T : unmanaged, IEquatable<TValue>
        {
            var e = this.GetValuesForKey(key);
            while (e.MoveNext())
            {
                if (value.Equals(e.Current))
                {
                    return true;
                }
            }

            return false;
        }

        public int CountValuesForKey(TKey key)
        {
            var count = 0;
            var e = this.GetValuesForKey(key);
            while (e.MoveNext())
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess()
        {
            this.CheckWrite();
            DynamicHashMapHelper<TKey>.TrimExcess(this.buffer, ref this.helper);
        }

        public void AddBatchUnsafe(NativeArray<TKey> keys, NativeArray<TValue> values)
        {
            CheckLengthsMatch(keys.Length, values.Length);
            this.AddBatchUnsafe((TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public void AddBatchUnsafe(TKey* keys, TValue* values, int length)
        {
            this.CheckWrite();
            DynamicHashMapHelper<TKey>.AddBatchUnsafe(this.buffer, ref this.helper, keys, (byte*)values, length);
        }

        /// <summary> Returns an array with a copy of all this hash map's keys (in no particular order). </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            return this.helper->GetKeyArray(allocator);
        }

        /// <summary> Returns an array with a copy of all this hash map's values (in no particular order). </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            return this.helper->GetValueArray<TValue>(allocator);
        }

        /// <summary> Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values. </summary>
        /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            return this.helper->GetKeyValueArrays<TValue>(allocator);
        }

        public DynamicHashMapKeyEnumerator<TKey, TValue> GetValuesForKey(TKey key)
        {
            return new DynamicHashMapKeyEnumerator<TKey, TValue> { hashmap = this, key = key, isFirst = 1 };
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public DynamicHashMapEnumerator<TKey, TValue> GetEnumerator()
        {
            return new DynamicHashMapEnumerator<TKey, TValue>(this.helper);
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
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
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicHashMapHelper<TKey>>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (this.readOnly)
            {
                throw new ArgumentException($"Trying to write to a readonly DynamicMultiHashMap");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckLengthsMatch(int keys, int values)
        {
            if (keys != values)
            {
                throw new ArgumentException("Key and value array don't match");
            }
        }
    }

    internal sealed unsafe class DynamicMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicHashMapHelper<TKey>* helper;

        public DynamicMultiHashMapDebuggerTypeProxy(DynamicMultiHashMap<TKey, TValue> target)
        {
            this.helper = target.Helper;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();

                if (this.helper == null)
                {
                    return result;
                }

                using var kva = this.helper->GetKeyValueArrays<TValue>(Allocator.Temp);

                for (var i = 0; i < kva.Length; ++i)
                {
                    result.Add(new Pair<TKey, TValue>(kva.Keys[i], kva.Values[i]));
                }

                return result;
            }
        }
    }
}
