// <copyright file="SingletonCollectionUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Events
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public interface ISingletonCollectionUtil<TC>
        where TC : unmanaged, IDisposable
    {
        UnsafeList<TC>.ReadOnly Streams { get; }
    }

    public unsafe struct SingletonCollectionUtil<T, TC> : ISingletonCollectionUtil<TC>, IDisposable
        where T : unmanaged, ISingletonCollection<TC>
        where TC : unmanaged, IDisposable
    {
        private readonly UnsafeList<TC>* streams;
        private AllocatorHelper<RewindableAllocator> allocator;
        private AllocatorHelper<RewindableAllocator> allocator2;
        private EntityQuery query;

        public SingletonCollectionUtil(ref SystemState state, int initialSizeInBytes = 16 * 1024, Allocator allocator = Allocator.Persistent)
        {
            this.allocator = new AllocatorHelper<RewindableAllocator>(allocator);
            this.allocator.Allocator.Initialize(initialSizeInBytes);
            this.allocator2 = new AllocatorHelper<RewindableAllocator>(allocator);
            this.allocator2.Allocator.Initialize(initialSizeInBytes);

            this.streams = UnsafeList<TC>.Create(1, Allocator.Persistent);

            var singleton = new T
            {
                Collections = this.streams,
                Allocator = this.allocator.Allocator.ToAllocator,
            };

            state.EntityManager.AddComponentData(state.SystemHandle, singleton);

            this.query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(EntityQueryOptions.IncludeSystems).Build(ref state);
        }

        public Allocator CurrentAllocator => this.allocator.Allocator.ToAllocator;

        public UnsafeList<TC>.ReadOnly Streams => this.streams->AsReadOnly();

        public void ClearRewind()
        {
            this.streams->Clear();

            (this.allocator, this.allocator2) = (this.allocator2, this.allocator);
            this.allocator.Allocator.Rewind();

            var s = this.query.GetSingletonRW<T>();
            s.ValueRW.Allocator = this.allocator.Allocator.ToAllocator;
        }

        public void Dispose()
        {
            this.streams->Dispose();
            AllocatorManager.Free(Allocator.Persistent, this.streams);

            this.allocator.Allocator.Dispose();
            this.allocator.Dispose();

            this.allocator2.Allocator.Dispose();
            this.allocator2.Dispose();
        }
    }
}
