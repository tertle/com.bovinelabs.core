namespace BovineLabs.Core.Collections
{
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativePartialKeyedMap<TValue> : INativeDisposable
        where TValue : unmanaged
    {
        private UnsafePartialKeyedMap<TValue>* map;

        public NativePartialKeyedMap(int* keys, TValue* values, int length, int bucketCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            this.map = UnsafePartialKeyedMap<TValue>.Create(keys, values, length, bucketCapacity, allocator);
        }

        public bool IsCreated => this.map != null;

        public TValue this[int i] => (*this.map)[i];

        public void Dispose()
        {
            UnsafePartialKeyedMap<TValue>.Destroy(this.map);
            this.map = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new NativePartialKeyedMapDisposeJob
            {
                Map = this.map,
                Next = this.map->Next,
                Buckets = this.map->Buckets,
                Allocator = this.map->Allocator,
            }.Schedule(inputDeps);

            this.map = null;

            return jobHandle;
        }

        public void Update(int* newKeys, TValue* newValues, int newLength)
        {
            this.map->Update(newKeys, newValues, newLength);
        }

        public bool TryGetFirstValue(int key, out TValue item, out UnsafeKeyedMapIterator it)
        {
            return this.map->TryGetFirstValue(key, out item, out it);
        }

        public bool TryGetNextValue(out TValue item, ref UnsafeKeyedMapIterator it)
        {
            return this.map->TryGetNextValue(out item, ref it);
        }
    }

    [BurstCompile]
    internal unsafe struct NativePartialKeyedMapDisposeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public void* Map;

        [NativeDisableUnsafePtrRestriction]
        public int* Next;

        [NativeDisableUnsafePtrRestriction]
        public int* Buckets;

        public AllocatorManager.AllocatorHandle Allocator;

        public void Execute()
        {
            Memory.Unmanaged.Free(this.Buckets, this.Allocator);
            Memory.Unmanaged.Free(this.Next, this.Allocator);
            Memory.Unmanaged.Free(this.Map, this.Allocator);
        }
    }
}
