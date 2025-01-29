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
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicMultiHashMapDebuggerTypeProxy<,>))]
    public unsafe struct DynamicMultiHashMap<TKey, TValue> : IEnumerable<KVPair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<TKey>* helper;

        internal DynamicMultiHashMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsHelper<TKey>();
        }

        /// <summary> Gets a value indicating whether whether this hash map has been allocated (and not yet deallocated). </summary>
        /// <value> True if this hash map has been allocated (and not yet deallocated). </value>
        public readonly bool IsCreated => this.buffer.IsCreated;

        /// <summary> Gets a value indicating whether whether this hash map is empty. </summary>
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
        /// <remarks> If the key is already present, this method throws without modifying the hash map. </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <exception cref="ArgumentException"> Thrown if the key was already present. </exception>
        public void Add(TKey key, TValue item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            var idx = DynamicHashMapHelper<TKey>.AddMulti(this.buffer, ref this.helper, key);
            UnsafeUtility.WriteArrayElement(this.helper->Values, idx, item);
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <returns> The number of elements removed. </returns>
        public readonly int Remove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return this.helper->Remove(key);
        }

        /*/// <summary> Returns the value associated with a key. </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>*/
        public readonly bool TryGetFirstValue(TKey key, out TValue item, out HashMapIterator<TKey> it)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetFirstValue(key, out item, out it);
        }

        public readonly bool TryGetNextValue(out TValue item, ref HashMapIterator<TKey> it)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetNextValue(out item, ref it);
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

        /// <summary>
        /// Returns true if a given key and value combination is present in this hash map.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="value"> The value to look up. </param>
        /// <typeparam name="T"> Type type of the value, should match <see cref="TValue" /> it just has an extra IEquatable constraint. </typeparam>
        /// <returns> True if the key and value combination was present. </returns>
        public readonly bool Contains<T>(TKey key, T value)
            where T : unmanaged, IEquatable<TValue>
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
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

        public readonly int CountValuesForKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            var count = 0;
            var e = this.GetValuesForKey(key);
            while (e.MoveNext())
            {
                count++;
            }

            return count;
        }

        /// <summary> Removes holes. </summary>
        public void Flatten()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicHashMapHelper<TKey>.Flatten(this.buffer, ref this.helper);
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

        public readonly DynamicHashMapKeyEnumerator<TKey, TValue> GetValuesForKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return new DynamicHashMapKeyEnumerator<TKey, TValue>
            {
                hashmap = this,
                key = key,
                isFirst = 1,
            };
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
            if (this.helper != this.buffer.GetPtr())
            {
                throw new ArgumentException("DynamicMultiHashMap was not passed by ref when doing a resize and is now invalid");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
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
