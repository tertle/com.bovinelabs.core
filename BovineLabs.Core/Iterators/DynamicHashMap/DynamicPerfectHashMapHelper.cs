// <copyright file="DynamicPerfectHashMapHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DynamicPerfectHashMapHelper<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal int KeysOffset;
        internal int ValuesOffset;
        internal int Size;
        internal TValue NullValue;

        internal TKey* Keys
        {
            get
            {
                fixed (DynamicPerfectHashMapHelper<TKey, TValue>* data = &this)
                {
                    return (TKey*)((byte*)data + data->KeysOffset);
                }
            }
        }

        internal TValue* Values
        {
            get
            {
                fixed (DynamicPerfectHashMapHelper<TKey, TValue>* data = &this)
                {
                    return (TValue*)((byte*)data + data->ValuesOffset);
                }
            }
        }

        internal static void Init(DynamicBuffer<byte> buffer, NativeHashMap<TKey, TValue> hashMap, TValue nullValue)
        {
            var data = Init(buffer, hashMap.GetKeyArray(Allocator.Temp), nullValue);

            var dataKeys = data->Keys;
            var dataValues = data->Values;

            foreach (var kvp in hashMap)
            {
                var index = IndexFor(kvp.Key, data->Size);
                dataKeys[index] = kvp.Key;
                dataValues[index] = kvp.Value;
            }
        }

        internal static void Init(DynamicBuffer<byte> buffer, DynamicHashMap<TKey, TValue> hashMap, TValue nullValue)
        {
            var data = Init(buffer, hashMap.GetKeyArray(Allocator.Temp), nullValue);

            var dataKeys = data->Keys;
            var dataValues = data->Values;

            foreach (var kvp in hashMap)
            {
                var index = IndexFor(kvp.Key, data->Size);
                dataKeys[index] = kvp.Key;
                dataValues[index] = kvp.Value;
            }
        }

        internal static void Init(DynamicBuffer<byte> buffer, NativeArray<TKey> keys, NativeArray<TValue> values, TValue nullValue)
        {
            if (keys.Length != values.Length)
            {
                throw new ArgumentException("Keys and values must have the same length.");
            }

            var data = Init(buffer, keys, nullValue);

            var dataKeys = data->Keys;
            var dataValues = data->Values;

            for (var i = 0; i < keys.Length; i++)
            {
                var index = IndexFor(keys[i], data->Size);
                dataKeys[index] = keys[i];
                dataValues[index] = values[i];
            }
        }

        private static DynamicPerfectHashMapHelper<TKey, TValue>* Init(DynamicBuffer<byte> buffer, NativeArray<TKey> keys, TValue nullValue)
        {
            var uniqueSet = new NativeHashSet<int>(keys.Length, Allocator.Temp);
            AssertCollisionFree(keys, uniqueSet);

            var size = FindSize(keys, uniqueSet);
            var totalSize = CalculateDataSize(size, out var keysOffset, out var valuesOffset);
            buffer.ResizeUninitialized(totalSize);

            var data = buffer.AsHelper<TKey, TValue>();

            data->Size = size;
            data->KeysOffset = keysOffset;
            data->ValuesOffset = valuesOffset;
            data->NullValue = nullValue;

            var dataValues = data->Values;

            UnsafeUtility.MemCpyReplicate(dataValues, &nullValue, sizeof(TValue), size);

            return data;
        }

        private static int IndexFor(TKey key, int size)
        {
            return key.GetHashCode() & (size - 1);
        }

        private static int FindSize(NativeArray<TKey> keys, NativeHashSet<int> unique)
        {
            // Find a power of 2 capacity greater than map.size().
            var size = math.ceilpow2(keys.Length);

            while (HasCollisions(size, keys, unique))
            {
                size <<= 1;
            }

            return size;
        }

        private static bool HasCollisions(int size, NativeArray<TKey> keys, NativeHashSet<int> usedIndexes)
        {
            usedIndexes.Clear();

            foreach (var key in keys)
            {
                var index = IndexFor(key, size);

                if (!usedIndexes.Add(index))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CalculateDataSize(int count, out int outKeysOffset, out int outValuesOffset)
        {
            var headerSize = sizeof(DynamicPerfectHashMapHelper<TKey, TValue>);
            var keysOffset = CollectionHelper.Align(headerSize, UnsafeUtility.AlignOf<TKey>());

            var keysSize = sizeof(TKey) * count;
            var valuesOffset = CollectionHelper.Align(keysOffset + keysSize, UnsafeUtility.AlignOf<TValue>());
            var valuesSize = sizeof(TValue) * count;

            outKeysOffset = keysOffset;
            outValuesOffset = valuesOffset;

            return valuesOffset + valuesSize;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void AssertCollisionFree(NativeArray<TKey> keys, NativeHashSet<int> unique)
        {
            foreach (var key in keys)
            {
                if (!unique.Add(key.GetHashCode()))
                {
                    throw new ArgumentException("HashCode collision.");
                }
            }
        }
    }
}
