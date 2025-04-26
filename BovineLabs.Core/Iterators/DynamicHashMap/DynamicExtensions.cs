// <copyright file="DynamicExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class DynamicExtensions
    {
        private const int DefaultMinGrowth = 64;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicBuffer<TBuffer> InitializeHashMap<TBuffer, TKey, TValue>(
            this DynamicBuffer<TBuffer> buffer, int capacity = 0, int minGrowth = DefaultMinGrowth)
            where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif

            var bytes = buffer.Reinterpret<byte>();
            DynamicHashMapHelper<TKey>.Init(bytes, capacity, sizeof(TValue), minGrowth);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicBuffer<TBuffer> InitializeMultiHashMap<TBuffer, TKey, TValue>(
            this DynamicBuffer<TBuffer> buffer, int capacity = 0, int minGrowth = DefaultMinGrowth)
            where TBuffer : unmanaged, IDynamicMultiHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif

            var bytes = buffer.Reinterpret<byte>();
            DynamicHashMapHelper<TKey>.Init(bytes, capacity, sizeof(TValue), minGrowth);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicBuffer<TBuffer> InitializeHashSet<TBuffer, TKey>(
            this DynamicBuffer<TBuffer> buffer, int capacity = 0, int minGrowth = DefaultMinGrowth)
            where TBuffer : unmanaged, IDynamicHashSet<TKey>
            where TKey : unmanaged, IEquatable<TKey>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif

            var bytes = buffer.Reinterpret<byte>();
            DynamicHashMapHelper<TKey>.Init(bytes, capacity, 0, minGrowth);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicBuffer<TBuffer> InitializePerfectHashMap<TBuffer, TKey, TValue>(
            this DynamicBuffer<TBuffer> buffer, NativeHashMap<TKey, TValue> map, TValue nullValue)
            where TBuffer : unmanaged, IDynamicPerfectHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif

            var bytes = buffer.Reinterpret<byte>();
            DynamicPerfectHashMapHelper<TKey, TValue>.Init(bytes, map, nullValue);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicBuffer<TBuffer> InitializePerfectHashMap<TBuffer, TKey, TValue>(
            this DynamicBuffer<TBuffer> buffer, DynamicHashMap<TKey, TValue> map, TValue nullValue)
            where TBuffer : unmanaged, IDynamicPerfectHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif

            var bytes = buffer.Reinterpret<byte>();
            DynamicPerfectHashMapHelper<TKey, TValue>.Init(bytes, map, nullValue);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicBuffer<TBuffer> InitializePerfectHashMap<TBuffer, TKey, TValue>(
            this DynamicBuffer<TBuffer> buffer, NativeArray<TKey> keys, NativeArray<TValue> values, TValue nullValue)
            where TBuffer : unmanaged, IDynamicPerfectHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif

            var bytes = buffer.Reinterpret<byte>();
            DynamicPerfectHashMapHelper<TKey, TValue>.Init(bytes, keys, values, nullValue);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicBuffer<TBuffer> InitializeUntypedHashMap<TBuffer, TKey>(
            this DynamicBuffer<TBuffer> buffer, int capacity = 0, int minGrowth = DefaultMinGrowth)
            where TBuffer : unmanaged, IDynamicUntypedHashMap<TKey>
            where TKey : unmanaged, IEquatable<TKey>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif

            var bytes = buffer.Reinterpret<byte>();
            DynamicUntypedHashMapHelper<TKey>.Init(bytes, capacity, capacity, minGrowth);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicBuffer<TBuffer> InitializeIndexed<TBuffer, TKey, TIndex, TValue>(
            this DynamicBuffer<TBuffer> buffer, int capacity = 0, int minGrowth = DefaultMinGrowth)
            where TBuffer : unmanaged, IDynamicIndexedMap<TKey, TIndex, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TIndex : unmanaged, IEquatable<TIndex>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif

            var bytes = buffer.Reinterpret<byte>();
            DynamicIndexedMapHelper<TKey, TIndex, TValue>.Init(bytes, capacity, minGrowth);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicHashMap<TKey, TValue> AsHashMap<TBuffer, TKey, TValue>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, sizeof(TBuffer));
#endif
            return new DynamicHashMap<TKey, TValue>(buffer.Reinterpret<byte>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicMultiHashMap<TKey, TValue> AsMultiHashMap<TBuffer, TKey, TValue>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicMultiHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, sizeof(TBuffer));
#endif
            return new DynamicMultiHashMap<TKey, TValue>(buffer.Reinterpret<byte>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicHashSet<T> AsHashSet<TBuffer, T>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicHashSet<T>
            where T : unmanaged, IEquatable<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, sizeof(TBuffer));
#endif
            return new DynamicHashSet<T>(buffer.Reinterpret<byte>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicPerfectHashMap<TKey, TValue> AsPerfectHashMap<TBuffer, TKey, TValue>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicPerfectHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged, IEquatable<TValue>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, sizeof(TBuffer));
#endif
            return new DynamicPerfectHashMap<TKey, TValue>(buffer.Reinterpret<byte>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicUntypedHashMap<TKey> AsUntypedHashMap<TBuffer, TKey>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicUntypedHashMap<TKey>
            where TKey : unmanaged, IEquatable<TKey>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, sizeof(TBuffer));
#endif
            return new DynamicUntypedHashMap<TKey>(buffer.Reinterpret<byte>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicIndexedMap<TKey, TIndex, TValue> AsIndexedMap<TBuffer, TKey, TIndex, TValue>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicIndexedMap<TKey, TIndex, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TIndex : unmanaged, IEquatable<TIndex>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, sizeof(TBuffer));
#endif
            return new DynamicIndexedMap<TKey, TIndex, TValue>(buffer.Reinterpret<byte>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DynamicHashMapHelper<TKey>* AsHelper<TKey>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
        {
            CheckSize<DynamicHashMapHelper<TKey>>(buffer);
            return (DynamicHashMapHelper<TKey>*)buffer.GetPtr();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DynamicUntypedHashMapHelper<TKey>* AsUntypedHelper<TKey>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
        {
            CheckSize<DynamicUntypedHashMapHelper<TKey>>(buffer);
            return (DynamicUntypedHashMapHelper<TKey>*)buffer.GetPtr();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DynamicPerfectHashMapHelper<TKey, TValue>* AsHelper<TKey, TValue>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckSize<DynamicPerfectHashMapHelper<TKey, TValue>>(buffer);
            return (DynamicPerfectHashMapHelper<TKey, TValue>*)buffer.GetPtr();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DynamicIndexedMapHelper<TKey, TIndex, TValue>* AsIndexedHelper<TKey, TIndex, TValue>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TIndex : unmanaged, IEquatable<TIndex>
            where TValue : unmanaged
        {
            CheckSize(buffer, sizeof(DynamicIndexedMapHelper<TKey, TIndex, TValue>));
            return (DynamicIndexedMapHelper<TKey, TIndex, TValue>*)buffer.GetPtr();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize<T>(DynamicBuffer<byte> buffer)
            where T : unmanaged
        {
            if (buffer.Length < sizeof(T))
            {
                throw new InvalidOperationException("Buffer not initialized before use.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize(DynamicBuffer<byte> buffer, int size)
        {
            if (buffer.Length < size)
            {
                throw new InvalidOperationException("Buffer not initialized before use.");
            }
        }
    }
}
