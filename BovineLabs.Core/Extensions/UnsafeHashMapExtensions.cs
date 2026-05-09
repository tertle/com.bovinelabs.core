// <copyright file="UnsafeHashMapExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class UnsafeHashMapExtensions
    {
        [Obsolete("Use GetOrAddRefUnsafe")]
        public static ref TValue GetOrAddRef<TKey, TValue>(ref this UnsafeHashMap<TKey, TValue> hashMap, TKey key, TValue defaultValue = default)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref hashMap.GetOrAddRefUnsafe(key, defaultValue);
        }

        /// <summary>
        /// Gets the value for a key or adds <paramref name="defaultValue" /> and returns it by reference.
        /// </summary>
        /// <remarks>
        /// Unsafe because the returned ref points directly into the hash map storage. Consume it immediately and do not keep or use it after any later
        /// write to the same hash map, such as add, get-or-add, remove, clear, or capacity-changing operations.
        /// </remarks>
        /// <param name="hashMap"> The hash map to read or add into. </param>
        /// <param name="key"> The key to look up. </param>
        /// <param name="defaultValue"> Value to add if the key is not present. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        /// <returns> A reference to the value stored in the hash map. </returns>
        public static ref TValue GetOrAddRefUnsafe<TKey, TValue>(ref this UnsafeHashMap<TKey, TValue> hashMap, TKey key, TValue defaultValue = default)
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

        [Obsolete("Use GetOrAddRefUnsafe")]
        public static ref TValue GetOrAddRef<TKey, TValue>(ref this UnsafeHashMap<TKey, TValue> hashMap, TKey key, TValue defaultValue, out bool added)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref hashMap.GetOrAddRefUnsafe(key, defaultValue, out added);
        }

        /// <summary>
        /// Gets the value for a key or adds <paramref name="defaultValue" /> and returns it by reference.
        /// </summary>
        /// <remarks>
        /// Unsafe because the returned ref points directly into the hash map storage. Consume it immediately and do not keep or use it after any later
        /// write to the same hash map, such as add, get-or-add, remove, clear, or capacity-changing operations.
        /// </remarks>
        /// <param name="hashMap"> The hash map to read or add into. </param>
        /// <param name="key"> The key to look up. </param>
        /// <param name="defaultValue"> Value to add if the key is not present. </param>
        /// <param name="added"> Outputs whether a new entry was added. </param>
        /// <typeparam name="TKey"> The key type. </typeparam>
        /// <typeparam name="TValue"> The value type. </typeparam>
        /// <returns> A reference to the value stored in the hash map. </returns>
        public static ref TValue GetOrAddRefUnsafe<TKey, TValue>(ref this UnsafeHashMap<TKey, TValue> hashMap, TKey key, TValue defaultValue, out bool added)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var idx = hashMap.m_Data.Find(key);

            if (idx == -1)
            {
                idx = hashMap.m_Data.AddNoFind(key);
                UnsafeUtility.WriteArrayElement(hashMap.m_Data.Ptr, idx, defaultValue);
                added = true;
            }
            else
            {
                added = false;
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
