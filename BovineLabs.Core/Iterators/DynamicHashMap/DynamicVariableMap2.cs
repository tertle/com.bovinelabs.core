// <copyright file="DynamicVariableMapTwoColumns.cs" company="BovineLabs">
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
    public interface IDynamicVariableMap<TKey, TValue, T1, TC1, T2, TC2> : IBufferElementData
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where T1 : unmanaged, IEquatable<T1>
        where TC1 : unmanaged, IColumn<T1>
        where T2 : unmanaged, IEquatable<T2>
        where TC2 : unmanaged, IColumn<T2>
    {
        byte Value { get; }
    }

    public unsafe struct DynamicVariableMap<TKey, TValue, T1, TC1, T2, TC2> : IEnumerable<DynamicVariableMap<TKey, TValue, T1, TC1, T2, TC2>.KVC>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where T1 : unmanaged, IEquatable<T1>
        where TC1 : unmanaged, IColumn<T1>
        where T2 : unmanaged, IEquatable<T2>
        where TC2 : unmanaged, IColumn<T2>
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>* helper;

        internal DynamicVariableMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsVariableHelper<TKey, TValue, T1, TC1, T2, TC2>();
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
                DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.Resize(this.buffer, ref this.helper, value);
            }
        }

        /// <summary> Gets the first column. Must be used or stored as a Ref. </summary>
        public ref TC1 Column1 => ref this.helper->Column1;

        /// <summary> Gets the second column. Must be used or stored as a Ref. </summary>
        public ref TC2 Column2 => ref this.helper->Column2;

        internal DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>* Helper => this.helper;

        /// <summary> Removes all key-value pairs. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public readonly void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        /// <summary> Adds a new key-value-columns. </summary>
        /// <remarks> If the key is already present, this method returns false without modifying the map. </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <param name="column1"> The first column to add. </param>
        /// <param name="column2"> The second column to add. </param>
        /// <returns> True if the key-value pair was added. </returns>
        public bool TryAdd(TKey key, TValue item, T1 column1, T2 column2)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.TryAdd(this.buffer, ref this.helper, key, item, column1, column2);
            return idx != -1;
        }

        /// <summary> Adds a new key-value-columns. </summary>
        /// <remarks> If the key is already present, this method throws without modifying the map. </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <param name="column1"> The first column to add. </param>
        /// <param name="column2"> The second column to add. </param>
        /// <exception cref="ArgumentException"> Thrown if the key was already present. </exception>
        public void Add(TKey key, TValue item, T1 column1, T2 column2)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.AddUnique(this.buffer, ref this.helper, key, item, column1, column2);
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

        public readonly ref TValue Replace(TKey key, T1 column1, T2 column2)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                ThrowKeyNotPresent(key);
            }

            this.helper->Column1.Replace(column1, idx);
            this.helper->Column2.Replace(column2, idx);
            return ref UnsafeUtility.ArrayElementAsRef<TValue>(this.helper->Values, idx);
        }

        public readonly void ReplaceColumn1(int idx, T1 column)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Column1.Replace(column, idx);
        }

        public readonly void ReplaceColumn2(int idx, T2 column)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Column2.Replace(column, idx);
        }

        public void AddOrReplace(TKey key, TValue value, T1 column1, T2 column2)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                this.Add(key, value, column1, column2);
            }
            else
            {
                this.helper->Column1.Replace(column1, idx);
                this.helper->Column2.Replace(column2, idx);
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
        /// <param name="column1"> Outputs the first column associated with the key. Outputs default if the key was not present. </param>
        /// <param name="column2"> Outputs the second column associated with the key. Outputs default if the key was not present. </param>
        /// <returns> True if the key was present. </returns>
        public readonly bool TryGetValue(TKey key, out TValue item, out T1 column1, out T2 column2)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetValue(key, out item, out column1, out column2);
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

        public readonly T1 GetColumn1AtIndex(int index)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetColumn1AtIndex(index);
        }

        public readonly T2 GetColumn2AtIndex(int index)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetColumn2AtIndex(index);
        }

        public readonly void GetAtIndex(int index, out TKey key, out TValue item, out T1 column1, out T2 column2)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            this.helper->GetValueAtIndex(index, out key, out item, out column1, out column2);
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

            if (buffer.Length < sizeof(DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>))
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }

        [DebuggerDisplay("Key = {Key}, Value = {Value}, Column1 = {Column1}, Column2 = {Column2}")]
        public struct KVC
        {
            internal DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>* Data;
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

            /// <summary> Gets the first column. </summary>
            public T1 Column1
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                    if (this.Index == -1)
                    {
                        throw new ArgumentException("must be valid");
                    }
#endif

                    return this.Data->Column1.GetValue(this.Index);
                }
            }

            /// <summary> Gets the second column. </summary>
            public T2 Column2
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                    if (this.Index == -1)
                    {
                        throw new ArgumentException("must be valid");
                    }
#endif

                    return this.Data->Column2.GetValue(this.Index);
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
            private DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.Enumerator enumerator;

            internal Enumerator(DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>* data)
            {
                this.enumerator = new DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.Enumerator(data);
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