// <copyright file="IJobParallelHashMapDefer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Jobs
{
    using BovineLabs.Core.Jobs;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    public class IJobParallelHashMapDefer
    {
        [TestCase(0)]
        [TestCase(1000000)]
        public void Schedule(int count)
        {
            var hashMap = new NativeParallelHashMap<int, int>(0, Allocator.Persistent);
            var result = new NativeQueue<byte>(Allocator.Persistent);

            var dependency = new ResizeJob
            {
                Count = count,
                HashMap = hashMap,
            }.Schedule();

            dependency = new CountJob
            {
                HashMap = hashMap,
                Count = result.AsParallelWriter(),
            }.ScheduleParallel(hashMap, 64, dependency);

            dependency.Complete();

            Assert.AreEqual(count, result.Count);

            result.Dispose();
            hashMap.Dispose();
        }

        [BurstCompile]
        private struct ResizeJob : IJob
        {
            public int Count;
            public NativeParallelHashMap<int, int> HashMap;

            public void Execute()
            {
                for (var i = 0; i < this.Count; i++)
                {
                    this.HashMap.Add(i, i);
                }
            }
        }

        [BurstCompile]
        private struct CountJob : Core.Jobs.IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<int, int> HashMap;

            public NativeQueue<byte>.ParallelWriter Count;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Count.Enqueue(0);
            }
        }
    }
}
