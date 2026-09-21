namespace BovineLabs.Core.Tests.Collections.ThreadStream
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    internal partial class ThreadWriter : ECSTestsFixture
    {

        [Test]
        public void ItemCount([Values(JobsUtility.MaxJobThreadCount + 1)] int count)
        {
            using var stream = new NativeThreadStream(Allocator.TempJob);
            var fillInts = new WriteIntsJob { Writer = stream.AsWriter() };
            fillInts.ScheduleParallel(count, 1, default).Complete();

            Assert.AreEqual((count * (count - 1)) / 2, stream.Count());
        }

        [Test]
        public void WriteRead([Values(JobsUtility.MaxJobThreadCount + 1)] int count)
        {
            using var stream = new NativeThreadStream(Allocator.TempJob);
            var fillInts = new WriteIntsJob { Writer = stream.AsWriter() };
            var jobHandle = fillInts.ScheduleParallel(count, 1, default);

            var compareInts = new ReadIntsJob { JobReader = stream.AsReader() };
            var res0 = compareInts.ScheduleParallel(UnsafeThreadStream.ForEachCount, 1, jobHandle);
            var res1 = compareInts.ScheduleParallel(UnsafeThreadStream.ForEachCount, 1, jobHandle);

            res0.Complete();
            res1.Complete();
        }

        [Test]
        public void SystemBaseEntitiesForeach([Values(JobsUtility.MaxJobThreadCount + 1)] int count)
        {
            var system = World.AddSystemManaged(new CodeGenTestSystem(count));
            system.Update();
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct WriteIntsJob : IJobFor
        {
            public NativeThreadStream.Writer Writer;

#pragma warning disable 649
            [NativeSetThreadIndex]
            private int _threadIndex;
#pragma warning restore 649

            public void Execute(int index)
            {
                for (var i = 0; i != index; i++)
                {
                    Writer.Write(_threadIndex);
                }
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct ReadIntsJob : IJobFor
        {
            [ReadOnly]
            public NativeThreadStream.Reader JobReader;

            public void Execute(int index)
            {
                var count = JobReader.BeginForEachIndex(index);

                for (var i = 0; i != count; i++)
                {
                    var value = JobReader.Read<int>();

                    UnityEngine.Assertions.Assert.AreEqual(index, value);
                }
            }
        }

        [DisableAutoCreation]
        private partial class CodeGenTestSystem : SystemBase
        {
            private readonly int _count;
            private NativeParallelHashMap<int, byte> _hashmap;

            public CodeGenTestSystem(int count)
            {
                _count = count;
            }

            protected override void OnCreate()
            {
                var arch = EntityManager.CreateArchetype(typeof(TestComponent));

                using var entities = new NativeArray<Entity>(_count, Allocator.Temp);
                EntityManager.CreateEntity(arch, entities);

                for (var index = 0; index < entities.Length; index++)
                {
                    var entity = entities[index];

                    EntityManager.SetComponentData(entity, new TestComponent { Value = index });
                }

                _hashmap = new NativeParallelHashMap<int, byte>(_count, Allocator.Persistent);
            }

            protected override void OnDestroy()
            {
                _hashmap.Dispose();
            }

            protected override void OnUpdate()
            {
                JobEntityTest();
                JobTest();
            }

            private void JobEntityTest()
            {
                var stream = new NativeThreadStream(Allocator.TempJob);
                NativeThreadStream.Writer writer = stream.AsWriter();

                Dependency = new JobEntityJob { Writer = writer }.ScheduleParallel(Dependency);

                Dependency = new ReadJob
                    {
                        JobReader = stream.AsReader(),
                        HashMap = _hashmap.AsParallelWriter(),
                    }
                    .ScheduleParallel(UnsafeThreadStream.ForEachCount, 1, Dependency);

                // this.Dependency = stream.Dispose(this.Dependency);
                Dependency.Complete();

                // Assert correct values were added
                for (var i = 0; i < _count; i++)
                {
                    Assert.IsTrue(_hashmap.TryGetValue(i, out _));
                }

                _hashmap.Clear();
                stream.Dispose();
            }

            private void JobTest()
            {
                var stream = new NativeThreadStream(Allocator.TempJob);
                var writer = stream.AsWriter();

                var c = _count;

                Dependency = new JobTestJob
                {
                    Writer = writer,
                    Count = c,
                }.Schedule(Dependency);

                Dependency = new ReadJob
                    {
                        JobReader = stream.AsReader(),
                        HashMap = _hashmap.AsParallelWriter(),
                    }
                    .ScheduleParallel(UnsafeThreadStream.ForEachCount, 1, Dependency);

                // this.Dependency = stream.Dispose(this.Dependency);
                Dependency.Complete();

                // Assert correct values were added
                for (var i = 0; i < _count; i++)
                {
                    Assert.IsTrue(_hashmap.TryGetValue(i, out _));
                }

                _hashmap.Clear();
                stream.Dispose();
            }

            [BurstCompile(CompileSynchronously = true)]
            private partial struct JobEntityJob : IJobEntity
            {
                public NativeThreadStream.Writer Writer;

                private void Execute(in TestComponent test)
                {
                    Writer.Write(test.Value);
                }
            }

            [BurstCompile(CompileSynchronously = true)]
            private struct JobTestJob : IJob
            {
                public NativeThreadStream.Writer Writer;
                public int Count;

                public void Execute()
                {
                    for (var i = 0; i < Count; i++)
                    {
                        Writer.Write(i);
                    }
                }
            }

            [BurstCompile(CompileSynchronously = true)]
            private struct ReadJob : IJobFor
            {
                [ReadOnly]
                public NativeThreadStream.Reader JobReader;

                public NativeParallelHashMap<int, byte>.ParallelWriter HashMap;

                public void Execute(int index)
                {
                    var count = JobReader.BeginForEachIndex(index);

                    for (var i = 0; i != count; i++)
                    {
                        var value = JobReader.Read<int>();
                        HashMap.TryAdd(value, 0);
                    }
                }
            }

            private struct TestComponent : IComponentData
            {
                public int Value;
            }
        }
    }
}
