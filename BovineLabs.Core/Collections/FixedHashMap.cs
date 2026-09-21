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
        private readonly TCapacity _data;
        private readonly int _sizeOfValueT;

        public FixedHashMap(TCapacity data)
        {
            _data = data;
            _sizeOfValueT = sizeof(TValue);
            Count = 0;
            Capacity = CalcCapacity();

            var totalHashesSizeInBytes = UnsafeUtility.SizeOf<uint>() * Capacity;
            UnsafeUtility.MemSet(Ptr, 0xff, totalHashesSizeInBytes);
        }

        public int Capacity { get; }

        public int Count { get; private set; }

        private uint* Ptr
        {
            get
            {
                fixed (TCapacity* key = &_data)
                {
                    return (uint*)key;
                }
            }
        }

        private int* NumItems => (int*)(Ptr + Capacity);

        private TKey* Keys => (TKey*)(Ptr + (Capacity * 2));

        public bool TryAdd(TKey key, TValue item)
        {
            var idx = TryAdd(key);
            if (idx != -1)
            {
                GetElementAt(idx) = item;
                return true;
            }

            return false;
        }

        [Pure]
        public bool TryGetValue(TKey key, out TValue item)
        {
            var idx = Find(key);

            if (idx != -1)
            {
                item = GetElementAt(idx);
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
            var hash = Hash(key);
            var firstIdx = GetFirstIdx(hash);
            var idx = firstIdx;

            do
            {
                var current = Ptr[idx];

                if (current == uint.MaxValue)
                {
                    Ptr[idx] = hash;
                    GetKeyAt(idx) = key;
                    Count++;

                    NumItems[firstIdx] += 1;

                    return idx;
                }

                if (current == hash && GetKeyAt(idx).Equals(key))
                {
                    return -1;
                }

                idx = (idx + 1) % Capacity;
            }
            while (idx != firstIdx);

            return -1;
        }

        private int Find(in TKey key)
        {
            var hash = Hash(key);
            var firstIdx = GetFirstIdx(hash);
            var num = NumItems[firstIdx];
            var idx = firstIdx;

            do
            {
                if (num == 0)
                {
                    return -1;
                }

                if (Ptr[idx] == hash)
                {
                    if (GetKeyAt(idx).Equals(key))
                    {
                        return idx;
                    }

                    num--;
                }

                idx = (idx + 1) % Capacity;
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
            return (int)(hash % (uint)Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref TKey GetKeyAt(int idx)
        {
            return ref Keys[idx];
        }

        private ref TValue GetElementAt(int idx)
        {
            return ref *(TValue*)GetElementAt(Ptr, Capacity, idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void* GetElementAt(void* src, int capacity, int idx)
        {
            var ptr = (byte*)src;
            ptr += capacity * (sizeof(uint) + sizeof(int) + sizeof(TKey));
            ptr += idx * _sizeOfValueT;

            return ptr;
        }
    }
}
