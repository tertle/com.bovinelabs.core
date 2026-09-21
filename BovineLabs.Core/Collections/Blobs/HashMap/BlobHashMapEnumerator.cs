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
        private readonly BlobHashMapData<TKey, TValue>* _data;
        private int _index;
        private int _bucketIndex;
        private int _nextIndex;

        internal BlobHashMapEnumerator(ref BlobHashMapData<TKey, TValue> data)
        {
            _data = (BlobHashMapData<TKey, TValue>*)UnsafeUtility.AddressOf(ref data);
            _index = -1;
            _bucketIndex = 0;
            _nextIndex = -1;
        }

        public KVPair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_data, _index);
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return MoveNext(_data, ref _bucketIndex, ref _nextIndex, out _index);
        }

        public void Reset()
        {
            _index = -1;
            _bucketIndex = 0;
            _nextIndex = -1;
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
