// <copyright file="NativeThreadStreamThreadWriterTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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

    /// <summary> Tests for thread based implementation. </summary>
    internal partial class ThreadWriter : ECSTestsFixture
    {
        // /// <summary> Tests that the dispose job works. </summary>
        // /// <remarks> The stream will be marked as not created straight away. </remarks>
        // [Test]
        // public void DisposeJob()
        // {
        //     var stream = NativeThreadStream.Create(ref this.World.Unmanaged.UpdateAllocator);
        //     Assert.IsTrue(stream.IsCreated);
        //
        //     var fillInts = new WriteIntsJob { Writer = stream.AsWriter() };
        //     var writerJob = fillInts.ScheduleParallel(JobsUtility.MaxJobThreadCount, 16, default);
        //
        //     var disposeJob = stream.Dispose(writerJob);
        //     Assert.IsFalse(stream.IsCreated);
        //
        //     disposeJob.Complete();
        // }

        /// <summary> Tests that ComputeItemCount() works. </summary>
        /// <param name="count"> <see cref="WriteIntsJob" /> count. </param>
        /// <param name="batchSize"> <see cref="WriteIntsJob" /> batch size. </param>
        [Test]
        public void ItemCount(
            [Values(1, 10, JobsUtility.MaxJobThreadCount + 1, 1024)]
            int count,
            [Values(1, 3, 10, 128)] int batchSize)
        {
            using var stream = new NativeThreadStream(Allocator.TempJob);
            var fillInts = new WriteIntsJob { Writer = stream.AsWriter() };
            fillInts.ScheduleParallel(count, batchSize, default).Complete();

            Assert.AreEqual((count * (count - 1)) / 2, stream.Count());
        }

        /// <summary> Tests that writing from job then reading in multiple jobs works. </summary>
        /// <param name="count"> <see cref="WriteIntsJob" /> count. </param>
        /// <param name="batchSize"> <see cref="WriteIntsJob" /> batch size. </param>
        [Test]
        public void WriteRead(
            [Values(1, 10, JobsUtility.MaxJobThreadCount + 1)]
            int count,
            [Values(1, 3, 10)] int batchSize)
        {
            using var stream = new NativeThreadStream(Allocator.TempJob);
            var fillInts = new WriteIntsJob { Writer = stream.AsWriter() };
            var jobHandle = fillInts.ScheduleParallel(count, batchSize, default);

            var compareInts = new ReadIntsJob { JobReader = stream.AsReader() };
            var res0 = compareInts.ScheduleParallel(UnsafeThreadStream.ForEachCount, batchSize, jobHandle);
            var res1 = compareInts.ScheduleParallel(UnsafeThreadStream.ForEachCount, batchSize, jobHandle);

            res0.Complete();
            res1.Complete();
        }

        /// <summary> Tests the container working in an Entities.ForEach in SystemBase. </summary>
        /// <param name="count"> The number of entities to test. </param>
        [Test]
        public void SystemBaseEntitiesForeach([Values(1, JobsUtility.MaxJobThreadCount + 1, 100000)] int count)
        {
            var system = this.World.AddSystemManaged(new CodeGenTestSystem(count));
            system.Update();
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct WriteIntsJob : IJobFor
        {
            public NativeThreadStream.Writer Writer;

#pragma warning disable 649
            [NativeSetThreadIndex]
            private int threadIndex;
#pragma warning restore 649

            public void Execute(int index)
            {
                for (var i = 0; i != index; i++)
                {
                    this.Writer.Write(this.threadIndex);
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
                var count = this.JobReader.BeginForEachIndex(index);

                for (var i = 0; i != count; i++)
                {
                    var value = this.JobReader.Read<int>();

                    UnityEngine.Assertions.Assert.AreEqual(index, value);
                }
            }
        }

        [DisableAutoCreation]
        private partial class CodeGenTestSystem : SystemBase
        {
            private readonly int count;
            private NativeParallelHashMap<int, byte> hashmap;

            public CodeGenTestSystem(int count)
            {
                this.count = count;
            }

            protected override void OnCreate()
            {
                var arch = this.EntityManager.CreateArchetype(typeof(TestComponent));

                using var entities = new NativeArray<Entity>(this.count, Allocator.Temp);
                this.EntityManager.CreateEntity(arch, entities);

                for (var index = 0; index < entities.Length; index++)
                {
                    var entity = entities[index];

                    this.EntityManager.SetComponentData(entity, new TestComponent { Value = index });
                }

                this.hashmap = new NativeParallelHashMap<int, byte>(this.count, Allocator.Persistent);
            }

            protected override void OnDestroy()
            {
                this.hashmap.Dispose();
            }

            protected override void OnUpdate()
            {
                this.JobEntityTest();
                this.JobTest();
            }

            private void JobEntityTest()
            {
                var stream = new NativeThreadStream(Allocator.TempJob);
                NativeThreadStream.Writer writer = stream.AsWriter();

                this.Dependency = new JobEntityJob { Writer = writer }.ScheduleParallel(this.Dependency);

                this.Dependency = new ReadJob
                    {
                        JobReader = stream.AsReader(),
                        HashMap = this.hashmap.AsParallelWriter(),
                    }
                    .ScheduleParallel(UnsafeThreadStream.ForEachCount, 1, this.Dependency);

                // this.Dependency = stream.Dispose(this.Dependency);
                this.Dependency.Complete();

                // Assert correct values were added
                for (var i = 0; i < this.count; i++)
                {
                    Assert.IsTrue(this.hashmap.TryGetValue(i, out _));
                }

                this.hashmap.Clear();
                stream.Dispose();
            }

            private void JobTest()
            {
                var stream = new NativeThreadStream(Allocator.TempJob);
                var writer = stream.AsWriter();

                var c = this.count;

                this.Dependency = new JobTestJob
                {
                    Writer = writer,
                    Count = c,
                }.Schedule(this.Dependency);

                this.Dependency = new ReadJob
                    {
                        JobReader = stream.AsReader(),
                        HashMap = this.hashmap.AsParallelWriter(),
                    }
                    .ScheduleParallel(UnsafeThreadStream.ForEachCount, 1, this.Dependency);

                // this.Dependency = stream.Dispose(this.Dependency);
                this.Dependency.Complete();

                // Assert correct values were added
                for (var i = 0; i < this.count; i++)
                {
                    Assert.IsTrue(this.hashmap.TryGetValue(i, out _));
                }

                this.hashmap.Clear();
                stream.Dispose();
            }

            [BurstCompile(CompileSynchronously = true)]
            private partial struct JobEntityJob : IJobEntity
            {
                public NativeThreadStream.Writer Writer;

                private void Execute(in TestComponent test)
                {
                    this.Writer.Write(test.Value);
                }
            }

            [BurstCompile(CompileSynchronously = true)]
            private struct JobTestJob : IJob
            {
                public NativeThreadStream.Writer Writer;
                public int Count;

                public void Execute()
                {
                    for (var i = 0; i < this.Count; i++)
                    {
                        this.Writer.Write(i);
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
                    var count = this.JobReader.BeginForEachIndex(index);

                    for (var i = 0; i != count; i++)
                    {
                        var value = this.JobReader.Read<int>();
                        this.HashMap.TryAdd(value, 0);
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
