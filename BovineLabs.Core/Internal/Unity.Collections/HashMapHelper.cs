// Layout matches Unity 6000.7.0b1; operations delegate to the engine implementation.
namespace BovineLabs.Core.Internal
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HashMapHelper<TKey> where TKey : unmanaged, IEquatable<TKey>
    {
        [NativeDisableUnsafePtrRestriction]
        public unsafe byte* Ptr;

        [NativeDisableUnsafePtrRestriction]
        public unsafe TKey* Keys;

        [NativeDisableUnsafePtrRestriction]
        public unsafe int* Next;

        [NativeDisableUnsafePtrRestriction]
        public unsafe int* Buckets;

        public int Count;

        public int Capacity;

        public int Log2MinGrowth;

        public int BucketCapacity;

        public int AllocatedIndex;

        public int FirstFreeIdx;

        public int SizeOfTValue;

        public AllocatorManager.AllocatorHandle Allocator;

        private ref Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey> Actual => ref UnsafeUtility.As<HashMapHelper<TKey>,
        Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>>(ref this);

        internal bool IsCreated => this.Actual.IsCreated;
        internal bool IsEmpty => this.Actual.IsEmpty;

        internal int CalcCapacityCeilPow2(int capacity)
        {
            return this.Actual.CalcCapacityCeilPow2(capacity);
        }

        internal void Clear()
        {
            this.Actual.Clear();
        }

        internal void Init(int capacity, int sizeOfValueT, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            this.Actual.Init(capacity, sizeOfValueT, minGrowth, allocator);
        }

        internal void Dispose()
        {
            this.Actual.Dispose();
        }

        internal void Resize(int newCapacity)
        {
            this.Actual.Resize(newCapacity);
        }

        internal void TrimExcess()
        {
            this.Actual.TrimExcess();
        }

        internal int Find(TKey key)
        {
            return this.Actual.Find(key);
        }

        internal bool TryGetValue<TValue>(TKey key, out TValue item) where TValue : unmanaged
        {
            return this.Actual.TryGetValue<TValue>(key, out item);
        }

        internal NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            return this.Actual.GetKeyArray(allocator);
        }

        internal NativeArray<TValue> GetValueArray<TValue>(AllocatorManager.AllocatorHandle allocator) where TValue : unmanaged
        {
            return this.Actual.GetValueArray<TValue>(allocator);
        }

        internal NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays<TValue>(AllocatorManager.AllocatorHandle allocator) where TValue : unmanaged
        {
            return this.Actual.GetKeyValueArrays<TValue>(allocator);
        }

        internal static int GetBucketSize(int capacity)
        {
            return Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>.GetBucketSize(capacity);
        }

        internal static long CalculateDataSize(
            int capacity, int bucketCapacity, int sizeOfTValue, out long keyOffset, out long nextOffset, out long bucketOffset)
        {
            return Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>.CalculateDataSize(capacity, bucketCapacity, sizeOfTValue, out keyOffset,
            out nextOffset, out bucketOffset);
        }

        internal static HashMapHelper<TKey>* Alloc(int capacity, int sizeOfValueT, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            return (HashMapHelper<TKey>*)Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>.Alloc(capacity, sizeOfValueT, minGrowth, allocator);
        }

        internal static void Free(HashMapHelper<TKey>* data)
        {
            Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>.Free((Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>*)data);
        }

    }
}
