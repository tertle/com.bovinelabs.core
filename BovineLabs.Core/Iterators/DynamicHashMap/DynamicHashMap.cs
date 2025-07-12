// <copyright file="DynamicHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicHashMapDebuggerTypeProxy<,>))]
    public unsafe struct DynamicHashMap<TKey, TValue> : IEnumerable<KVPair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<TKey>* helper;

        internal DynamicHashMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsHelper<TKey>();
        }

        /// <summary> Gets a value indicating whether this hash map has been allocated (and not yet deallocated). </summary>
        /// <value> True if this hash map has been allocated (and not yet deallocated). </value>
        public readonly bool IsCreated => this.buffer.IsCreated;

        /// <summary> Gets a value indicating whether this hash map is empty. </summary>
        /// <value> True if this hash map is empty or if the map has not been constructed. </value>
        public readonly bool IsEmpty
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return !this.IsCreated || this.helper->IsEmpty;
            }
        }

        /// <summary> Gets the current number of key-value pairs in this hash map. </summary>
        /// <returns> The current number of key-value pairs in this hash map. </returns>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.helper->Count;
            }
        }

        /// <summary> Gets or sets the number of key-value pairs that fit in the current allocation. </summary>
        /// <value> The number of key-value pairs that fit in the current allocation. </value>
        /// <param name="value"> A new capacity. Must be larger than the current capacity. </param>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.helper->Capacity;
            }

            set
            {
                this.buffer.CheckWriteAccess();
                this.RefCheck();
                DynamicHashMapHelper<TKey>.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal DynamicHashMapHelper<TKey>* Helper => this.helper;

        /// <summary> Gets and sets values by key. </summary>
        /// <remarks> Getting a key that is not present will throw. Setting a key that is not already present will add the key. </remarks>
        /// <param name="key"> The key to look up. </param>
        /// <value> The value associated with the key. </value>
        /// <exception cref="ArgumentException"> For getting, thrown if the key was not present. </exception>
        public TValue this[TKey key]
        {
            readonly get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                if (this.TryGetValue(key, out var res))
                {
                    return res;
                }

                ThrowKeyNotPresent(key);

                return default;
            }

            set
            {
                this.buffer.CheckWriteAccess();
                this.RefCheck();

                var idx = this.helper->Find(key);

                if (idx == -1)
                {
                    // Use optimized path that doesn't repeat the Find() call
                    idx = DynamicHashMapHelper<TKey>.AddWithKnownAbsence(this.buffer, ref this.helper, key);
                }

                UnsafeUtility.WriteArrayElement(this.helper->Values, idx, value);
            }
        }

        /// <summary> Removes all key-value pairs. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public readonly void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks> If the key is already present, this method returns false without modifying the hash map. </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <returns> True if the key-value pair was added. </returns>
        public bool TryAdd(TKey key, TValue item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = DynamicHashMapHelper<TKey>.TryAdd(this.buffer, ref this.helper, key);
            if (idx != -1)
            {
                UnsafeUtility.WriteArrayElement(this.helper->Values, idx, item);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks> If the key is already present, this method throws without modifying the hash map. </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <exception cref="ArgumentException"> Thrown if the key was already present. </exception>
        public void Add(TKey key, TValue item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = DynamicHashMapHelper<TKey>.AddUnique(this.buffer, ref this.helper, key);
            UnsafeUtility.WriteArrayElement(this.helper->Values, idx, item);
        }

        public ref TValue GetOrAddRef(TKey key, TValue defaultValue = default)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicHashMapHelper<TKey>.AddWithKnownAbsence(this.buffer, ref this.helper, key);
                UnsafeUtility.WriteArrayElement(this.helper->Values, idx, defaultValue);
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(this.helper->Values, idx);
        }

        public ref TValue GetOrAddRef(TKey key, out bool add, TValue defaultValue = default)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicHashMapHelper<TKey>.AddWithKnownAbsence(this.buffer, ref this.helper, key);
                UnsafeUtility.WriteArrayElement(this.helper->Values, idx, defaultValue);
                add = true;
            }
            else
            {
                add = false;
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(this.helper->Values, idx);
        }

        public Ptr<TValue> GetRef(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                return default;
            }

            return new Ptr<TValue>((TValue*)this.helper->Values + idx);
        }

        public readonly TValue GetOrDefault(TKey key, TValue defaultValue = default)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                return defaultValue;
            }

            return UnsafeUtility.ReadArrayElement<TValue>(this.helper->Values, idx);
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <returns> True if a key-value pair was removed. </returns>
        public readonly bool Remove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return this.helper->TryRemove(key) != -1;
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <returns> True if the key was present. </returns>
        public readonly bool TryGetValue(TKey key, out TValue item)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetValue(key, out item);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <returns> True if the key was present. </returns>
        public readonly bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Find(key) != -1;
        }

        /// <summary> Removes holes. </summary>
        public void Flatten()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicHashMapHelper<TKey>.Flatten(this.buffer, ref this.helper);
        }

        public void RemoveRangeShiftDown(int index, int range)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->RemoveRangeShiftDown(index, range);
        }

        public void AddBatchUnsafe(NativeArray<TKey> keys, NativeArray<TValue> values)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            CheckLengthsMatch(keys.Length, values.Length);
            this.AddBatchUnsafe((TKey*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        public void AddBatchUnsafe(TKey* keys, TValue* values, int length)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicHashMapHelper<TKey>.AddBatchUnsafe(this.buffer, ref this.helper, keys, (byte*)values, length);
        }

        public void AddBatchUnsafe(NativeSlice<TKey> keys, NativeSlice<TValue> values)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            CheckLengthsMatch(keys.Length, values.Length);
            DynamicHashMapHelper<TKey>.AddBatchUnsafe(this.buffer, ref this.helper, keys, values);
        }

        public void AddBatchUnsafe(NativeSlice<TKey> keys, NativeArray<TValue> values)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            CheckLengthsMatch(keys.Length, values.Length);
            DynamicHashMapHelper<TKey>.AddBatchUnsafe(this.buffer, ref this.helper, keys, values);
        }

        /// <summary> Returns an array with a copy of all this hash map's keys (in no particular order). </summary>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> An array with a copy of all this hash map's keys (in no particular order). </returns>
        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetKeyArray(allocator);
        }

        /// <summary> Returns an array with a copy of all this hash map's values (in no particular order). </summary>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> An array with a copy of all this hash map's values (in no particular order). </returns>
        public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetValueArray<TValue>(allocator);
        }

        /// <summary> Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values. </summary>
        /// <remarks> The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`. </remarks>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> A NativeKeyValueArrays with a copy of all this hash map's keys and values. </returns>
        public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetKeyValueArrays<TValue>(allocator);
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns> An enumerator over the key-value pairs of this hash map. </returns>
        public readonly DynamicHashMapEnumerator<TKey, TValue> GetEnumerator()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return new DynamicHashMapEnumerator<TKey, TValue>(this.helper);
        }

        public TValue* GetUnsafeValuePtr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return (TValue*)this.helper->Values;
        }

        public TKey* GetUnsafeKeyPtr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Keys;
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
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            var ptr = this.buffer.GetPtr();
            if (this.helper != ptr)
            {
                throw new ArgumentException("DynamicHashMap was not passed by ref when doing a resize and is now invalid");
            }
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
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
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

    internal sealed unsafe class DynamicHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicHashMapHelper<TKey>* helper;

        public DynamicHashMapDebuggerTypeProxy(DynamicHashMap<TKey, TValue> target)
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
