namespace BovineLabs.Core.Collections
{
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativePartialKeyedMap<TValue> : INativeDisposable
        where TValue : unmanaged
    {
        private UnsafePartialKeyedMap<TValue>* _map;

        public NativePartialKeyedMap(int* keys, TValue* values, int length, int bucketCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            _map = UnsafePartialKeyedMap<TValue>.Create(keys, values, length, bucketCapacity, allocator);
        }

        public bool IsCreated => _map != null;

        public TValue this[int i] => (*_map)[i];

        public void Dispose()
        {
            UnsafePartialKeyedMap<TValue>.Destroy(_map);
            _map = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new NativePartialKeyedMapDisposeJob
            {
                Map = _map,
                Next = _map->Next,
                Buckets = _map->Buckets,
                Allocator = _map->Allocator,
            }.Schedule(inputDeps);

            _map = null;

            return jobHandle;
        }

        public void Update(int* newKeys, TValue* newValues, int newLength)
        {
            _map->Update(newKeys, newValues, newLength);
        }

        public bool TryGetFirstValue(int key, out TValue item, out UnsafeKeyedMapIterator it)
        {
            return _map->TryGetFirstValue(key, out item, out it);
        }

        public bool TryGetNextValue(out TValue item, ref UnsafeKeyedMapIterator it)
        {
            return _map->TryGetNextValue(out item, ref it);
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
            CollectionMemory.Free(Buckets, Allocator);
            CollectionMemory.Free(Next, Allocator);
            CollectionMemory.Free(Map, Allocator);
        }
    }
}
