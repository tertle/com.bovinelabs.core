// <copyright file="DynamicIndexedMap.cs" company="BovineLabs">
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

    [DebuggerTypeProxy(typeof(DynamicIndexedMapDebuggerTypeProxy<,,>))]
    public unsafe struct DynamicIndexedMap<TKey, TIndex, TValue> : IEnumerable<KIV<TKey, TIndex, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex : unmanaged, IEquatable<TIndex>
        where TValue : unmanaged
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicIndexedMapHelper<TKey, TIndex, TValue>* helper;

        internal DynamicIndexedMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsIndexedHelper<TKey, TIndex, TValue>();
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
                DynamicIndexedMapHelper<TKey, TIndex, TValue>.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal DynamicIndexedMapHelper<TKey, TIndex, TValue>* Helper => this.helper;

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
        /// <param name="index"> The index to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <returns> True if the key-value pair was added. </returns>
        public bool TryAdd(TKey key, TIndex index, TValue item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = DynamicIndexedMapHelper<TKey, TIndex, TValue>.TryAdd(this.buffer, ref this.helper, key, index, item);
            return idx != -1;
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks> If the key is already present, this method throws without modifying the hash map. </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="index"> The index to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <exception cref="ArgumentException"> Thrown if the key was already present. </exception>
        public void Add(TKey key, TIndex index, TValue item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            DynamicIndexedMapHelper<TKey, TIndex, TValue>.AddUnique(this.buffer, ref this.helper, key, index, item);
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <returns> The number of elements removed. </returns>
        public readonly bool Remove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return this.helper->Remove(key);
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
            DynamicIndexedMapHelper<TKey, TIndex, TValue>.Flatten(this.buffer, ref this.helper);
        }

        /// <summary>
        /// Removes a range from the hashmap. This should only be called on a hashmap with no holes and you should know whaty ou're doing.
        /// </summary>
        /// <param name="index"> The index to start. </param>
        /// <param name="range"> The range. </param>
        public void UnsafeRemoveRangeShiftDown(int index, int range)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->RemoveRangeShiftDown(index, range);
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="index"> Outputs the value index with the key. Outputs default if the key was not present. </param>v
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <returns> True if the key was present. </returns>
        public readonly bool TryGetValue(TKey key, out TIndex index, out TValue item)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetValue(key, out index, out item);
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="index">The index to look up.</param>
        /// <param name="key">>Outputs the unique key associated with the index. Outputs default if the index was not present.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <param name="it"> The iterator to be used for <see cref="TryGetNextValue"/>. </param>
        /// <returns>True if the key was present.</returns>
        public readonly bool TryGetFirstValue(TIndex index, out TKey key, out TValue item, out HashMapIterator<TIndex> it)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetFirstValue(index, out key, out item, out it);
        }

        /// <summary> Advances an iterator to the next value associated with its key. </summary>
        /// <param name="key">>Outputs the next key.</param>
        /// <param name="item">Outputs the next value.</param>
        /// <param name="it">A reference to the iterator to advance.</param>
        /// <returns>True if the key was present and had another value.</returns>
        public readonly bool TryGetNextValue(out TKey key, out TValue item, ref HashMapIterator<TIndex> it)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetNextValue(out key, out item, ref it);
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns> An enumerator over the key-value pairs of this hash map. </returns>
        public readonly DynamicIndexedMapEnumerator<TKey, TIndex, TValue> GetEnumerator()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return new DynamicIndexedMapEnumerator<TKey, TIndex, TValue>(this.helper);
        }

        public TKey* GetUnsafeKeyPtr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->KeyHash.Keys;
        }

        public TIndex* GetUnsafeIndexPtr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->IndexHash.Keys;
        }

        public TValue* GetUnsafeValuePtr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Values;
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator" /> instead.
        /// </summary>
        /// <returns> Throws NotImplementedException. </returns>
        /// <exception cref="NotImplementedException"> Method is not implemented. </exception>
        IEnumerator<KIV<TKey, TIndex, TValue>> IEnumerable<KIV<TKey, TIndex, TValue>>.GetEnumerator()
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

            if (buffer.Length < sizeof(DynamicIndexedMapHelper<TKey, TIndex, TValue>))
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

    internal sealed unsafe class DynamicIndexedMapDebuggerTypeProxy<TKey, TIndex, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex : unmanaged, IEquatable<TIndex>
        where TValue : unmanaged
    {
        private readonly DynamicIndexedMapHelper<TKey, TIndex, TValue>* helper;

        public DynamicIndexedMapDebuggerTypeProxy(DynamicIndexedMap<TKey, TIndex, TValue> target)
        {
            this.helper = target.Helper;
        }

        public List<Triplet> Items
        {
            get
            {
                var result = new List<Triplet>();

                if (this.helper == null)
                {
                    return result;
                }

                var kva = this.helper->GetArrays(Allocator.Temp);

                for (var i = 0; i < kva.Keys.Length; ++i)
                {
                    result.Add(new Triplet(kva.Keys[i], kva.Indices[i], kva.Values[i]));
                }

                return result;
            }
        }

        internal readonly struct Triplet
        {
            public readonly TKey Key;
            public readonly TIndex Index;
            public readonly TValue Value;

            public Triplet(TKey k, TIndex i, TValue v)
            {
                this.Key = k;
                this.Index = i;
                this.Value = v;
            }

            public override string ToString()
            {
                return $"{this.Key} = {this.Index} = {this.Value}";
            }
        }
    }
}
