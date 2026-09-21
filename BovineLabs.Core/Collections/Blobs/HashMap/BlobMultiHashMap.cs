namespace BovineLabs.Core.Collections
{
    using System;
    using BovineLabs.Core.Utility;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [MayOnlyLiveInBlobStorage]
    public struct BlobMultiHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal BlobHashMapData<TKey, TValue> Data;

        public int Count => Data.Count[0];

        public bool TryGetFirstValue(TKey key, out Ptr<TValue> item, out BlobMultiHashMapIterator<TKey> it)
        {
            return Data.TryGetFirstValue(key, out item, out it);
        }

        public bool TryGetNextValue(out Ptr<TValue> item, ref BlobMultiHashMapIterator<TKey> it)
        {
            return Data.TryGetNextValue(out item, ref it);
        }

        public bool ContainsKey(TKey key)
        {
            return Data.TryGetFirstValue(key, out _, out _);
        }

        public unsafe BlobHashMapEnumerator<TKey, TValue> GetEnumerator()
        {
            return new BlobHashMapEnumerator<TKey, TValue>(ref UnsafeUtility.AsRef<BlobHashMapData<TKey, TValue>>(UnsafeUtility.AddressOf(ref this)));
        }
    }
}
