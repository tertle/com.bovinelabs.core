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
            data = default;
            data.Init(initialCapacity, sizeof(TValue), minGrowth, allocator);
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data.IsCreated;
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsCreated || data.IsEmpty;
        }

        public UnsafeHashMapBucketData<TKey, TValue> UnsafeBucketData => new((TValue*)data.Ptr, data.Keys, data.Next, data.Buckets);

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data.Count;
        }

        /// <summary>
        /// Capacity cannot shrink.
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => data.Capacity;

            set => data.Resize(value);
        }

        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            data.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!IsCreated)
            {
                return inputDeps;
            }

            var jobHandle = CollectionAccess.ScheduleDispose(data.Ptr, data.Allocator, inputDeps);

            data = default;

            return jobHandle;
        }

        public void Clear()
        {
            data.Clear();
        }

        public void Add(TKey key, TValue item)
        {
            var idx = data.AddNoFind(key);
            UnsafeUtility.WriteArrayElement(data.Ptr, idx, item);
        }

        public void AddNoResize(TKey key, TValue item)
        {
            var idx = data.AddNoFindNoResize(key);
            UnsafeUtility.WriteArrayElement(data.Ptr, idx, item);
        }

        public void AddLinear(TKey key, TValue item)
        {
            var idx = data.AddLinearNoResize(key);
            UnsafeUtility.WriteArrayElement(data.Ptr, idx, item);
        }

        public int Remove(TKey key)
        {
            return data.Remove(key);
        }

        public bool TryGetFirstValue(TKey key, out TValue item, out HashMapIterator<TKey> it)
        {
            return data.TryGetFirstValue(key, out item, out it);
        }

        public bool TryGetNextValue(out TValue item, ref HashMapIterator<TKey> it)
        {
            return data.TryGetNextValue(out item, ref it);
        }

        public bool ContainsKey(TKey key)
        {
            return data.Find(key) != -1;
        }

        public void TrimExcess()
        {
            data.TrimExcess();
        }

        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            return data.GetKeyArray(allocator);
        }

        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            return data.GetValueArray<TValue>(allocator);
        }

        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            return data.GetKeyValueArrays<TValue>(allocator);
        }

        public UnsafeHashMap<TKey, TValue>.Enumerator GetEnumerator()
        {
            fixed (HashMapHelper<TKey>* data = &this.data)
            {
                return CollectionAccess.CreateUnsafeEnumerator<TKey, TValue>(data);
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
        private HashMapHelper<TKey> _data;

        public UnsafeMultiHashMapDebuggerTypeProxy(UnsafeMultiHashMap<TKey, TValue> target)
        {
            _data = target.data;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();
                using var kva = _data.GetKeyValueArrays<TValue>(Allocator.Temp);

                for (var i = 0; i < kva.Length; ++i)
                {
                    result.Add(new Pair<TKey, TValue>(kva.Keys[i], kva.Values[i]));
                }

                return result;
            }
        }
    }
}
