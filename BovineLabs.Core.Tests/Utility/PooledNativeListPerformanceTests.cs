// <copyright file="PooledNativeListPerformanceTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using BovineLabs.Core.Utility;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.PerformanceTesting;

    public partial class PooledNativeListPerformanceTests : ECSTestsFixture
    {
        private const int EntityCount = 10_000;
        private const int BufferCount = 1_000;

        [Test]
        [Performance]
        public void PoolTest()
        {
            var archetype = this.World.EntityManager.CreateArchetype(typeof(TestBuffer), typeof(TestResult));
            var entities = this.World.EntityManager.CreateEntity(archetype, EntityCount, Allocator.Temp);
            foreach (var e in entities)
            {
                var buffer = this.World.EntityManager.GetBuffer<TestBuffer>(e);
                buffer.ResizeUninitialized(BufferCount);
            }

            var system = this.World.CreateSystem<System1>();

            Measure.Method(() => system.Update(this.WorldUnmanaged)).WarmupCount(5).MeasurementCount(10).Run();
        }

        [Test]
        [Performance]
        public void TempList()
        {
            var archetype = this.World.EntityManager.CreateArchetype(typeof(TestBuffer), typeof(TestResult));
            var entities = this.World.EntityManager.CreateEntity(archetype, EntityCount, Allocator.Temp);
            foreach (var e in entities)
            {
                var buffer = this.World.EntityManager.GetBuffer<TestBuffer>(e);
                buffer.ResizeUninitialized(BufferCount);
            }

            var system = this.World.CreateSystem<System2>();

            Measure.Method(() => system.Update(this.WorldUnmanaged)).WarmupCount(5).MeasurementCount(10).Run();
        }

        [Test]
        [Performance]
        public void StackAlloc()
        {
            var archetype = this.World.EntityManager.CreateArchetype(typeof(TestBuffer), typeof(TestResult));
            var entities = this.World.EntityManager.CreateEntity(archetype, EntityCount, Allocator.Temp);
            foreach (var e in entities)
            {
                var buffer = this.World.EntityManager.GetBuffer<TestBuffer>(e);
                buffer.ResizeUninitialized(BufferCount);
            }

            var system = this.World.CreateSystem<System3>();

            Measure.Method(() => system.Update(this.WorldUnmanaged)).WarmupCount(5).MeasurementCount(10).Run();
        }

        public partial struct System1 : ISystem
        {
            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                new PoolListJob().ScheduleParallel();

                state.Dependency.Complete();
            }

            [BurstCompile]
            private partial struct PoolListJob : IJobEntity
            {
                private static void Execute(in DynamicBuffer<TestBuffer> counts, ref TestResult result)
                {
                    // Get a list from the pool
                    using var intList = PooledNativeList<int>.Make();

                    foreach (var i in counts)
                    {
                        if (i.Value % 10 == 0)
                        {
                            intList.List.Add(i.Value);
                        }
                    }

                    var sum = 0;
                    foreach (var a in intList.List.AsArray())
                    {
                        sum += a;
                    }

                    result.Value = sum;
                }
            }
        }

        public partial struct System2 : ISystem
        {
            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                new TempListJob().ScheduleParallel();

                state.Dependency.Complete();
            }

            [BurstCompile]
            private partial struct TempListJob : IJobEntity
            {
                private static void Execute(in DynamicBuffer<TestBuffer> counts, ref TestResult result)
                {
                    // Get a list from the pool
                    var list = new NativeList<int>(counts.Length, Allocator.Temp);

                    foreach (var i in counts)
                    {
                        if (i.Value % 10 == 0)
                        {
                            list.Add(i.Value);
                        }
                    }

                    var sum = 0;
                    foreach (var a in list.AsArray())
                    {
                        sum += a;
                    }

                    result.Value = sum;
                }
            }
        }

        public partial struct System3 : ISystem
        {
            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                new StackAllocJob().ScheduleParallel();

                state.Dependency.Complete();
            }

            [BurstCompile]
            private unsafe partial struct StackAllocJob : IJobEntity
            {
                private static void Execute(in DynamicBuffer<TestBuffer> counts, ref TestResult result)
                {
                    var sums = stackalloc int[counts.Length];
                    var index = 0;

                    foreach (var i in counts)
                    {
                        if (i.Value % 10 == 0)
                        {
                            sums[index++] = i.Value;
                        }
                    }

                    var sum = 0;
                    for (var i = 0; i < index; i++)
                    {
                        sum += sums[i];
                    }

                    result.Value = sum;
                }
            }
        }

        [InternalBufferCapacity(0)]
        private struct TestBuffer : IBufferElementData
        {
            public int Value;
        }

        private struct TestResult : IComponentData
        {
            public int Value;
        }
    }
}
