// <copyright file="DynamicUntypedHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct DynamicUntypedHashMap<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicUntypedHashMapHelper<TKey>* helper;

        internal DynamicUntypedHashMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsUntypedHelper<TKey>();
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
                DynamicUntypedHashMapHelper<TKey>.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal DynamicUntypedHashMapHelper<TKey>* Helper => this.helper;

        public void Add<TValue>(TKey key, TValue item)
            where TValue : unmanaged
        {
            DynamicUntypedHashMapHelper<TKey>.AddUnique(this.buffer, ref this.helper, key, item);
        }

        /// <summary> Adds or sets a key-value pair. </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <typeparam name="TValue"> The type of value. </typeparam>
        public void AddOrSet<TValue>(TKey key, TValue item)
            where TValue : unmanaged
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicUntypedHashMapHelper<TKey>.AddOrSet(this.buffer, ref this.helper, key, item);
        }

        /// <summary> Adds or sets a key-value pair using raw bytes. </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="value"> Pointer to the value data. </param>
        /// <param name="length"> The size in bytes. </param>
        public void AddOrSet(TKey key, void* value, int length)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicUntypedHashMapHelper<TKey>.AddOrSetRaw(this.buffer, ref this.helper, key, value, length);
        }

        [Obsolete("Use GetOrAddRefUnsafe")]
        public ref TValue GetOrAddRef<TValue>(TKey key, TValue defaultValue = default)
            where TValue : unmanaged
        {
            return ref this.GetOrAddRefUnsafe(key, defaultValue);
        }

        /// <summary> Gets a value if it exists otherwise adds it then returns it by ref. </summary>
        /// <remarks>
        /// Unsafe because the returned ref points directly into the hash map storage. Consume it immediately and do not keep or use it after any later
        /// write to the same hash map, such as add-or-set, get-or-add, clear, or capacity-changing operations.
        /// </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="defaultValue"> Value to use if the key doesn't exist. </param>
        /// <typeparam name="TValue"> The type of value. </typeparam>
        /// <returns> The value in the map. </returns>
        public ref TValue GetOrAddRefUnsafe<TValue>(TKey key, TValue defaultValue = default)
            where TValue : unmanaged
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicUntypedHashMapHelper<TKey>.AddUnique(this.buffer, ref this.helper, key, defaultValue);
            }

            return ref DynamicUntypedHashMapHelper<TKey>.GetValue<TValue>(this.helper, idx);
        }

        /// <summary> Gets a value if it exists otherwise adds it then returns it by pointer. </summary>
        /// <param name="key"> The key to add. </param>
        /// <param name="defaultValue"> Pointer to the value data. </param>
        /// <param name="length"> The size in bytes. </param>
        /// <param name="storedLength"> Outputs the size in bytes. </param>
        /// <returns> Pointer to the value in the map. </returns>
        public byte* GetOrAddRaw(TKey key, void* defaultValue, int length, out int storedLength)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicUntypedHashMapHelper<TKey>.AddUniqueRaw(this.buffer, ref this.helper, key, defaultValue, length);
            }

            return DynamicUntypedHashMapHelper<TKey>.GetValueRaw(this.helper, idx, out storedLength);
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <returns> True if the key was present. </returns>
        /// <typeparam name="TValue"> The type of value. </typeparam>
        public readonly bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetValue(key, out item);
        }

        /// <summary> Returns the raw value associated with a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="value"> Outputs pointer to the value data. Outputs null if the key was not present. </param>
        /// <param name="length"> Outputs the size in bytes. </param>
        /// <returns> True if the key was present. </returns>
        public readonly bool TryGetValue(TKey key, out byte* value, out int length)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetValueRaw(key, out value, out length);
        }

        public readonly bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            return idx != -1;
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

        /// <summary> Returns an array with a copy of all this hash map's keys (in no particular order). </summary>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> An array with a copy of all this hash map's keys (in no particular order). </returns>
        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetKeyArray(allocator);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            if (this.helper != this.buffer.GetPtr())
            {
                throw new ArgumentException("DynamicUntypedHashMap was not passed by ref when doing a resize and is now invalid");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicUntypedHashMapHelper<TKey>>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }
    }
}
