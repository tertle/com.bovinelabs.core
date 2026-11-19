namespace BovineLabs.Core.Tests.Jobs
{
    using BovineLabs.Core.Jobs;
    using BovineLabs.Core.Utility;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    public class ArchetypeChunkTraversalTests : ECSTestsFixture
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ScheduledJob_UsesUnfilteredIndicesAndCompletesWorkerCallbacks(bool parallel, bool filtered)
        {
            CreateChunks();
            using var query = CreateQuery(filtered);
            var filteredIndices = query.CalculateFilteredChunkIndexArray(Allocator.Temp);
            using var entityCounts = new NativeArray<int>(filteredIndices.Length, Allocator.TempJob);
            using var workerCounts = new NativeQueue<int>(Allocator.TempJob);
            var job = new CountEntitiesJob
            {
                EntityCounts = entityCounts,
                WorkerCounts = workerCounts.AsParallelWriter(),
            };

            var handle = parallel ? job.ScheduleParallel(query, default) : job.Schedule(query, default);
            handle.Complete();

            Assert.That(entityCounts.Length, Is.EqualTo(6));
            var matchingEntityCount = filtered ? 1 : 2;
            for (var index = 0; index < entityCounts.Length; index++)
            {
                var expected = filteredIndices[index] < 0 ? 0 : matchingEntityCount;
                Assert.That(entityCounts[index], Is.EqualTo(expected), $"Unfiltered chunk index {index}");
            }

            var visitedChunks = 0;
            while (workerCounts.TryDequeue(out var count))
            {
                Assert.That(count, Is.GreaterThanOrEqualTo(0));
                visitedChunks += count;
            }

            Assert.That(visitedChunks, Is.EqualTo(filtered ? 2 : 6));
        }

        [Test]
        public void QueryEnumerator_ResetRevisitsOnlyEnabledMatches()
        {
            CreateChunks();
            using var query = CreateQuery(true);
            var enumerator = new QueryEntityEnumerator(query);
            for (var pass = 0; pass < 2; pass++)
            {
                var count = 0;
                while (enumerator.MoveNextChunk(out _, out var entities))
                {
                    while (entities.NextEntityIndex(out _))
                    {
                        count++;
                    }
                }

                Assert.That(count, Is.EqualTo(2));
                enumerator.Reset();
            }
        }

        private EntityQuery CreateQuery(bool filtered)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<SharedGroup>();
            if (filtered)
            {
                builder.WithAll<EnabledTag>();
            }

            var query = builder.Build(Manager);
            if (filtered)
            {
                query.SetSharedComponentFilter(new SharedGroup { Value = 1 });
            }

            return query;
        }

        private void CreateChunks()
        {
            var first = Manager.CreateArchetype(typeof(SharedGroup), typeof(EnabledTag));
            var second = Manager.CreateArchetype(typeof(SharedGroup), typeof(EnabledTag), typeof(ExtraTag));
            for (var group = 0; group < 3; group++)
            {
                var shared = new SharedGroup { Value = group };
                CreateChunkEntities(first, shared);
                CreateChunkEntities(second, shared);
            }
        }

        private void CreateChunkEntities(EntityArchetype archetype, SharedGroup shared)
        {
            var enabled = Manager.CreateEntity(archetype);
            var disabled = Manager.CreateEntity(archetype);
            Manager.SetSharedComponent(enabled, shared);
            Manager.SetSharedComponent(disabled, shared);
            Manager.SetComponentEnabled<EnabledTag>(disabled, false);
        }

        private struct SharedGroup : ISharedComponentData
        {
            public int Value;
        }

        private struct EnabledTag : IComponentData, IEnableableComponent
        {
        }

        private struct ExtraTag : IComponentData
        {
        }

        [BurstCompile]
        private struct CountEntitiesJob : IJobChunkWorkerBeginEnd
        {
            [WriteOnly]
            public NativeArray<int> EntityCounts;

            public NativeQueue<int>.ParallelWriter WorkerCounts;

            private bool _workerStarted;
            private int _visitedChunks;

            public void OnWorkerBegin()
            {
                _workerStarted = true;
                _visitedChunks = 0;
            }

            public void OnWorkerEnd()
            {
                WorkerCounts.Enqueue(_workerStarted ? _visitedChunks : -1);
            }

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                var count = 0;
                while (entities.NextEntityIndex(out _))
                {
                    count++;
                }

                EntityCounts[unfilteredChunkIndex] = count;
                _visitedChunks++;
            }
        }
    }
}
