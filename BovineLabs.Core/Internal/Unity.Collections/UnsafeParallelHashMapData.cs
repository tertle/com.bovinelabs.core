#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference

// Layout matches Unity 6000.7.0b1; operations delegate to the engine implementation.
namespace BovineLabs.Core.Internal
{
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct UnsafeParallelHashMapData
    {
        [FieldOffset(0)]
        public unsafe byte* values;

        [FieldOffset(8)]
        public unsafe byte* keys;

        [FieldOffset(16)]
        public unsafe byte* next;

        [FieldOffset(24)]
        public unsafe byte* buckets;

        [FieldOffset(32)]
        public int keyCapacity;

        [FieldOffset(36)]
        public int bucketCapacityMask;

        [FieldOffset(40)]
        public int allocatedIndexLength;

        public const int IntsPerCacheLine = Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData.IntsPerCacheLine;

        public unsafe int* firstFreeTLS => Actual.firstFreeTLS;

        private ref Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData Actual => ref UnsafeUtility.As<UnsafeParallelHashMapData,
        Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData>(ref this);

        internal static long GetBucketSize(int capacity)
        {
            return Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData.GetBucketSize(capacity);
        }

        internal static int GrowCapacity(int capacity)
        {
            return Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData.GrowCapacity(capacity);
        }

        internal static void ReallocateHashMap<TKey, TValue>(
            UnsafeParallelHashMapData* data, int capacity, long bucketCapacity,
            AllocatorManager.AllocatorHandle allocator) where TKey : unmanaged where TValue : unmanaged
        {
            Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData.ReallocateHashMap<TKey,
            TValue>((Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData*)data, capacity, bucketCapacity, allocator);
        }

        internal static void GetKeyArray<TKey>(UnsafeParallelHashMapData* data, NativeArray<TKey> result) where TKey : unmanaged
        {
            Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData.GetKeyArray<TKey>((Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData*)data,
            result);
        }

        internal UnsafeParallelHashMapBucketData GetBucketData() => Actual.GetBucketData();
    }
}
