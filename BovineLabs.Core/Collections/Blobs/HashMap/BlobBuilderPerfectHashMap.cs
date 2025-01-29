// <copyright file="BlobBuilderPerfectHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    public unsafe ref struct BlobBuilderPerfectHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged, IEquatable<TValue>
    {
        private readonly int capacity;
        private BlobBuilderArray<TValue> values;

        public BlobBuilderPerfectHashMap(
            ref BlobBuilder builder, ref BlobPerfectHashMap<TKey, TValue> data, NativeHashMap<TKey, TValue> hashmap, TValue nullValue = default)
        {
            this.capacity = data.Capacity = GetRequiredCapacity(hashmap);
            data.NullValue = nullValue;

            this.values = builder.Allocate(ref data.Values, data.Capacity);

            // Write null values
            UnsafeUtility.MemCpyReplicate(this.values.GetUnsafePtr(), &nullValue, sizeof(TValue), data.Capacity);

            foreach (var kvp in hashmap)
            {
                var index = IndexFor(kvp.Key, data.Capacity);
                this.values[index] = kvp.Value;
            }
        }

        public ref TValue this[TKey key]
        {
            get
            {
                if (!this.TryGetIndex(key, out var index))
                {
                    throw new ArgumentException($"Key: {key} is not present.");
                }

                return ref this.values[index];
            }
        }

        private static int GetRequiredCapacity(NativeHashMap<TKey, TValue> source)
        {
            var uniqueSet = new NativeHashSet<int>(source.Count, Allocator.Temp);
            AssertCollisionFree(source, uniqueSet);

            return FindSize(source, uniqueSet);
        }

        private static int FindSize(NativeHashMap<TKey, TValue> source, NativeHashSet<int> unique)
        {
            // Find a power of 2 capacity greater than map.size().
            var size = math.ceilpow2(source.Count);

            while (HasCollisions(size, source, unique))
            {
                size <<= 1;
            }

            return size;
        }

        private static bool HasCollisions(int size, NativeHashMap<TKey, TValue> source, NativeHashSet<int> usedIndexes)
        {
            usedIndexes.Clear();

            foreach (var kvp in source)
            {
                var index = IndexFor(kvp.Key, size);

                if (!usedIndexes.Add(index))
                {
                    return true;
                }
            }

            return false;
        }

        private static int IndexFor(TKey key, int size)
        {
            return key.GetHashCode() & (size - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetIndex(TKey key, out int index)
        {
            index = this.IndexFor(key);
            return index >= 0 && index < this.capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexFor(TKey key)
        {
            return key.GetHashCode() & (this.capacity - 1);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void AssertCollisionFree(NativeHashMap<TKey, TValue> source, NativeHashSet<int> unique)
        {
            foreach (var kvp in source)
            {
                if (!unique.Add(kvp.Key.GetHashCode()))
                {
                    throw new ArgumentException("HashCode collision.");
                }
            }
        }
    }
}
