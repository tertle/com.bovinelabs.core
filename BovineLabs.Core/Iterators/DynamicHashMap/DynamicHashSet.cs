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
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicHashSetDebuggerTypeProxy<>))]
    public unsafe struct DynamicHashSet<T> : IEnumerable<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<T>* helper;

        internal DynamicHashSet(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsHelper<T>();
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
                DynamicHashMapHelper<T>.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal readonly DynamicHashMapHelper<T>* Helper => this.helper;

        /// <summary> Removes all key-value pairs. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public readonly void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        /// <summary>
        /// Adds a new value (unless it is already present).
        /// </summary>
        /// <param name="item"> The value to add. </param>
        /// <returns> True if the value was not already present. </returns>
        public bool Add(T item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return DynamicHashMapHelper<T>.TryAdd(this.buffer, ref this.helper, item) != -1;
        }

        /// <summary> Removes a particular value. </summary>
        /// <param name="item"> The key to remove. </param>
        /// <returns> True if a key-value pair was removed. </returns>
        public readonly bool Remove(T item)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryRemove(item) != -1;
        }

        /// <summary> Returns true if a particular value is present. </summary>
        /// <param name="item"> The item to look up. </param>
        /// <returns> True if the value was present. </returns>
        public readonly bool Contains(T item)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Find(item) != -1;
        }

        /// <summary> Removes holes. </summary>
        public void Flatten()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicHashMapHelper<T>.Flatten(this.buffer, ref this.helper);
        }

        /// <summary> Returns an array with a copy of this set's values (in no particular order). </summary>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> An array with a copy of the set's values. </returns>
        public readonly NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns> An enumerator over the key-value pairs of this hash map. </returns>
        public readonly DynamicHashSetEnumerator<T> GetEnumerator()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return new DynamicHashSetEnumerator<T>(this.helper);
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator" /> instead.
        /// </summary>
        /// <returns> Throws NotImplementedException. </returns>
        /// <exception cref="NotImplementedException"> Method is not implemented. </exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
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
                throw new ArgumentException("DynamicHashSet was not passed by ref when doing a resize and is now invalid");
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

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicHashMapHelper<T>>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
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
