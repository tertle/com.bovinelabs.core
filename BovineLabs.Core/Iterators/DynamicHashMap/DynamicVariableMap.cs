// <copyright file="DynamicVariableMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators.Columns;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Needed for safety.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Defines memory layout")]
    public interface IDynamicVariableMap<TKey, TValue, T, TC> : IBufferElementData
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where T : unmanaged, IEquatable<T>
        where TC : unmanaged, IColumn<T>
    {
        byte Value { get; }
    }

    public unsafe struct DynamicVariableMap<TKey, TValue, T, TC> : IEnumerable<DynamicVariableMap<TKey, TValue, T, TC>.KVC>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where T : unmanaged, IEquatable<T>
        where TC : unmanaged, IColumn<T>
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicVariableMapHelper<TKey, TValue, T, TC>* helper;

        internal DynamicVariableMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsVariableHelper<TKey, TValue, T, TC>();
        }

        /// <summary> Gets a value indicating whether this map has been allocated (and not yet deallocated). </summary>
        /// <value> True if this map has been allocated (and not yet deallocated). </value>
        public readonly bool IsCreated => this.buffer.IsCreated;

        /// <summary> Gets a value indicating whether this map is empty. </summary>
        /// <value> True if this map is empty or if the map has not been constructed. </value>
        public readonly bool IsEmpty
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return !this.IsCreated || this.helper->Count == 0;
            }
        }

        /// <summary> Gets the current number of key-value pairs in this map. </summary>
        /// <returns> The current number of key-value pairs in this map. </returns>
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
                DynamicVariableMapHelper<TKey, TValue, T, TC>.Resize(this.buffer, ref this.helper, value);
            }
        }

        /// <summary> Gets the column. Must be used or stored as a Ref. </summary>
        public ref TC Column => ref this.helper->Column;

        internal DynamicVariableMapHelper<TKey, TValue, T, TC>* Helper => this.helper;

        /// <summary> Removes all key-value pairs. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public readonly void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        /// <summary> Adds a new key-value-column. </summary>
        /// <remarks> If the key is already present, this method returns false without modifying the map. </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <param name="column"> The column to add. </param>
        /// <returns> True if the key-value pair was added. </returns>
        public bool TryAdd(TKey key, TValue item, T column)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = DynamicVariableMapHelper<TKey, TValue, T, TC>.TryAdd(this.buffer, ref this.helper, key, item, column);
            return idx != -1;
        }

        /// <summary> Adds a new key-value-column. </summary>
        /// <remarks> If the key is already present, this method throws without modifying the map. </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <param name="column"> The column to add. </param>
        /// <exception cref="ArgumentException"> Thrown if the key was already present. </exception>
        public void Add(TKey key, TValue item, T column)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            DynamicVariableMapHelper<TKey, TValue, T, TC>.AddUnique(this.buffer, ref this.helper, key, item, column);
        }

        /// <summary> Removes a key-value-index. /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <returns> True if an element was removed. </returns>
        public readonly bool Remove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return this.helper->Remove(key);
        }

        /// <summary> Removes a key-value-index. /// </summary>
        /// <param name="idx"> The index to remove, usually from a Column. </param>
        public readonly void RemoveAt(int idx)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->RemoveAt(idx);
        }

        public readonly ref TValue Replace(TKey key, T column)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                ThrowKeyNotPresent(key);
            }

            this.helper->Column.Replace(column, idx);
            return ref UnsafeUtility.ArrayElementAsRef<TValue>(this.helper->Values, idx);
        }

        public readonly void ReplaceColumn(int idx, T column)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Column.Replace(column, idx);
        }

        public void AddOrReplace(TKey key, TValue value, T column)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                this.Add(key, value, column);
            }
            else
            {
                this.helper->Column.Replace(column, idx);
                UnsafeUtility.WriteArrayElement(this.helper->Values, idx, value);
            }
        }

        /// <summary> Returns true if a given key is present in this map. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <returns> True if the key was present. </returns>
        public readonly bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Find(key) != -1;
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <param name="column"> Outputs the column associated with the key. Outputs default if the key was not present. </param>v
        /// <returns> True if the key was present. </returns>
        public readonly bool TryGetValue(TKey key, out TValue item, out T column)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetValue(key, out item, out column);
        }

        public readonly int TryGetIndex(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Find(key);
        }

        public readonly TKey GetKeyAtIndex(int index)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetKeyAtIndex(index);
        }

        public readonly ref TValue GetValueAtIndex(int index)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return ref this.helper->GetValueAtIndex(index);
        }

        public readonly T GetColumnAtIndex(int index)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetColumnAtIndex(index);
        }

        public readonly void GetAtIndex(int index, out TKey key, out TValue item, out T column)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            this.helper->GetAtIndex(index, out key, out item, out column);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
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

            if (buffer.Length < sizeof(DynamicVariableMapHelper<TKey, TValue, T, TC>))
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }

        [DebuggerDisplay("Key = {Key}, Value = {Value}, Column = {Column}")]
        public struct KVC
        {
            internal DynamicVariableMapHelper<TKey, TValue, T, TC>* Data;
            internal int Index;

            /// <summary> Gets an invalid KeyValue. </summary>
            public static KVC Null => new() { Index = -1 };

            /// <summary> Gets the key. </summary>
            /// <value> The key. If this KeyValue is Null, returns the default of TKey. </value>
            public TKey Key
            {
                get
                {
                    if (this.Index != -1)
                    {
                        return this.Data->KeyHash.Keys[this.Index];
                    }

                    return default;
                }
            }

            /// <summary> Gets the value. </summary>
            public ref TValue Value
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                    if (this.Index == -1)
                    {
                        throw new ArgumentException("must be valid");
                    }
#endif

                    return ref UnsafeUtility.AsRef<TValue>(this.Data->Values + this.Index);
                }
            }

            /// <summary> Gets the index. </summary>
            public T Column
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                    if (this.Index == -1)
                    {
                        throw new ArgumentException("must be valid");
                    }
