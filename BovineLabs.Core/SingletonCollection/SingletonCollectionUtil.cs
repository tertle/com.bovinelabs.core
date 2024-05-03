// <copyright file="SingletonCollectionUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.SingletonCollection
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public interface ISingletonCollectionUtil<TC>
        where TC : unmanaged
    {
        UnsafeList<TC>.ReadOnly Containers { get; }
    }

    public unsafe struct SingletonCollectionUtil<T, TC> : ISingletonCollectionUtil<TC>, IDisposable
        where T : unmanaged, ISingletonCollection<TC>
        where TC : unmanaged
    {
        private readonly UnsafeList<TC>* containers;
        private DoubleRewindableAllocators allocator;
        private EntityQuery query;

        public SingletonCollectionUtil(ref SystemState state, int initialSizeInBytes = 16 * 1024, Allocator allocator = Allocator.Persistent)
        {
            this.allocator = new DoubleRewindableAllocators(allocator, initialSizeInBytes);

            this.containers = UnsafeList<TC>.Create(1, Allocator.Persistent);

            var singleton = new T
            {
                Collections = this.containers,
                Allocator = this.allocator.Allocator.ToAllocator,
            };

            state.EntityManager.AddComponentData(state.SystemHandle, singleton);

            this.query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(EntityQueryOptions.IncludeSystems).Build(ref state);
        }

        public Allocator CurrentAllocator => this.allocator.Allocator.ToAllocator;

        public UnsafeList<TC>.ReadOnly Containers => this.containers->AsReadOnly();

        /// <summary> Gets the underlying container. Don't use this unless you really know what you're doing. </summary>
        public UnsafeList<TC>* ContainersUnsafe => this.containers;

        public void ClearRewind()
        {
            this.containers->Clear();

            this.allocator.Update();
            var s = this.query.GetSingletonRW<T>();
            s.ValueRW.Allocator = this.allocator.Allocator.ToAllocator;
        }

        public void Dispose()
        {
            UnsafeList<TC>.Destroy(this.containers);

            this.allocator.Dispose();
        }
    }
}
