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

        public readonly bool IsCreated => this.buffer.IsCreated;

        public readonly bool IsEmpty
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return !this.IsCreated || this.helper->Count == 0;
            }
        }

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

        /// <summary>
        /// Capacity cannot shrink.
        /// </summary>
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

        /// <summary>
        /// Must be used and stored by ref.
        /// </summary>
        public ref TC1 Column1 => ref this.helper->Column1;

        /// <summary>
        /// Must be used and stored by ref.
        /// </summary>
        public ref TC2 Column2 => ref this.helper->Column2;

        internal DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>* Helper => this.helper;

        public readonly void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        public bool TryAdd(TKey key, TValue item, T1 column1, T2 column2)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.TryAdd(this.buffer, ref this.helper, key, item, column1, column2);
            return idx != -1;
        }

        public void Add(TKey key, TValue item, T1 column1, T2 column2)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.AddUnique(this.buffer, ref this.helper, key, item, column1, column2);
        }

        public readonly bool Remove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return this.helper->Remove(key);
        }

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

        public readonly bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Find(key) != -1;
        }

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

            public static KVC Null => new() { Index = -1 };

            /// <summary>
            /// Returns default(TKey) for a null KeyValue.
            /// </summary>
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

            public KVC Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.enumerator.GetCurrent();
            }

            object IEnumerator.Current => this.Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return this.enumerator.MoveNext();
            }

            public void Reset()
            {
                this.enumerator.Reset();
            }

            public void Dispose()
            {
            }
        }

        public readonly Enumerator GetEnumerator()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return new Enumerator(this.helper);
        }

        /// <summary>
        /// Not implemented; use the concrete GetEnumerator instead.
        /// </summary>
        IEnumerator<KVC> IEnumerable<KVC>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented; use the concrete GetEnumerator instead.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
