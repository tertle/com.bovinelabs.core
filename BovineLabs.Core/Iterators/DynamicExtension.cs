// <copyright file="DynamicExtension.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Assertions;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static class DynamicExtension
    {
        public static DynamicHashMap<TKey, TValue> AsHashMap<TBuffer, TKey, TValue>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif
            return new DynamicHashMap<TKey, TValue>(buffer.Reinterpret<byte>());
        }

        public static DynamicMultiHashMap<TKey, TValue> AsMultiHashMap<TBuffer, TKey, TValue>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicMultiHashMap<TKey, TValue>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif
            return new DynamicMultiHashMap<TKey, TValue>(buffer.Reinterpret<byte>());
        }

        public static DynamicIndexMap<TValue> AsIndexMap<TBuffer, TValue>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicIndexMap<TValue>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif
            return new DynamicIndexMap<TValue>(buffer.Reinterpret<byte>());
        }

        public static DynamicHashSet<TKey> AsHashSet<TBuffer, TKey>(this DynamicBuffer<TBuffer> buffer)
            where TBuffer : unmanaged, IDynamicHashSet<TKey>
            where TKey : unmanaged, IEquatable<TKey>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif
            return new DynamicHashSet<TKey>(buffer.Reinterpret<byte>());
        }

        public static DynamicIndexMap<TValue> AsIndexMapAndInitialize<TBuffer, TValue>(this DynamicBuffer<TBuffer> buffer, int bucketLength, int length = 0)
            where TBuffer : unmanaged, IDynamicIndexMap<TValue>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, UnsafeUtility.SizeOf<TBuffer>());
#endif
            return new DynamicIndexMap<TValue>(buffer.Reinterpret<byte>(), bucketLength, length);
        }

        [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Consistency")]
        internal static unsafe DynamicHashMapData* AsData<TKey, TValue>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckSize<DynamicHashMapData>(buffer);
            return (DynamicHashMapData*)buffer.GetUnsafePtr();
        }

        [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Consistency")]
        internal static unsafe DynamicHashMapData* AsDataReadOnly<TKey, TValue>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckSize<DynamicHashMapData>(buffer);
            return (DynamicHashMapData*)buffer.GetUnsafeReadOnlyPtr();
        }

        [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Consistency")]
        internal static unsafe DynamicHashSetData* AsSetData<TKey>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
        {
            CheckSize<DynamicHashSetData>(buffer);
            return (DynamicHashSetData*)buffer.GetUnsafePtr();
        }

        [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Consistency")]
        internal static unsafe DynamicHashSetData* AsSetDataReadOnly<TKey>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
        {
            CheckSize<DynamicHashSetData>(buffer);
            return (DynamicHashSetData*)buffer.GetUnsafeReadOnlyPtr();
        }

        [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Consistency")]
        internal static unsafe DynamicIndexMapData* AsIndexData<TValue>(this DynamicBuffer<byte> buffer)
            where TValue : unmanaged
        {
            CheckSize<DynamicIndexMapData>(buffer);
            return (DynamicIndexMapData*)buffer.GetUnsafePtr();
        }

        [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Consistency")]
        internal static unsafe DynamicIndexMapData* AsIndexDataReadonly<TValue>(this DynamicBuffer<byte> buffer)
            where TValue : unmanaged
        {
            CheckSize<DynamicIndexMapData>(buffer);
            return (DynamicIndexMapData*)buffer.GetUnsafeReadOnlyPtr();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize<T>(DynamicBuffer<byte> buffer)
            where T : unmanaged
        {
            if (buffer.Length < UnsafeUtility.SizeOf<T>())
            {
                throw new InvalidOperationException("Buffer not initialized before use.");
            }
        }
    }
}
