// <copyright file="DynamicExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators.Columns;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class DynamicExtensions
    {
        public const int DefaultMinGrowth = 0;

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
        public static DynamicBuffer<TBuffer> InitializeVariableMap<TBuffer, TKey, TValue, T, TC>(
            this DynamicBuffer<TBuffer> buffer, int capacity = 0, int minGrowth = DefaultMinGrowth)
            where TBuffer : unmanaged, IDynamicVariableMap<TKey, TValue, T, TC>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where T : unmanaged, IEquatable<T>
            where TC : unmanaged, IColumn<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif

            var bytes = buffer.Reinterpret<byte>();
            DynamicVariableMapHelper<TKey, TValue, T, TC>.Init(bytes, capacity, minGrowth);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicBuffer<TBuffer> InitializeVariableMap<TBuffer, TKey, TValue, T1, TC1, T2, TC2>(
            this DynamicBuffer<TBuffer> buffer, int capacity = 0, int minGrowth = DefaultMinGrowth)
            where TBuffer : unmanaged, IDynamicVariableMap<TKey, TValue, T1, TC1, T2, TC2>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where T1 : unmanaged, IEquatable<T1>
            where TC1 : unmanaged, IColumn<T1>
            where T2 : unmanaged, IEquatable<T2>
            where TC2 : unmanaged, IColumn<T2>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif

            var bytes = buffer.Reinterpret<byte>();
            DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>.Init(bytes, capacity, minGrowth);
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
        public static DynamicVariableMap<TKey, TValue, T, TC> AsVariableMap<TBuffer, TKey, TValue, T, TC>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicVariableMap<TKey, TValue, T, TC>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where T : unmanaged, IEquatable<T>
            where TC : unmanaged, IColumn<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, sizeof(TBuffer));
#endif
            return new DynamicVariableMap<TKey, TValue, T, TC>(buffer.Reinterpret<byte>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicVariableMap<TKey, TValue, T1, TC1, T2, TC2> AsVariableMap<TBuffer, TKey, TValue, T1, TC1, T2, TC2>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicVariableMap<TKey, TValue, T1, TC1, T2, TC2>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where T1 : unmanaged, IEquatable<T1>
            where TC1 : unmanaged, IColumn<T1>
            where T2 : unmanaged, IEquatable<T2>
            where TC2 : unmanaged, IColumn<T2>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, sizeof(TBuffer));
#endif
            return new DynamicVariableMap<TKey, TValue, T1, TC1, T2, TC2>(buffer.Reinterpret<byte>());
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
        internal static DynamicVariableMapHelper<TKey, TValue, T, TC>* AsVariableHelper<TKey, TValue, T, TC>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where T : unmanaged, IEquatable<T>
            where TC : unmanaged, IColumn<T>
        {
            CheckSize(buffer, sizeof(DynamicVariableMapHelper<TKey, TValue, T, TC>));
            return (DynamicVariableMapHelper<TKey, TValue, T, TC>*)buffer.GetPtr();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>* AsVariableHelper<TKey, TValue, T1, TC1, T2, TC2>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where T1 : unmanaged, IEquatable<T1>
            where TC1 : unmanaged, IColumn<T1>
            where T2 : unmanaged, IEquatable<T2>
            where TC2 : unmanaged, IColumn<T2>
        {
            CheckSize(buffer, sizeof(DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>));
            return (DynamicVariableMapHelper<TKey, TValue, T1, TC1, T2, TC2>*)buffer.GetPtr();
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
