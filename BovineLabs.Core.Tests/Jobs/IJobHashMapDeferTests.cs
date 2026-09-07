// <copyright file="IJobHashMapDeferTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Jobs
{
    using BovineLabs.Core.Jobs;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    public class IJobHashMapDeferTests
    {
        [TestCase(0)]
        [TestCase(257)]
        public void Schedule_AfterResizingJob_VisitsEveryKeyValueExactlyOnce(int count)
        {
            using var hashMap = new NativeHashMap<int, int>(0, Allocator.TempJob);
            using var result = new NativeQueue<int2>(Allocator.TempJob);

            var dependency = new ResizeJob
            {
                Count = count,
                HashMap = hashMap,
            }.Schedule();

            dependency = new ReadJob
            {
                HashMap = hashMap,
                Results = result.AsParallelWriter(),
            }.ScheduleParallel(hashMap, 64, dependency);

            dependency.Complete();

            Assert.AreEqual(count, result.Count);
            var seen = new bool[count];
            while (result.TryDequeue(out var pair))
            {
                Assert.That(pair.x, Is.InRange(0, count - 1));
                Assert.IsFalse(seen[pair.x], $"Key {pair.x} was visited more than once.");
                Assert.AreEqual((pair.x * 3) + 1, pair.y);
                seen[pair.x] = true;
            }
        }

        [BurstCompile]
        private struct ResizeJob : IJob
        {
            public int Count;
            public NativeHashMap<int, int> HashMap;

            public void Execute()
            {
                for (var i = 0; i < this.Count; i++)
                {
                    this.HashMap.Add(i, (i * 3) + 1);
                }
            }
        }

        [BurstCompile]
        private struct ReadJob : IJobHashMapDefer
        {
            [ReadOnly]
            public NativeHashMap<int, int> HashMap;

            public NativeQueue<int2>.ParallelWriter Results;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.HashMap, entryIndex, out var key, out var value);
                this.Results.Enqueue(new int2(key, value));
            }
        }
    }
}