#endif

                    return this.Data->Column.GetValue(this.Index);
                }
            }


        }

        /// <summary>
        /// An enumerator over the key-value pairs of a container.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current" /> is not valid to read.
        /// From this state, the first <see cref="MoveNext" /> call advances the enumerator to the first key-value pair.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Enumerator : IEnumerator<KVC>
        {
            [NativeDisableUnsafePtrRestriction]
            private DynamicVariableMapHelper<TKey, TValue, T, TC>.Enumerator enumerator;

            internal Enumerator(DynamicVariableMapHelper<TKey, TValue, T, TC>* data)
            {
                this.enumerator = new DynamicVariableMapHelper<TKey, TValue, T, TC>.Enumerator(data);
            }

            /// <summary> The current key-value pair. </summary>
            public KVC Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.enumerator.GetCurrent();
            }

            /// <summary> Gets the element at the current position of the enumerator in the container. </summary>
            object IEnumerator.Current => this.Current;

            /// <summary> Advances the enumerator to the next key-value pair. </summary>
            /// <returns> True if <see cref="Current" /> is valid to read after the call. </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return this.enumerator.MoveNext();
            }

            /// <summary> Resets the enumerator to its initial state. </summary>
            public void Reset()
            {
                this.enumerator.Reset();
            }

            /// <summary> Does nothing. </summary>
            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns> An enumerator over the key-value pairs of this hash map. </returns>
        public readonly Enumerator GetEnumerator()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return new Enumerator(this.helper);
        }

        /// <summary> This method is not implemented. Use <see cref="GetEnumerator" /> instead. </summary>
        /// <returns> Throws NotImplementedException. </returns>
        /// <exception cref="NotImplementedException"> Method is not implemented. </exception>
        IEnumerator<KVC> IEnumerable<KVC>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary> This method is not implemented. Use <see cref="GetEnumerator" /> instead. </summary>
        /// <returns> Throws NotImplementedException. </returns>
        /// <exception cref="NotImplementedException"> Method is not implemented. </exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
