namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
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

        public readonly bool IsCreated => this.buffer.IsCreated;

        public readonly bool IsEmpty
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return !this.IsCreated || this.helper->IsEmpty;
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
                DynamicHashMapHelper<TKey>.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal DynamicHashMapHelper<TKey>* Helper => this.helper;

        public readonly void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        public void Add(TKey key, TValue item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            var idx = DynamicHashMapHelper<TKey>.AddMulti(this.buffer, ref this.helper, key);
            UnsafeUtility.WriteArrayElement(this.helper->Values, idx, item);
        }

        public bool TryAddUniquePair<T>(TKey key, T item)
            where T : unmanaged, IEquatable<TValue>
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            CheckValueSize<T>();

            if (this.helper->TryGetFirstValue(key, out TValue value, out var it))
            {
                do
                {
                    if (item.Equals(value))
                    {
                        return false;
                    }
                }
                while (this.helper->TryGetNextValue(out value, ref it));
            }

            var idx = DynamicHashMapHelper<TKey>.AddMulti(this.buffer, ref this.helper, key);
            UnsafeUtility.WriteArrayElement(this.helper->Values, idx, item);
            return true;
        }

        public readonly int Remove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return this.helper->Remove(key);
        }

        public readonly void Remove(HashMapIterator<TKey> it)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Remove(it);
        }

        /// <summary>
        /// Removes only one occurrence of the exact key-value pair.
        /// </summary>
        public readonly bool Remove<T>(TKey key, T value)
            where T : unmanaged, IEquatable<TValue>
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            CheckValueSize<T>();

            if (!this.helper->TryGetFirstValue(key, out TValue item, out var it))
            {
                return false;
            }

            do
            {
                if (!value.Equals(item))
                {
                    continue;
                }

                this.helper->Remove(it);
                return true;
            }
            while (this.helper->TryGetNextValue(out item, ref it));

            return false;
        }

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

        public readonly bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Find(key) != -1;
        }

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

        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetKeyArray(allocator);
        }

        public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetValueArray<TValue>(allocator);
        }

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

        public readonly DynamicHashMapEnumerator<TKey, TValue> GetEnumerator()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return new DynamicHashMapEnumerator<TKey, TValue>(this.helper);
        }

        /// <summary>
        /// Not implemented; use the concrete GetEnumerator instead.
        /// </summary>
        IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckValueSize<T>()
            where T : unmanaged
        {
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<TValue>())
            {
                throw new InvalidOperationException("DynamicMultiHashMap exact-pair operation value type must match the map value type size.");
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
