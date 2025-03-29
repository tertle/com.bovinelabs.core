// <copyright file="FixedHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct FixedHashMap<TKey, TValue, TCapacity>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TCapacity : unmanaged
    {
        private readonly TCapacity data;
        private readonly int sizeOfValueT;

        public FixedHashMap(TCapacity data)
        {
            this.data = data;
            this.sizeOfValueT = sizeof(TValue);
            this.Count = 0;
            this.Capacity = CalcCapacity();

            var totalHashesSizeInBytes = UnsafeUtility.SizeOf<uint>() * this.Capacity;
            UnsafeUtility.MemSet(this.Ptr, 0xff, totalHashesSizeInBytes);
        }

        public int Capacity { get; }

        public int Count { get; private set; }

        private uint* Ptr
        {
            get
            {
                fixed (TCapacity* key = &this.data)
                {
                    return (uint*)key;
                }
            }
        }

        private int* NumItems => (int*)(this.Ptr + this.Capacity);

        private TKey* Keys => (TKey*)(this.Ptr + (this.Capacity * 2));

        public bool TryAdd(TKey key, TValue item)
        {
            var idx = this.TryAdd(key);
            if (idx != -1)
            {
                this.GetElementAt(idx) = item;
                return true;
            }

            return false;
        }

        [Pure]
        public bool TryGetValue(TKey key, out TValue item)
        {
            var idx = this.Find(key);

            if (idx != -1)
            {
                item = this.GetElementAt(idx);
                return true;
            }

            item = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalcCapacity()
        {
            var sizeOfElement = sizeof(uint) + sizeof(uint) + sizeof(TValue) + sizeof(TKey);
            var capacity = sizeof(TCapacity) / sizeOfElement;
            return capacity;
        }

        private int TryAdd(in TKey key)
        {
            var hash = this.Hash(key);
            var firstIdx = this.GetFirstIdx(hash);
            var idx = firstIdx;

            do
            {
                var current = this.Ptr[idx];

                if (current == uint.MaxValue)
                {
                    this.Ptr[idx] = hash;
                    this.GetKeyAt(idx) = key;
                    this.Count++;

                    this.NumItems[firstIdx] += 1;

                    return idx;
                }

                if (current == hash && this.GetKeyAt(idx).Equals(key))
                {
                    return -1;
                }

                idx = (idx + 1) % this.Capacity;
            }
            while (idx != firstIdx);

            return -1;
        }

        private int Find(in TKey key)
        {
            var hash = this.Hash(key);
            var firstIdx = this.GetFirstIdx(hash);
            var num = this.NumItems[firstIdx];
            var idx = firstIdx;

            do
            {
                if (num == 0)
                {
                    return -1;
                }

                if (this.Ptr[idx] == hash)
                {
                    if (this.GetKeyAt(idx).Equals(key))
                    {
                        return idx;
                    }

                    num--;
                }

                idx = (idx + 1) % this.Capacity;
            }
            while (idx != firstIdx);

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Hash(in TKey key)
        {
            var hash = (uint)key.GetHashCode();
            return hash == uint.MaxValue ? 0 : hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetFirstIdx(uint hash)
        {
            return (int)(hash % (uint)this.Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref TKey GetKeyAt(int idx)
        {
            return ref this.Keys[idx];
        }

        private ref TValue GetElementAt(int idx)
        {
            return ref *(TValue*)this.GetElementAt(this.Ptr, this.Capacity, idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void* GetElementAt(void* src, int capacity, int idx)
        {
            var ptr = (byte*)src;
            ptr += capacity * (sizeof(uint) + sizeof(int) + sizeof(TKey));
            ptr += idx * this.sizeOfValueT;

            return ptr;
        }
    }
}
