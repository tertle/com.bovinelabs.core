// <copyright file="NativeParallelMultiHashMapFallbackTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    public partial class NativeParallelMultiHashMapFallbackTests : ECSTestsFixture
    {
        private const int EntityCount = 100000;

        [Test]
        public void OverflowTest()
        {
            var testSystem = this.World.CreateSystem<TestSystem>();
            testSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<DamageBuffer>().Build(this.Manager);
            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var handle = this.Manager.GetBufferTypeHandle<DamageBuffer>(false);

            var count = 0;

            // Check all damage instances were written safely
            foreach (var c in chunks)
            {
                var damageInstances = c.GetBufferAccessor(ref handle);
                for (var i = 0; i < c.Count; i++)
                {
                    count += damageInstances[i].Length;
                }
            }

            Assert.AreEqual(EntityCount, count);
        }

        private partial struct TestSystem : ISystem
        {
            private NativeArray<Entity> entities;
            private NativeParallelMultiHashMapFallback<Entity, int> damageInstances;
            private ThreadRandom random;

            public void OnCreate(ref SystemState state)
            {
                var arch = state.EntityManager.CreateArchetype(typeof(DamageBuffer));
                this.entities = state.EntityManager.CreateEntity(arch, EntityCount, Allocator.Persistent);
                this.damageInstances =
                    new NativeParallelMultiHashMapFallback<Entity, int>((int)(EntityCount * 0.75f), Allocator.Persistent); // Capacity < count so it'll overflow

                this.random = new ThreadRandom(1234, Allocator.Persistent);
            }

            public void OnDestroy(ref SystemState state)
            {
                this.entities.Dispose();
                this.damageInstances.Dispose();
                this.random.Dispose();
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                state.Dependency = new WriteDamageJob
                {
                    Random = this.random,
                    Entities = this.entities,
                    DamageInstances = this.damageInstances.AsWriter(),
                }.ScheduleParallel(state.Dependency);

                state.Dependency = this.damageInstances.Apply(state.Dependency, out var reader);

                state.Dependency = new ReadDamageJob
                {
                    DamageInstances = reader,
                    DamageBuffers = SystemAPI.GetBufferLookup<DamageBuffer>(),
                }.ScheduleParallel(reader, 128, state.Dependency);
            }
        }

        [BurstCompile]
        [WithAll(typeof(DamageBuffer))]
        private partial struct WriteDamageJob : IJobEntity
        {
            public ThreadRandom Random;

            [ReadOnly]
            public NativeArray<Entity> Entities;

            public NativeParallelMultiHashMapFallback<Entity, int>.ParallelWriter DamageInstances;

            private void Execute()
            {
                ref var random = ref this.Random.GetRandomRef();
                var index = random.NextInt(this.Entities.Length);
                this.DamageInstances.Add(this.Entities[index], random.NextInt());
            }
        }

        [BurstCompile]
        private struct ReadDamageJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelMultiHashMap<Entity, int>.ReadOnly DamageInstances;

            [NativeDisableParallelForRestriction]
            public BufferLookup<DamageBuffer> DamageBuffers;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.DamageInstances, entryIndex, out var target, out var damage);
                this.DamageBuffers[target].Add(new DamageBuffer { Value = damage });
            }
        }

        [BurstCompile]
        public struct DamageBuffer : IBufferElementData
        {
            public int Value;
        }
    }
}