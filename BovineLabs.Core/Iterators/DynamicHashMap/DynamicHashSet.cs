// <copyright file="DynamicHashSet.cs" company="BovineLabs">
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

    [DebuggerTypeProxy(typeof(DynamicHashSetDebuggerTypeProxy<>))]
    public unsafe struct DynamicHashSet<T> : IEnumerable<T>
        where T : unmanaged, IEquatable<T>
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly bool readOnly;
#endif

        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<T>* helper;

        internal DynamicHashSet(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.readOnly = this.buffer.m_IsReadOnly == 1;
            this.helper = this.readOnly ? buffer.AsHelperReadOnly<T>() : buffer.AsHelper<T>();
#else
            this.helper = buffer.AsHelperReadOnly<T>();
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
                DynamicHashMapHelper<T>.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal DynamicHashMapHelper<T>* Helper => this.helper;

        /// <summary> Removes all key-value pairs. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public void Clear()
        {
            this.CheckWrite();
            this.helper->Clear();
        }

        /// <summary>
        /// Adds a new value (unless it is already present).
        /// </summary>
        /// <param name="item">The value to add.</param>
        /// <returns>True if the value was not already present.</returns>
        public bool Add(T item)
        {
            this.CheckWrite();
            return DynamicHashMapHelper<T>.TryAdd(this.buffer, ref this.helper, item) != -1;
        }

        /// <summary> Removes a particular value. </summary>
        /// <param name="item">The key to remove.</param>
        /// <returns>True if a key-value pair was removed.</returns>
        public bool Remove(T item)
        {
            return this.helper->TryRemove(item) != -1;
        }

        /// <summary> Returns true if a particular value is present. </summary>
        /// <param name="item">The item to look up.</param>
        /// <returns>True if the value was present.</returns>
        public bool Contains(T item)
        {
            return this.helper->Find(item) != -1;
        }

        /// <summary>
        /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess()
        {
            this.CheckWrite();
            DynamicHashMapHelper<T>.TrimExcess(this.buffer, ref this.helper);
        }

        // public void AddBatchUnsafe(NativeArray<T> keys, NativeArray<TValue> values)
        // {
        //     CheckLengthsMatch(keys.Length, values.Length);
        //     this.AddBatchUnsafe((T*)keys.GetUnsafeReadOnlyPtr(), (TValue*)values.GetUnsafeReadOnlyPtr(), keys.Length);
        // }
        //
        // public void AddBatchUnsafe(T* keys, TValue* values, int length)
        // {
        //     this.CheckWrite();
        //     DynamicHashMapHelper<T>.AddBatchUnsafe(this.buffer, ref this.helper, keys, (byte*)values, length);
        // }

        /// <summary> Returns an array with a copy of this set's values (in no particular order). </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of the set's values.</returns>
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            return this.helper->GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public DynamicHashSetEnumerator<T> GetEnumerator()
        {
            return new DynamicHashSetEnumerator<T>(this.helper);
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
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

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicHashMapHelper<T>>())
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
                throw new ArgumentException($"Trying to write to a readonly dynamicHashMap");
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void ThrowKeyNotPresent(T key)
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

    internal sealed unsafe class DynamicHashSetDebuggerTypeProxy<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly DynamicHashMapHelper<T>* helper;

        public DynamicHashSetDebuggerTypeProxy(DynamicHashSet<T> target)
        {
            this.helper = target.Helper;
        }

        public List<T> Items
        {
            get
            {
                var result = new List<T>();

                if (this.helper == null)
                {
                    return result;
                }

                using var items = this.helper->GetKeyArray(Allocator.Temp);

                for (var i = 0; i < items.Length; ++i)
                {
                    result.Add(items[i]);
                }

                return result;
            }
        }
    }
}
