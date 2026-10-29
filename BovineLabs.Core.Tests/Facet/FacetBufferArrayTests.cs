namespace BovineLabs.Core.Tests.Facet
{
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    public partial class FacetBufferArrayTests : ECSTestsFixture
    {
        [Test]
        public void Lookup_ReadsBuffersAsArrays()
        {
            var entity = CreateEntity();
            ref var state = ref CreateTestSystemState();
            var data = default(FacetBufferArrayFacet.SingletonData);
            var lookup = default(FacetBufferArrayFacet.Lookup);
            data.Create(ref state);
            lookup.Create(ref state);
            lookup.Update(ref state, data);

            Assert.IsTrue(lookup.TryGet(entity, out var facet));
            AssertFacet(facet);

            var b = facet.B;
            b[0] = new BufferB { Value = 7 };
            Assert.AreEqual(7, Manager.GetBuffer<BufferB>(entity)[0].Value);
        }

        [Test]
        public void ResolvedChunk_ReadsBuffersAsArrays()
        {
            CreateEntity();
            ref var state = ref CreateTestSystemState();
            var data = default(FacetBufferArrayFacet.SingletonData);
            var typeHandle = default(FacetBufferArrayFacet.TypeHandle);
            data.Create(ref state);
            typeHandle.Create(ref state);
            typeHandle.Update(ref state, data);

            using var query = FacetBufferArrayFacet.CreateQueryBuilder().Build(Manager);
            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);

            AssertFacet(typeHandle.Resolve(chunks[0])[0]);
        }

        [Test]
        public void Lookup_CompletesSingletonWriters()
        {
            var entity = CreateEntity();
            World.CreateSystem<WriterSystem>().Update(WorldUnmanaged);
            ref var state = ref CreateTestSystemState();
            var data = default(FacetBufferArrayFacet.SingletonData);
            var lookup = default(FacetBufferArrayFacet.Lookup);
            data.Create(ref state);
            lookup.Create(ref state);
            lookup.Update(ref state, data, true);

            Assert.IsTrue(lookup.TryGet(entity, out var facet));
            Assert.AreEqual(9, facet.Shared[0].Value);
        }

        [Test]
        public void Lookup_MissingRequiredBufferFails()
        {
            var entity = Manager.CreateEntity(typeof(BufferA));
            Manager.CreateEntity(typeof(SingletonB));
            ref var state = ref CreateTestSystemState();
            var data = default(FacetBufferArrayFacet.SingletonData);
            var lookup = default(FacetBufferArrayFacet.Lookup);
            data.Create(ref state);
            lookup.Create(ref state);
            lookup.Update(ref state, data);

            Assert.IsFalse(lookup.TryGet(entity, out _));
        }

        [Test]
        public void Constructor_TakesArrays()
        {
            var a = new NativeArray<BufferA>(1, Allocator.Temp);
            var b = new NativeArray<BufferB>(2, Allocator.Temp);
            var singleton = new NativeArray<SingletonB>(3, Allocator.Temp);

            var facet = new FacetBufferArrayFacet(a, b, default, singleton);

            Assert.AreEqual(1, facet.A.Length);
            Assert.AreEqual(2, facet.B.Length);
            Assert.IsFalse(facet.C.IsCreated);
            Assert.AreEqual(3, facet.Shared.Length);
        }

        private static void AssertFacet(in FacetBufferArrayFacet facet)
        {
            Assert.AreEqual(2, facet.A.Length);
            Assert.AreEqual(2, facet.A[1].Value);
            Assert.AreEqual(1, facet.B.Length);
            Assert.AreEqual(3, facet.B[0].Value);
            Assert.IsFalse(facet.C.IsCreated);
            Assert.AreEqual(1, facet.Shared.Length);
            Assert.AreEqual(4, facet.Shared[0].Value);
        }

        private Entity CreateEntity()
        {
            var entity = Manager.CreateEntity(typeof(BufferA), typeof(BufferB));
            var a = Manager.GetBuffer<BufferA>(entity);
            a.Add(new BufferA { Value = 1 });
            a.Add(new BufferA { Value = 2 });
            Manager.GetBuffer<BufferB>(entity).Add(new BufferB { Value = 3 });

            var singleton = Manager.CreateEntity(typeof(SingletonB));
            Manager.GetBuffer<SingletonB>(singleton).Add(new SingletonB { Value = 4 });
            return entity;
        }

        private ref SystemState CreateTestSystemState()
        {
            World.CreateSystem<TestSystem>();
            return ref WorldUnmanaged.GetExistingSystemState<TestSystem>();
        }

        private partial struct TestSystem : ISystem
        {
            public void OnUpdate(ref SystemState state)
            {
            }
        }

        private partial struct WriterSystem : ISystem
        {
            public void OnUpdate(ref SystemState state)
            {
                state.Dependency = new WriteJob { Shared = SystemAPI.GetSingletonBuffer<SingletonB>() }.Schedule(state.Dependency);
            }

            private struct WriteJob : IJob
            {
                public DynamicBuffer<SingletonB> Shared;

                public void Execute()
                {
                    this.Shared[0] = new SingletonB { Value = 9 };
                }
            }
        }
    }

    public readonly partial struct FacetBufferArrayFacet : IFacet
    {
        [ReadOnly]
        public readonly NativeArray<BufferA> A;

        public readonly NativeArray<BufferB> B;

        [FacetOptional]
        [ReadOnly]
        public readonly NativeArray<BufferC> C;

        [ReadOnly]
        [Singleton]
        public readonly NativeArray<SingletonB> Shared;
    }
}
