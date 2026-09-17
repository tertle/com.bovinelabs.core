namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    [NativeContainerIsReadOnly]
    public unsafe struct BlobHashMapEnumerator<TKey, TValue> : IEnumerator<KVPair<TKey, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly BlobHashMapData<TKey, TValue>* data;
        private int index;
        private int bucketIndex;
        private int nextIndex;

        internal BlobHashMapEnumerator(ref BlobHashMapData<TKey, TValue> data)
        {
            this.data = (BlobHashMapData<TKey, TValue>*)UnsafeUtility.AddressOf(ref data);
            this.index = -1;
            this.bucketIndex = 0;
            this.nextIndex = -1;
        }

        public KVPair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(this.data, this.index);
        }

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return MoveNext(this.data, ref this.bucketIndex, ref this.nextIndex, out this.index);
        }

        public void Reset()
        {
            this.index = -1;
            this.bucketIndex = 0;
            this.nextIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MoveNext(BlobHashMapData<TKey, TValue>* data, ref int bucketIndex, ref int nextIndex, out int index)
        {
            if (nextIndex != -1)
            {
                index = nextIndex;
                nextIndex = data->Next[nextIndex];
                return true;
            }

            return MoveNextSearch(data, ref bucketIndex, out nextIndex, out index);
        }

        private static bool MoveNextSearch(BlobHashMapData<TKey, TValue>* data, ref int bucketIndex, out int nextIndex, out int index)
        {
            var bucketCapacity = data->BucketCapacityMask + 1;
            for (int i = bucketIndex, num = bucketCapacity; i < num; ++i)
            {
                var idx = data->Buckets[i];

                if (idx != -1)
                {
                    index = idx;
                    bucketIndex = i + 1;
                    nextIndex = data->Next[idx];

                    return true;
                }
            }

            index = -1;
            bucketIndex = bucketCapacity;
            nextIndex = -1;
            return false;
        }
    }
}
