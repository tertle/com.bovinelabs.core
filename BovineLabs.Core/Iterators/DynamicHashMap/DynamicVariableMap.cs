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
                DynamicVariableMapHelper<TKey, TValue, T, TC>.Resize(this.buffer, ref this.helper, value);
            }
        }

        /// <summary>
        /// Must be used and stored by ref.
        /// </summary>
        public ref TC Column => ref this.helper->Column;

        internal DynamicVariableMapHelper<TKey, TValue, T, TC>* Helper => this.helper;

        public readonly void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        public bool TryAdd(TKey key, TValue item, T column)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = DynamicVariableMapHelper<TKey, TValue, T, TC>.TryAdd(this.buffer, ref this.helper, key, item, column);
            return idx != -1;
        }

        public void Add(TKey key, TValue item, T column)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            DynamicVariableMapHelper<TKey, TValue, T, TC>.AddUnique(this.buffer, ref this.helper, key, item, column);
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

        public readonly bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Find(key) != -1;
        }

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
