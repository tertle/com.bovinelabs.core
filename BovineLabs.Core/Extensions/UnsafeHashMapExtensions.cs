// <copyright file="UnsafeHashMapExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class UnsafeHashMapExtensions
    {
        public static ref TValue GetOrAddRef<TKey, TValue>(ref this UnsafeHashMap<TKey, TValue> hashMap, TKey key, TValue defaultValue = default)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var idx = hashMap.m_Data.Find(key);

            if (idx == -1)
            {
                idx = hashMap.m_Data.AddNoFind(key);
                UnsafeUtility.WriteArrayElement(hashMap.m_Data.Ptr, idx, defaultValue);
            }

            return ref UnsafeUtility.ArrayElementAsRef<TValue>(hashMap.m_Data.Ptr, idx);
        }

        public static bool Remove<TKey, TValue>(ref this UnsafeHashMap<TKey, TValue> hashMap, TKey key, out TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var data = hashMap.m_Data;

            if (hashMap.Capacity == 0)
            {
                value = default;
                return false;
            }

            // First find the slot based on the hash
            var bucket = data.GetBucket(key);

            var prevEntry = -1;
            var entryIdx = data.Buckets[bucket];

            while (entryIdx >= 0 && entryIdx < data.Capacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(data.Keys, entryIdx).Equals(key))
                {
                    // Found matching element, remove it
                    if (prevEntry < 0)
                    {
                        data.Buckets[bucket] = data.Next[entryIdx];
                    }
                    else
                    {
                        data.Next[prevEntry] = data.Next[entryIdx];
                    }

                    value = UnsafeUtility.ReadArrayElement<TValue>(hashMap.m_Data.Ptr, entryIdx);

                    // And free the index
                    data.Next[entryIdx] = data.FirstFreeIdx;
                    data.FirstFreeIdx = entryIdx;
                    data.Count -= 1;

                    return true;
                }

                prevEntry = entryIdx;
                entryIdx = data.Next[entryIdx];
            }

            value = default;
            return false;
        }
    }
}
