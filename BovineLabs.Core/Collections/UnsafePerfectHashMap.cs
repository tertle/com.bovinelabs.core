// <copyright file="UnsafePerfectHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    public unsafe struct UnsafePerfectHashMap<TKey, TValue> : IDisposable
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged, IEquatable<TValue>
    {
        [NativeDisableUnsafePtrRestriction]
        internal TKey* Keys;

        [NativeDisableUnsafePtrRestriction]
        internal TValue* Values;
        internal int Size;
        internal TValue NullValue;

        private readonly AllocatorManager.AllocatorHandle allocator;

        public UnsafePerfectHashMap(NativeArray<TKey> keys, NativeArray<TValue> values, TValue nullValue, AllocatorManager.AllocatorHandle allocator)
        {
            var uniqueSet = new NativeHashSet<int>(keys.Length, Allocator.Temp);
            AssertCollisionFree(keys, uniqueSet);

            var size = FindSize(keys, uniqueSet);
            var totalSize = CalculateDataSize(size, out var valueOffset);

            var ptr = Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, allocator);
            this.allocator = allocator;
            this.Size = size;
            this.NullValue = nullValue;
            this.Keys = (TKey*)ptr;
            this.Values = (TValue*)((byte*)ptr + valueOffset);

            UnsafeUtility.MemCpyReplicate(this.Values, &nullValue, sizeof(TValue), size);

            for (var i = 0; i < keys.Length; i++)
            {
                var index = IndexFor(keys[i], size);
                this.Keys[index] = keys[i];
                this.Values[index] = values[i];
            }
        }

        public static UnsafePerfectHashMap<TKey, TValue>* Alloc(
            NativeArray<TKey> keys, NativeArray<TValue> values, TValue nullValue, AllocatorManager.AllocatorHandle allocator)
        {
            var data = (UnsafePerfectHashMap<TKey, TValue>*)Memory.Unmanaged.Allocate(sizeof(UnsafePerfectHashMap<TKey, TValue>),
                UnsafeUtility.AlignOf<UnsafePerfectHashMap<TKey, TValue>>(), allocator);

            *data = new UnsafePerfectHashMap<TKey, TValue>(keys, values, nullValue, allocator);
            return data;
        }

        public static void Free(UnsafePerfectHashMap<TKey, TValue>* data)
        {
            if (data == null)
            {
                throw new InvalidOperationException("Hash based container has yet to be created or has been destroyed!");
            }

            var allocator = data->allocator;
            data->Dispose();
            Memory.Unmanaged.Free(data, allocator);
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value> True if this hash map has been allocated (and not yet deallocated). </value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Keys != null;
        }

        /// <summary> Gets and sets values by key. </summary>
        /// <remarks> Getting a key that is not present will throw. Setting a key that is not already present will add the key. </remarks>
        /// <param name="key"> The key to look up. </param>
        /// <value> The value associated with the key. </value>
        /// <exception cref="ArgumentException"> For getting, thrown if the key was not present. </exception>
        public TValue this[TKey key]
        {
            get
            {
                if (Hint.Unlikely(!this.TryGetValue(key, out var value)))
                {
                    this.ThrowKeyNotPresent(key);
                    return default;
                }

                return value;
            }

            set
            {
                if (!this.TryGetIndex(key, out var index))
                {
                    this.ThrowKeyNotPresent(key);
                }

                this.Values[index] = value;
            }
        }

        public void Dispose()
        {
            if (!this.IsCreated)
            {
                return;
            }

            Memory.Unmanaged.Free(this.Keys, this.allocator);
            this = default;
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <returns> True if the key was present. </returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            if (!this.TryGetIndex(key, out var index))
            {
                item = default;
                return false;
            }

            item = this.Values[index];
            return !item.Equals(this.NullValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexFor(TKey key, int size)
        {
            return key.GetHashCode() & (size - 1);
        }

        private static int FindSize(NativeArray<TKey> keys, NativeHashSet<int> unique)
        {
            // Find a power of 2 capacity greater than map.size().
            var size = 1; // TODO can this start higher?

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

        private static int CalculateDataSize(int count, out int outValueOffset)
        {
            var sizeOfTKey = sizeof(TKey);
            var sizeOfTValue = sizeof(TValue);

            var keysSize = sizeOfTKey * count;
            var valuesSize = sizeOfTValue * count;
            var totalSize = valuesSize + keysSize;

            outValueOffset = keysSize;

            return totalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetIndex(TKey key, out int index)
        {
            index = this.IndexFor(key);
            return index >= 0 && index < this.Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IndexFor(TKey key)
        {
            return IndexFor(key, this.Size);
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
        }
    }
}
