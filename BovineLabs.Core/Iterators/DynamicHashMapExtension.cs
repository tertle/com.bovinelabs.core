// <copyright file="DynamicHashMapExtension.cs" company="BovineLabs">
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

    public static class DynamicHashMapExtension
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

        [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Consistency")]
        internal static unsafe DynamicHashMapData* AsData<TKey, TValue>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckSize(buffer);
            return (DynamicHashMapData*)buffer.GetUnsafePtr();
        }

        [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Consistency")]
        internal static unsafe DynamicHashMapData* AsDataReadOnly<TKey, TValue>(this DynamicBuffer<byte> buffer)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CheckSize(buffer);
            return (DynamicHashMapData*)buffer.GetUnsafeReadOnlyPtr();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length < UnsafeUtility.SizeOf<DynamicHashMapData>())
            {
                throw new InvalidOperationException("Buffer not initialized before use.");
            }
        }
    }
}
