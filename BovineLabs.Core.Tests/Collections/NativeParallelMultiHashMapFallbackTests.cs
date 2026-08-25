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

        [Test]
        public void Add_SingleEntryWithinCapacity_CanReadByKey()
        {
            var map = new NativeParallelMultiHashMapFallback<int, int>(1, Allocator.TempJob);
            try
            {
                var writer = map.AsWriter();
                writer.Add(7, 123);

                var handle = map.Apply(default, out var reader);
                handle.Complete();

                Assert.IsTrue(reader.TryGetFirstValue(7, out var value, out _), "Expected to read entry written with Add()");
                Assert.AreEqual(123, value);
                Assert.IsFalse(reader.TryGetFirstValue(8, out _, out _), "Unexpected value found for missing key");
            }
            finally
            {
                map.Dispose();
            }
        }

        [Test]
        public void Add_MultipleEntriesSameKey_AllValuesReadable()
        {
            var map = new NativeParallelMultiHashMapFallback<int, int>(3, Allocator.TempJob);
            try
            {
                var writer = map.AsWriter();
                writer.Add(9, 10);
                writer.Add(9, 20);
                writer.Add(9, 30);

                var handle = map.Apply(default, out var reader);
                handle.Complete();

                Assert.IsTrue(reader.TryGetFirstValue(9, out var value, out var it));

                var count = 1;
                var sum = value;
                while (reader.TryGetNextValue(out value, ref it))
                {
                    count++;
                    sum += value;
                }

                Assert.AreEqual(3, count, "All values for the same key should be readable");
                Assert.AreEqual(60, sum, "Expected all values written with Add() for key 9");
            }
            finally
            {
                map.Dispose();
            }
        }

        [Test]
        public void Add_ReservedAndFallbackPaths_BothReadableByKey()
        {
            var map = new NativeParallelMultiHashMapFallback<int, int>(1, Allocator.TempJob);
            try
            {
                var writer = map.AsWriter();
                writer.Add(1, 100); // Should hit reserved path.
                writer.Add(2, 200); // Should overflow to fallback path.
                writer.Add(3, 300); // Should overflow to fallback path.

                var handle = map.Apply(default, out var reader);
                handle.Complete();

                Assert.IsTrue(reader.TryGetFirstValue(1, out var value1, out _), "Reserved-path entry should be readable");
                Assert.IsTrue(reader.TryGetFirstValue(2, out var value2, out _), "Fallback entry should be readable");
                Assert.IsTrue(reader.TryGetFirstValue(3, out var value3, out _), "Fallback entry should be readable");
                Assert.AreEqual(100, value1);
                Assert.AreEqual(200, value2);
                Assert.AreEqual(300, value3);
            }
            finally
            {
                map.Dispose();
            }
        }

        [Test]
        public void Dispose_WithDependency_DisposesOwnedContainers()
        {
            var map = new NativeParallelMultiHashMapFallback<int, int>(1, Allocator.TempJob);

            try
            {
                map.Dispose(default).Complete();

                Assert.IsFalse(map.HashMap.IsCreated);
                Assert.IsFalse(map.Fallback.IsCreated);
            }
            finally
            {
                if (map.HashMap.IsCreated)
                {
                    map.HashMap.Dispose();
                }

                if (map.Fallback.IsCreated)
                {
                    map.Fallback.Dispose();
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Clear_WithPendingFallback_DoesNotRestoreEntriesOnApply(bool scheduled)
        {
            var map = new NativeParallelMultiHashMapFallback<int, int>(1, Allocator.TempJob);

            try
            {
                var writer = map.AsWriter();
                writer.Add(1, 10);
                writer.Add(2, 20);

                if (scheduled)
                {
                    map.Clear(default).Complete();
                }
                else
                {
                    map.Clear();
                }

                map.Apply(default, out var reader).Complete();

                Assert.AreEqual(0, reader.Count());
            }
            finally
            {
                map.Dispose();
            }
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
