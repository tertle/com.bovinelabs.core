// <copyright file="NativeLookupTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    public class NativeLookupTests
    {
        [Test]
        public void AddBatch()
        {
            var lookup = new NativeLookup<int, int>(2, Allocator.Temp);

            var keys = new NativeArray<int>(5, Allocator.Temp);
            keys[0] = 1;
            keys[1] = 2;
            keys[2] = 3;
            keys[3] = 4;
            keys[4] = 5;

            var values = new NativeArray<int>(5, Allocator.Temp);
            values[0] = 5;
            values[1] = 4;
            values[2] = 3;
            values[3] = 2;
            values[4] = 1;

            lookup.AddBatch(keys, values);

            Assert.AreEqual(5, lookup[1]);
            Assert.AreEqual(4, lookup[2]);
            Assert.AreEqual(3, lookup[3]);
            Assert.AreEqual(2, lookup[4]);
            Assert.AreEqual(1, lookup[5]);
        }

        [Test]
        public void ParallelWriterTest()
        {
            const int indices = 128;

            var lookup = new NativeLookup<int, int>(ParallelTestJob.PerIndex * indices, Allocator.TempJob);

            new ParallelTestJob
                {
                    Lookup = lookup.AsParallelWriter(),
                }
                .ScheduleParallel(indices, 64, default).Complete();

            for (var i = 0; i < ParallelTestJob.PerIndex * indices; i++)
            {
                Assert.AreEqual(i, lookup[i]);
            }

            lookup.Dispose();
        }

        [BurstCompile]
        private struct ParallelTestJob : IJobFor
        {
            public const int PerIndex = 64;

            public NativeLookup<int, int>.ParallelWriter Lookup;

            public void Execute(int index)
            {
                var keys = new NativeArray<int>(PerIndex, Allocator.Temp);
                var values = new NativeArray<int>(PerIndex, Allocator.Temp);

                for (var i = 0; i < PerIndex; i++)
                {
                    keys[i] = (index * PerIndex) + i;
                    values[i] = keys[i];
                }

                this.Lookup.AddBatch(keys, values);
            }
        }
    }
}
