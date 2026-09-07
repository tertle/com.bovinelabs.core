// <copyright file="PooledNativeListTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using System;
    using BovineLabs.Core.Utility;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    public class PooledNativeListTests
    {
        [Test]
        public void GetAndDispose_MultiplePooledLists_ReusesList()
        {
            int initialCapacity;
            using (var pooledList = PooledNativeList<int>.Make())
            {
                pooledList.List.Add(1);
                pooledList.List.Add(2);
                pooledList.List.Add(3);
                initialCapacity = pooledList.List.Capacity;
            }

            using (var pooledList = PooledNativeList<int>.Make())
            {
                Assert.AreEqual(0, pooledList.List.Length);
                Assert.GreaterOrEqual(pooledList.List.Capacity, initialCapacity);
            }
        }

        [Test]
        public void UsingReturnedList_ThrowsInEditor()
        {
            var pooledList = PooledNativeList<int>.Make();
            pooledList.List.Add(1);
            pooledList.Dispose();

            Assert.Catch<InvalidOperationException>(() => pooledList.List.Add(2));
        }

        [Test]
        public void DoubleDisposeFromCopy_ThrowsInEditor()
        {
            var pooledList = PooledNativeList<int>.Make();
            pooledList.List.Add(1);

            var pooledListCopy = pooledList;
            pooledList.Dispose();

            Assert.Catch<InvalidOperationException>(() => pooledListCopy.Dispose());
        }

        [Test]
        public void BurstParallelJobs_PreserveContentsAcrossTypedPools()
        {
            const int itemCount = 512;
            using var results = new NativeArray<JobResult>(itemCount, Allocator.TempJob);
            new ReadPooledListsJob { Results = results }.ScheduleParallel(itemCount, 16, default).Complete();

            for (var i = 0; i < itemCount; i++)
            {
                var count = (i % 16) + 1;
                var sequenceSum = (count * (count - 1)) / 2;
                Assert.AreEqual(count, results[i].IntegerCount, $"Integer list length for job {i}.");
                Assert.AreEqual(count, results[i].VectorCount, $"Vector list length for job {i}.");
                Assert.AreEqual((i * count) + sequenceSum, results[i].IntegerSum, $"Integer list contents for job {i}.");
                Assert.AreEqual(new float3(i * count, sequenceSum, (i * count) + sequenceSum), results[i].VectorSum, $"Vector contents for job {i}.");
            }
        }

        private struct JobResult
        {
            public int IntegerCount;
            public int VectorCount;
            public int IntegerSum;
            public float3 VectorSum;
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct ReadPooledListsJob : IJobFor
        {
            public NativeArray<JobResult> Results;

            public void Execute(int index)
            {
                using var integers = PooledNativeList<int>.Make();
                using var vectors = PooledNativeList<float3>.Make();
                var count = (index % 16) + 1;
                for (var i = 0; i < count; i++)
                {
                    integers.List.Add(index + i);
                    vectors.List.Add(new float3(index, i, index + i));
                }

                var result = new JobResult
                {
                    IntegerCount = integers.List.Length,
                    VectorCount = vectors.List.Length,
                };
                for (var i = 0; i < count; i++)
                {
                    result.IntegerSum += integers.List[i];
                    result.VectorSum += vectors.List[i];
                }

                this.Results[index] = result;
            }
        }
    }
}
