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
        private readonly DynamicBuffer<byte> _buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>* _helper;

        internal DynamicVariableMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            _buffer = buffer;
            _helper = buffer.AsVariableHelper<TKey, TValue, T1, TC1, T2, TC2>();
        }

        public readonly bool IsCreated => _buffer.IsCreated;

        public readonly bool IsEmpty
        {
            get
            {
                _buffer.CheckReadAccess();
                RefCheck();
                return !IsCreated || _helper->Count == 0;
            }
        }

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                _buffer.CheckReadAccess();
                RefCheck();
                return _helper->Count;
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
                _buffer.CheckReadAccess();
                RefCheck();
                return _helper->Capacity;
            }

            set
            {
                _buffer.CheckWriteAccess();
                RefCheck();
                DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.Resize(_buffer, ref _helper, value);
            }
        }

        /// <summary>
        /// Must be used and stored by ref.
        /// </summary>
        public ref TC1 Column1 => ref _helper->Column1;

        /// <summary>
        /// Must be used and stored by ref.
        /// </summary>
        public ref TC2 Column2 => ref _helper->Column2;

        internal DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>* Helper => _helper;

        public readonly void Clear()
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->Clear();
        }

        public bool TryAdd(TKey key, TValue item, T1 column1, T2 column2)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var idx = DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.TryAdd(_buffer, ref _helper, key, item, column1, column2);
            return idx != -1;
        }

        public void Add(TKey key, TValue item, T1 column1, T2 column2)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.AddUnique(_buffer, ref _helper, key, item, column1, column2);
        }

        public readonly bool Remove(TKey key)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            return _helper->Remove(key);
        }

        public readonly void RemoveAt(int idx)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->RemoveAt(idx);
        }

        public readonly ref TValue Replace(TKey key, T1 column1, T2 column2)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var idx = _helper->Find(key);
            if (idx == -1)
            {
                ThrowKeyNotPresent(key);
            }

            _helper->Column1.Replace(column1, idx);
            _helper->Column2.Replace(column2, idx);
            return ref UnsafeUtility.ArrayElementAsRef<TValue>(_helper->Values, idx);
        }

        public readonly void ReplaceColumn1(int idx, T1 column)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->Column1.Replace(column, idx);
        }

        public readonly void ReplaceColumn2(int idx, T2 column)
        {
            _buffer.CheckWriteAccess();
            RefCheck();
            _helper->Column2.Replace(column, idx);
        }

        public void AddOrReplace(TKey key, TValue value, T1 column1, T2 column2)
        {
            _buffer.CheckWriteAccess();
            RefCheck();

            var idx = _helper->Find(key);
            if (idx == -1)
            {
                Add(key, value, column1, column2);
            }
            else
            {
                _helper->Column1.Replace(column1, idx);
                _helper->Column2.Replace(column2, idx);
                UnsafeUtility.WriteArrayElement(_helper->Values, idx, value);
            }
        }

        public readonly bool ContainsKey(TKey key)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->Find(key) != -1;
        }

        public readonly bool TryGetValue(TKey key, out TValue item, out T1 column1, out T2 column2)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->TryGetValue(key, out item, out column1, out column2);
        }

        public readonly int TryGetIndex(TKey key)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->Find(key);
        }

        public readonly TKey GetKeyAtIndex(int index)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->GetKeyAtIndex(index);
        }

        public readonly ref TValue GetValueAtIndex(int index)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return ref _helper->GetValueAtIndex(index);
        }

        public readonly T1 GetColumn1AtIndex(int index)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->GetColumn1AtIndex(index);
        }

        public readonly T2 GetColumn2AtIndex(int index)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return _helper->GetColumn2AtIndex(index);
        }

        public readonly void GetAtIndex(int index, out TKey key, out TValue item, out T1 column1, out T2 column2)
        {
            _buffer.CheckReadAccess();
            RefCheck();
            _helper->GetValueAtIndex(index, out key, out item, out column1, out column2);
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
            var ptr = _buffer.GetPtr();
            if (_helper != ptr)
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
                    if (Index != -1)
                    {
                        return Data->KeyHash.Keys[Index];
                    }

                    return default;
                }
            }

            public ref TValue Value
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                    if (Index == -1)
                    {
                        throw new ArgumentException("must be valid");
                    }
#endif

                    return ref UnsafeUtility.AsRef<TValue>(Data->Values + Index);
                }
            }

            public T1 Column1
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                    if (Index == -1)
                    {
                        throw new ArgumentException("must be valid");
                    }
#endif

                    return Data->Column1.GetValue(Index);
                }
            }

            public T2 Column2
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                    if (Index == -1)
                    {
                        throw new ArgumentException("must be valid");
                    }
#endif

                    return Data->Column2.GetValue(Index);
                }
            }
        }

        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Enumerator : IEnumerator<KVC>
        {
            [NativeDisableUnsafePtrRestriction]
            private DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.Enumerator _enumerator;

            internal Enumerator(DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>* data)
            {
                _enumerator = new DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.Enumerator(data);
            }

            public KVC Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _enumerator.GetCurrent();
            }

            object IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            public void Dispose()
            {
            }
        }

        public readonly Enumerator GetEnumerator()
        {
            _buffer.CheckReadAccess();
            RefCheck();
            return new Enumerator(_helper);
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
