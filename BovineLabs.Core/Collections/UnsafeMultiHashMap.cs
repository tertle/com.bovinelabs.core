// ReSharper disable once CheckNamespace

namespace Unity.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    /// <summary>
    /// Parallel writes are unsupported; use NativeParallelMultiHashMap instead.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeMultiHashMapDebuggerTypeProxy<,>))]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Exposed for others")]
    public unsafe struct UnsafeMultiHashMap<TKey, TValue> : INativeDisposable, IEnumerable<KVPair<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal HashMapHelper<TKey> data;

        public UnsafeMultiHashMap(int initialCapacity, AllocatorManager.AllocatorHandle allocator, int minGrowth = 256)
        {
            this.data = default;
            this.data.Init(initialCapacity, sizeof(TValue), minGrowth, allocator);
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data.IsCreated;
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !this.IsCreated || this.data.IsEmpty;
        }

        public UnsafeHashMapBucketData<TKey, TValue> UnsafeBucketData => new((TValue*)this.data.Ptr, this.data.Keys, this.data.Next, this.data.Buckets);

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data.Count;
        }

        /// <summary>
        /// Capacity cannot shrink.
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => this.data.Capacity;

            set => this.data.Resize(value);
        }

        public void Dispose()
        {
            if (!this.IsCreated)
            {
                return;
            }

            this.data.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!this.IsCreated)
            {
                return inputDeps;
            }

            var jobHandle = new UnsafeDisposeJob
            {
                Ptr = this.data.Ptr,
                Allocator = this.data.Allocator,
            }.Schedule(inputDeps);

            this.data = default;

            return jobHandle;
        }

        public void Clear()
        {
            this.data.Clear();
        }

        public void Add(TKey key, TValue item)
        {
            var idx = this.data.AddNoFind(key);
            UnsafeUtility.WriteArrayElement(this.data.Ptr, idx, item);
        }

        public void AddNoResize(TKey key, TValue item)
        {
            var idx = this.data.AddNoFindNoResize(key);
            UnsafeUtility.WriteArrayElement(this.data.Ptr, idx, item);
        }

        public void AddLinear(TKey key, TValue item)
        {
            var idx = this.data.AddLinearNoResize(key);
            UnsafeUtility.WriteArrayElement(this.data.Ptr, idx, item);
        }

        public int Remove(TKey key)
        {
            return this.data.Remove(key);
        }

        public bool TryGetFirstValue(TKey key, out TValue item, out HashMapIterator<TKey> it)
        {
            return this.data.TryGetFirstValue(key, out item, out it);
        }

        public bool TryGetNextValue(out TValue item, ref HashMapIterator<TKey> it)
        {
            return this.data.TryGetNextValue(out item, ref it);
        }

        public bool ContainsKey(TKey key)
        {
            return this.data.Find(key) != -1;
        }

        public void TrimExcess()
        {
            this.data.TrimExcess();
        }

        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            return this.data.GetKeyArray(allocator);
        }

        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            return this.data.GetValueArray<TValue>(allocator);
        }

        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            return this.data.GetKeyValueArrays<TValue>(allocator);
        }

        public UnsafeHashMap<TKey, TValue>.Enumerator GetEnumerator()
        {
            fixed (HashMapHelper<TKey>* data = &this.data)
            {
                return new UnsafeHashMap<TKey, TValue>.Enumerator { m_Enumerator = new HashMapHelper<TKey>.Enumerator(data) };
            }
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
    }

    public static unsafe class UnsafeMultiHashMapExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearLengthBuckets<TKey, TValue>(this ref UnsafeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.data.ClearLengthBuckets();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RecalculateBuckets<TKey, TValue>(this ref UnsafeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.data.RecalculateBuckets();
        }

        /// <summary>
        /// Does not update Count; set it separately.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReserveAtomicNoResize<TKey, TValue>(this ref UnsafeMultiHashMap<TKey, TValue> hashMap, int length)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.data.ReserveAtomicNoResize(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCount<TKey, TValue>(this ref UnsafeMultiHashMap<TKey, TValue> hashMap, int count)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            hashMap.data.SetCount(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TKey* GetKeys<TKey, TValue>(this in UnsafeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.data.Keys;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue* GetValues<TKey, TValue>(this in UnsafeMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return (TValue*)hashMap.data.Ptr;
        }
    }

    internal sealed class UnsafeMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private HashMapHelper<TKey> Data;

        public UnsafeMultiHashMapDebuggerTypeProxy(UnsafeMultiHashMap<TKey, TValue> target)
        {
            this.Data = target.data;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();
                using var kva = this.Data.GetKeyValueArrays<TValue>(Allocator.Temp);

                for (var i = 0; i < kva.Length; ++i)
                {
                    result.Add(new Pair<TKey, TValue>(kva.Keys[i], kva.Values[i]));
                }

                return result;
            }
        }
    }
}
