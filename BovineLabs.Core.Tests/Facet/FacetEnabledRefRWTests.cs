// <copyright file="FacetEnabledRefRWTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Facet
{
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    public class FacetEnabledRefRWTests : ECSTestsFixture
    {
        [Test]
        public void Lookup_ReadsAndWritesEnabledState()
        {
            var entity = this.Manager.CreateEntity(typeof(EnabledB));
            this.Manager.SetComponentEnabled<EnabledB>(entity, false);

            ref var state = ref this.CreateTestSystemState();
            var lookup = default(FacetEnabledRefRWFacet.Lookup);
            lookup.Create(ref state);
            lookup.Update(ref state);

            Assert.IsTrue(lookup.TryGet(entity, out var facet));
            Assert.IsTrue(facet.IsValid);
            Assert.IsFalse(facet.ValueRO);

            facet.SetEnabled(true);

            Assert.IsTrue(facet.ValueRO);
            Assert.IsTrue(this.Manager.IsComponentEnabled<EnabledB>(entity));

            facet.SetEnabled(false);

            Assert.IsFalse(facet.ValueRO);
            Assert.IsFalse(this.Manager.IsComponentEnabled<EnabledB>(entity));
        }

        [Test]
        public void ResolvedChunk_ReadsAndWritesEnabledState()
        {
            var entity = this.Manager.CreateEntity(typeof(EnabledB));
            this.Manager.SetComponentEnabled<EnabledB>(entity, false);

            ref var state = ref this.CreateTestSystemState();
            var typeHandle = default(FacetEnabledRefRWFacet.TypeHandle);
            typeHandle.Create(ref state);
            typeHandle.Update(ref state);

            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnabledB>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(this.Manager);
            using var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var facet = typeHandle.Resolve(chunks[0])[0];

            Assert.IsTrue(facet.IsValid);
            Assert.IsFalse(facet.ValueRO);

            facet.SetEnabled(true);

            Assert.IsTrue(facet.ValueRO);
            Assert.IsTrue(this.Manager.IsComponentEnabled<EnabledB>(entity));
        }

        [Test]
        public void OptionalLookup_MissingComponentReturnsInvalidValue()
        {
            var entity = this.Manager.CreateEntity();
            ref var state = ref this.CreateTestSystemState();
            var lookup = default(OptionalFacetEnabledRefRWFacet.Lookup);
            lookup.Create(ref state);
            lookup.Update(ref state);

            Assert.IsTrue(lookup.TryGet(entity, out var facet));
            Assert.IsFalse(facet.IsValid);
        }

        [Test]
        public void BufferLookup_ReadsAndWritesEnabledState()
        {
            var entity = this.Manager.CreateEntity();
            this.Manager.AddBuffer<EnabledBufferElement>(entity);
            this.Manager.SetComponentEnabled<EnabledBufferElement>(entity, false);

            ref var state = ref this.CreateTestSystemState();
            var lookup = default(BufferFacetEnabledRefRWFacet.Lookup);
            lookup.Create(ref state);
            lookup.Update(ref state);

            Assert.IsTrue(lookup.TryGet(entity, out var facet));
            Assert.IsTrue(facet.IsValid);
            Assert.IsFalse(facet.ValueRO);

            facet.SetEnabled(true);

            Assert.IsTrue(facet.ValueRO);
            Assert.IsTrue(this.Manager.IsComponentEnabled<EnabledBufferElement>(entity));
        }

        [Test]
        public void BufferResolvedChunk_ReadsAndWritesEnabledState()
        {
            var entity = this.Manager.CreateEntity();
            this.Manager.AddBuffer<EnabledBufferElement>(entity);
            this.Manager.SetComponentEnabled<EnabledBufferElement>(entity, false);

            ref var state = ref this.CreateTestSystemState();
            var typeHandle = default(BufferFacetEnabledRefRWFacet.TypeHandle);
            typeHandle.Create(ref state);
            typeHandle.Update(ref state);

            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnabledBufferElement>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(this.Manager);
            using var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var facet = typeHandle.Resolve(chunks[0])[0];

            Assert.IsTrue(facet.IsValid);
            Assert.IsFalse(facet.ValueRO);

            facet.SetEnabled(true);

            Assert.IsTrue(facet.ValueRO);
            Assert.IsTrue(this.Manager.IsComponentEnabled<EnabledBufferElement>(entity));
        }

        [Test]
        public void Lookup_ReadsEnabledStateFromBurst()
        {
            var entity = this.Manager.CreateEntity(typeof(EnabledB));
            ref var state = ref this.CreateTestSystemState();
            var lookup = default(FacetEnabledRefRWFacet.Lookup);
            lookup.Create(ref state);
            lookup.Update(ref state);
            using var result = new NativeArray<bool>(1, Allocator.TempJob);

            new ReadLookupJob
            {
                Lookup = lookup,
                Entity = entity,
                Result = result,
            }.Schedule().Complete();

            Assert.IsTrue(result[0]);
        }

        private ref SystemState CreateTestSystemState()
        {
            this.World.CreateSystem<TestSystem>();
            return ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>();
        }

        private partial struct TestSystem : ISystem
        {
            public void OnUpdate(ref SystemState state)
            {
            }
        }

        [BurstCompile]
        private struct ReadLookupJob : IJob
        {
            public FacetEnabledRefRWFacet.Lookup Lookup;
            public Entity Entity;
            public NativeArray<bool> Result;

            public void Execute()
            {
                this.Result[0] = this.Lookup[this.Entity].ValueRO;
            }
        }
    }
}
