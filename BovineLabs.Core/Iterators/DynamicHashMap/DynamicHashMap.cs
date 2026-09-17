namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicHashMapDebuggerTypeProxy<,>))]
    public unsafe struct DynamicHashMap<TKey, TValue> : IEnumerable<KVPair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<TKey>* helper;

        internal DynamicHashMap(DynamicBuffer<byte> buffer)
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

        public TValue this[TKey key]
        {
            readonly get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                if (this.TryGetValue(key, out var res))
                {
                    return res;
                }

                ThrowKeyNotPresent(key);

                return default;
            }

            set
            {
                this.buffer.CheckWriteAccess();
                this.RefCheck();

                var idx = this.helper->Find(key);

                if (idx == -1)
                {
                    // Use optimized path that doesn't repeat the Find() call
                    idx = DynamicHashMapHelper<TKey>.AddWithKnownAbsence(this.buffer, ref this.helper, key);
                }

                UnsafeUtility.WriteArrayElement(this.helper->Values, idx, value);
            }
        }

        public readonly void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        public readonly void ClearDense()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->ClearDense();
        }

        public bool TryAdd(TKey key, TValue item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = DynamicHashMapHelper<TKey>.TryAdd(this.buffer, ref this.helper, key);
            if (idx != -1)
            {
                UnsafeUtility.WriteArrayElement(this.helper->Values, idx, item);
                return true;
            }

            return false;
        }

        public void Add(TKey key, TValue item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = DynamicHashMapHelper<TKey>.AddUnique(this.buffer, ref this.helper, key);
            UnsafeUtility.WriteArrayElement(this.helper->Values, idx, item);
        }

        /// <summary>
        /// The returned reference aliases map storage. Consume immediately; any later map write or capacity change invalidates it.
        /// </summary>
        public ref TValue GetOrAddRefUnsafe(TKey key, TValue defaultValue = default)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicHashMapHelper<TKey>.AddWithKnownAbsence(this.buffer, ref this.helper, key);
                UnsafeUtility.WriteArrayElement(this.helper->Values, idx, defaultValue);
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(this.helper->Values, idx);
        }

        /// <summary>
        /// The returned reference aliases map storage. Consume immediately; any later map write or capacity change invalidates it.
        /// </summary>
        public ref TValue GetOrAddRefUnsafe(TKey key, out bool add, TValue defaultValue = default)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicHashMapHelper<TKey>.AddWithKnownAbsence(this.buffer, ref this.helper, key);
                UnsafeUtility.WriteArrayElement(this.helper->Values, idx, defaultValue);
                add = true;
            }
            else
            {
                add = false;
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(this.helper->Values, idx);
        }

        public Ptr<TValue> GetRef(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                return default;
            }

            return new Ptr<TValue>((TValue*)this.helper->Values + idx);
        }

        public readonly TValue GetOrDefault(TKey key, TValue defaultValue = default)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                return defaultValue;
            }

            return UnsafeUtility.ReadArrayElement<TValue>(this.helper->Values, idx);
        }

        public readonly bool Remove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return this.helper->TryRemove(key) != -1;
        }

        public readonly bool TryGetValue(TKey key, out TValue item)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetValue(key, out item);
        }

        public readonly bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Find(key) != -1;
        }

        public void Flatten()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicHashMapHelper<TKey>.Flatten(this.buffer, ref this.helper);
        }

        public void RemoveRangeShiftDown(int index, int range)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->RemoveRangeShiftDown(index, range);
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

        public void AddBatchUnsafe(NativeSlice<TKey> keys, NativeSlice<TValue> values)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            CheckLengthsMatch(keys.Length, values.Length);
            DynamicHashMapHelper<TKey>.AddBatchUnsafe(this.buffer, ref this.helper, keys, values);
        }

        public void AddBatchUnsafe(NativeSlice<TKey> keys, NativeArray<TValue> values)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            CheckLengthsMatch(keys.Length, values.Length);
            DynamicHashMapHelper<TKey>.AddBatchUnsafe(this.buffer, ref this.helper, keys, values);
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

        public readonly DynamicHashMapEnumerator<TKey, TValue> GetEnumerator()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return new DynamicHashMapEnumerator<TKey, TValue>(this.helper);
        }

        public TValue* GetUnsafeValuePtr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return (TValue*)this.helper->Values;
        }

        public TKey* GetUnsafeKeyPtr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Keys;
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

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicHashMapHelper<TKey>>())
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

    internal sealed unsafe class DynamicHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private readonly DynamicHashMapHelper<TKey>* helper;

        public DynamicHashMapDebuggerTypeProxy(DynamicHashMap<TKey, TValue> target)
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
