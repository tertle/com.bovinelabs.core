// <copyright file="DynamicExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class DynamicExtensions
    {
        private const int DefaultMinGrowth = 256;

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

        public static DynamicBuffer<TBuffer> InitializeHashSet<TBuffer, TKey>(this DynamicBuffer<TBuffer> buffer, int capacity = 0, int minGrowth = DefaultMinGrowth)
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

        public static DynamicHashSet<T> AsHashSet<TBuffer, T>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicHashSet<T>
            where T : unmanaged, IEquatable<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, sizeof(TBuffer));
#endif
            return new DynamicHashSet<T>(buffer.Reinterpret<byte>());
        }

        public static DynamicPerfectHashMap<TKey, TValue> AsPerfectHashMap<TBuffer, TKey, TValue>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicPerfectHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, sizeof(TBuffer));
#endif
            return new DynamicPerfectHashMap<TKey, TValue>(buffer.Reinterpret<byte>());
        }

        internal static DynamicHashMapHelper<TKey>* AsHelper<TKey>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
        {
            CheckSize<DynamicHashMapHelper<TKey>>(buffer);
            return (DynamicHashMapHelper<TKey>*)buffer.GetUnsafePtr();
        }

        internal static DynamicHashMapHelper<TKey>* AsHelperReadOnly<TKey>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
        {
            CheckSize<DynamicHashMapHelper<TKey>>(buffer);
            return (DynamicHashMapHelper<TKey>*)buffer.GetUnsafeReadOnlyPtr();
        }

        internal static DynamicPerfectHashMapHelper<TKey, TValue>* AsHelper<TKey, TValue>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckSize<DynamicHashMapHelper<TKey>>(buffer);
            return (DynamicPerfectHashMapHelper<TKey, TValue>*)buffer.GetUnsafePtr();
        }

        internal static DynamicPerfectHashMapHelper<TKey, TValue>* AsHelperReadOnly<TKey, TValue>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckSize<DynamicHashMapHelper<TKey>>(buffer);
            return (DynamicPerfectHashMapHelper<TKey, TValue>*)buffer.GetUnsafeReadOnlyPtr();
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
    }
}
