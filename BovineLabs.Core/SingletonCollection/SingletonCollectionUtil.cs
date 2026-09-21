namespace BovineLabs.Core.SingletonCollection
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;

    public interface ISingletonCollectionUtil<TC>
        where TC : unmanaged
    {
        UnsafeList<TC>.ReadOnly Containers { get; }
    }

    public unsafe struct SingletonCollectionUtil<T, TC> : ISingletonCollectionUtil<TC>, IDisposable
        where T : unmanaged, ISingletonCollection<TC>
        where TC : unmanaged
    {
        private readonly JobHandle* _previousHandle;
        private DoubleRewindableAllocators _allocator;
        private EntityQuery _query;

        public SingletonCollectionUtil(ref SystemState state, int initialSizeInBytes = 16 * 1024)
        {
            _allocator = new DoubleRewindableAllocators(Allocator.Persistent, initialSizeInBytes);
            _previousHandle = AllocatorManager.Allocate<JobHandle>(Allocator.Persistent);
            *_previousHandle = default;

            ContainersUnsafe = UnsafeList<TC>.Create(1, Allocator.Persistent);

            var singleton = new T
            {
                Collections = ContainersUnsafe,
                Allocator = _allocator.Allocator.ToAllocator,
            };

            state.EntityManager.AddComponentData(state.SystemHandle, singleton);

            _query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(EntityQueryOptions.IncludeSystems).Build(ref state);
        }

        public Allocator CurrentAllocator => _allocator.Allocator.ToAllocator;

        public UnsafeList<TC>.ReadOnly Containers => ContainersUnsafe->AsReadOnly();

        public UnsafeList<TC>* ContainersUnsafe { get; }

        public void ClearRewind(JobHandle handle)
        {
            ContainersUnsafe->Clear();

            _previousHandle->Complete();
            *_previousHandle = handle;

            _allocator.Update();
            var s = _query.GetSingletonRW<T>();
            s.ValueRW.Allocator = _allocator.Allocator.ToAllocator;
        }

        public void Dispose()
        {
            AllocatorManager.Free(Allocator.Persistent, _previousHandle);

            UnsafeList<TC>.Destroy(ContainersUnsafe);
            _allocator.Dispose();
        }
    }
}
