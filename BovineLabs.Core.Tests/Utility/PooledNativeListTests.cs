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
        public void Get_ReturnsValidList()
        {
            // Act
            using var pooledList = PooledNativeList<int>.Make();

            // Assert
            Assert.IsNotNull(pooledList.List);
            Assert.AreEqual(0, pooledList.List.Length);
            Assert.GreaterOrEqual(pooledList.List.Capacity, 0);
        }

        [Test]
        public void PooledList_CanBeUsedLikeNormalList()
        {
            // Arrange
            using var pooledList = PooledNativeList<int>.Make();

            // Act
            pooledList.List.Add(42);
            pooledList.List.Add(24);

            // Assert
            Assert.AreEqual(2, pooledList.List.Length);
            Assert.AreEqual(42, pooledList.List[0]);
            Assert.AreEqual(24, pooledList.List[1]);
        }

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
        public void DifferentTypes_CanBeStored()
        {
            // Test that the pool can store different types
            using (var intList = PooledNativeList<int>.Make())
            {
                intList.List.Add(42);
                Assert.AreEqual(1, intList.List.Length);
            }

            using (var floatList = PooledNativeList<float>.Make())
            {
                floatList.List.Add(3.14f);
                Assert.AreEqual(1, floatList.List.Length);
            }

            using (var vector3List = PooledNativeList<float3>.Make())
            {
                vector3List.List.Add(new float3(1, 2, 3));
                Assert.AreEqual(1, vector3List.List.Length);
            }
        }

        [Test]
        public void EmptyPool_CreatesNewList()
        {
            // Get a bunch of lists to try to exhaust the pool
            var lists = new PooledNativeList<int>[10];

            // Act - Get lists until we exhaust the pool and need to create a new one
            for (var i = 0; i < lists.Length; i++)
            {
                lists[i] = PooledNativeList<int>.Make();
            }

            // Assert - Verify all lists work
            for (var i = 0; i < lists.Length; i++)
            {
                lists[i].List.Add(i);
                Assert.AreEqual(1, lists[i].List.Length);
                Assert.AreEqual(i, lists[i].List[0]);
            }

            // Clean up
            for (var i = 0; i < lists.Length; i++)
            {
                lists[i].Dispose();
            }
        }

        [Test]
        public void CapacityConversion_BetweenDifferentSizedTypes()
        {
            // Test capacity conversion between types of different sizes

            // First use with a small type (int - 4 bytes)
            int smallTypeCapacity;
            using (var intList = PooledNativeList<int>.Make())
            {
                // Add items to ensure the capacity is set
                for (var i = 0; i < 100; i++)
                {
                    intList.List.Add(i);
                }

                smallTypeCapacity = intList.List.Capacity;
            }

            // Then use with a larger type (double - 8 bytes)
            using (var doubleList = PooledNativeList<double>.Make())
            {
                // The capacity should be approximately half (due to size difference)
                // Allow some tolerance for pooling overhead and rounding
                Assert.LessOrEqual(doubleList.List.Capacity, smallTypeCapacity);
                Assert.GreaterOrEqual(doubleList.List.Capacity * 2 + 1, smallTypeCapacity);

                // Ensure the list is still usable
                for (var i = 0; i < 50; i++)
                {
                    doubleList.List.Add(i);
                }

                Assert.AreEqual(50, doubleList.List.Length);
            }

            // Then go back to the smaller type
            using (var intList = PooledNativeList<int>.Make())
            {
                // The capacity should be approximately double (due to size difference)
                Assert.GreaterOrEqual(intList.List.Capacity, smallTypeCapacity / 2);

                // Ensure the list is still usable
                for (var i = 0; i < 100; i++)
                {
                    intList.List.Add(i);
                }

                Assert.AreEqual(100, intList.List.Length);
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
        public void BackToBackJobs_WorkCorrectly()
        {
            // This test verifies that the pool works correctly with multiple batches of jobs
            // scheduled one after another with dependencies

            var totalBatches = 5;
            var itemsPerBatch = 100;
            var resultsPerBatch = new NativeArray<NativeArray<int>>(totalBatches, Allocator.Temp);

            // Create arrays to store results from each batch
            for (var batch = 0; batch < totalBatches; batch++)
            {
                resultsPerBatch[batch] = new NativeArray<int>(itemsPerBatch, Allocator.TempJob);
            }

            // Schedule multiple batches of jobs sequentially, each dependent on the previous
            var dependsOn = default(JobHandle);

            for (var batch = 0; batch < totalBatches; batch++)
            {
                // Schedule a batch of jobs
                dependsOn = new BackToBackTestJob
                {
                    BatchIndex = batch,
                    Results = resultsPerBatch[batch],
                }.ScheduleParallel(itemsPerBatch, 4, dependsOn);
            }

            // Wait for all jobs to complete
            dependsOn.Complete();

            // Verify results from all batches
            for (var batch = 0; batch < totalBatches; batch++)
            {
                for (var i = 0; i < itemsPerBatch; i++)
                {
                    // Each item should have the correct batch index stored
                    Assert.AreEqual(batch, resultsPerBatch[batch][i],
                        $"Item {i} in batch {batch} does not have the correct batch index");
                }
            }

            // Dispose all result arrays
            for (var batch = 0; batch < totalBatches; batch++)
            {
                resultsPerBatch[batch].Dispose();
            }

            resultsPerBatch.Dispose();
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
