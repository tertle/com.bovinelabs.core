namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Utility;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [MayOnlyLiveInBlobStorage]
    public struct BlobHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal BlobHashMapData<TKey, TValue> Data;

        public int Count => Data.Count[0];

        public ref TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                {
                    return ref value.Ref;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                throw new KeyNotFoundException($"Key: {key} is not present in the BlobHashMap.");
#else
                return ref value.Ref;
#endif
            }
        }

        public bool TryGetValue(TKey key, out Ptr<TValue> item)
        {
            return Data.TryGetFirstValue(key, out item, out _);
        }

        public bool ContainsKey(TKey key)
        {
            return TryGetValue(key, out _);
        }

        public unsafe BlobHashMapEnumerator<TKey, TValue> GetEnumerator()
        {
            return new BlobHashMapEnumerator<TKey, TValue>(ref UnsafeUtility.AsRef<BlobHashMapData<TKey, TValue>>(UnsafeUtility.AddressOf(ref this)));
        }
    }
}
