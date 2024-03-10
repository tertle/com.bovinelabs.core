namespace BovineLabs.Core.Collections
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    internal unsafe struct BlobHashMapData<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal BlobArray<TValue> Values;
        internal BlobArray<TKey> Keys;
        internal BlobArray<int> Next;
        internal BlobArray<int> Buckets;
        internal BlobArray<int> Count; // only contains a single element containing the true count (set by builder)

        internal int BucketCapacityMask; // == buckets.Length - 1

        internal bool TryGetFirstValue(TKey key, out TValue item, out BlobMultiHashMapIterator<TKey> it)
        {
            it.Key = key;

            // ReSharper disable once Unity.BurstAccessingManagedMethod
            var bucket = key.GetHashCode() & this.BucketCapacityMask;
            it.NextIndex = this.Buckets[bucket];

            return this.TryGetNextValue(out item, ref it);
        }

        internal bool TryGetNextValue(out TValue item, ref BlobMultiHashMapIterator<TKey> it)
        {
            var index = it.NextIndex;
            it.NextIndex = -1;
            item = default;

            if (index < 0)
            {
                return false;
            }

            while (!this.Keys[index].Equals(it.Key))
            {
                index = this.Next[index];
                if (index < 0)
                {
                    return false;
                }
            }

            it.NextIndex = this.Next[index];
            item = this.Values[index];

            return true;
        }

        internal NativeArray<TKey> GetKeys(Allocator allocator)
        {
            var length = this.Count[0];
            var arr = new NativeArray<TKey>(length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(arr.GetUnsafePtr(), this.Keys.GetUnsafePtr(), UnsafeUtility.SizeOf<TKey>() * length);
            return arr;
        }

        internal NativeArray<TValue> GetValues(Allocator allocator)
        {
            var length = this.Count[0];
            var arr = new NativeArray<TValue>(length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(arr.GetUnsafePtr(), this.Values.GetUnsafePtr(), UnsafeUtility.SizeOf<TValue>() * length);
            return arr;
        }
    }
}
