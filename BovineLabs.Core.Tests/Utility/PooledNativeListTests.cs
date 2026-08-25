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
            // Arrange
            int initialCapacity;

            // Act & Assert - First use
            using (var pooledList = PooledNativeList<int>.Make())
            {
                pooledList.List.Add(1);
                pooledList.List.Add(2);
                pooledList.List.Add(3);
                initialCapacity = pooledList.List.Capacity;
            }

            // Act & Assert - Second use, should reuse the same underlying list
            using (var pooledList = PooledNativeList<int>.Make())
            {
                // The list should be empty but have the same or larger capacity
                Assert.AreEqual(0, pooledList.List.Length);
                Assert.GreaterOrEqual(pooledList.List.Capacity, initialCapacity);
            }
        }

        [Test]
        public void ThreadSafety_MultipleLists_FromParallelJobs()
        {
            // This test verifies that the pool is thread-safe by getting lists from multiple jobs
            var jobCount = math.min(10, Unity.Jobs.LowLevel.Unsafe.JobsUtility.ThreadIndexCount * 2);
            var results = new NativeArray<JobResult>(jobCount, Allocator.TempJob);

            // Create parallel jobs that each get a list, add items, and record results
            var jobHandle = new AddItemsToPooledListJob
            {
                Results = results,
                ItemsToAdd = 5,
            }.ScheduleParallel(jobCount, 1, default);

            // Wait for all jobs to complete
            jobHandle.Complete();

            // Verify results
            for (var i = 0; i < jobCount; i++)
            {
                Assert.AreEqual(5, results[i].ListLength, $"Job {i} did not add the expected number of items");
                Assert.AreNotEqual(-1, results[i].ThreadIndex, $"Job {i} did not record a valid thread index");
                // Make sure thread index is within bounds of our pool
                Assert.Less(results[i].ThreadIndex, Unity.Jobs.LowLevel.Unsafe.JobsUtility.ThreadIndexCount,
                    $"Job {i} recorded an out-of-bounds thread index");
            }

            // Verify that at least some valid thread indices were recorded
            // Note: We don't assert that multiple threads were used since Unity's job scheduler
            // may choose to run all jobs on a single thread depending on system load and configuration
            var uniqueThreadIndices = new NativeHashSet<int>(jobCount, Allocator.Temp);
            for (var i = 0; i < jobCount; i++)
            {
                uniqueThreadIndices.Add(results[i].ThreadIndex);
            }

            Assert.Greater(uniqueThreadIndices.Count, 0, "No valid thread indices were recorded");

            uniqueThreadIndices.Dispose();
            results.Dispose();
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
        public void BurstCompatibility_ParallelJobs_WorkCorrectly()
        {
            // This test verifies that the pool works correctly with Burst-compiled parallel jobs

            var itemCount = 512;
            var results = new NativeArray<int>(itemCount, Allocator.TempJob);

            // Schedule multiple jobs that use both int and float3 lists
            var jobHandle = new BurstCompatibilityTestJob
            {
                Results = results,
            }.ScheduleParallel(itemCount, 16, default);

            // Wait for all jobs to complete
            jobHandle.Complete();

            // Verify all jobs completed successfully
            for (var i = 0; i < itemCount; i++)
            {
                // Each result should contain a value indicating successful execution
                Assert.AreEqual(1, results[i], $"Job {i} did not complete successfully");
            }

            results.Dispose();
        }

        private struct JobResult
        {
            public int ListLength;
            public int ThreadIndex;
        }

        [BurstCompile]
        private struct AddItemsToPooledListJob : IJobFor
        {
            public NativeArray<JobResult> Results;

            public int ItemsToAdd;

            public void Execute(int index)
            {
                // Store thread index before any other operations
                var threadIndex = Unity.Jobs.LowLevel.Unsafe.JobsUtility.ThreadIndex;

                using var pooledList = PooledNativeList<int>.Make();
                for (var i = 0; i < this.ItemsToAdd; i++)
                {
                    pooledList.List.Add(i);
                }

                // Record results before the list is disposed and cleared
                this.Results[index] = new JobResult
                {
                    ListLength = pooledList.List.Length,
                    ThreadIndex = threadIndex,
                };
            }
        }

        [BurstCompile]
        private struct BackToBackTestJob : IJobFor
        {
            public int BatchIndex;

            public NativeArray<int> Results;

            public void Execute(int index)
            {
                // Get a list from the pool
                using var intList = PooledNativeList<int>.Make();

                // Add some items to the list
                var itemCount = (index % 10) + 1; // 1 to 10 items
                for (var i = 0; i < itemCount; i++)
                {
                    intList.List.Add(i + this.BatchIndex);
                }

                // Use the same pool to get another list of a different type
                using var floatList = PooledNativeList<float>.Make();

                // Add some items to this list too
                for (var i = 0; i < itemCount; i++)
                {
                    floatList.List.Add(i + this.BatchIndex);
                }

                // Record the batch index in the results
                this.Results[index] = this.BatchIndex;
            }
        }

        [BurstCompile]
        private struct BurstCompatibilityTestJob : IJobFor
        {
            public NativeArray<int> Results;

            public void Execute(int index)
            {
                // Randomly choose between float and float3 lists
                if ((index % 2) == 0)
                {
                    // Get and use a float list
                    using var list = PooledNativeList<float>.Make();

                    // Add a variable number of elements based on index
                    var count = (index % 16) + 1;
                    for (var i = 0; i < count; i++)
                    {
                        list.List.Add(i);
                    }

                    // Mark as successful
                    this.Results[index] = 1;
                }
                else
                {
                    // Get and use a float3 list
                    using var list = PooledNativeList<float3>.Make();

                    // Add a variable number of elements based on index
                    var count = (index % 16) + 1;
                    for (var i = 0; i < count; i++)
                    {
                        list.List.Add(new float3(i));
                    }

                    // Mark as successful
                    this.Results[index] = 1;
                }
            }
        }
    }
}
