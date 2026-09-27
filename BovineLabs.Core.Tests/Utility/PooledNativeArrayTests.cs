namespace BovineLabs.Core.Tests.Utility
{
    using System;
    using BovineLabs.Core.Utility;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public class PooledNativeArrayTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(257)]
        public void Make_ExposesRequestedLength(int length)
        {
            using var pool = PooledNativeArray<int>.Make(length);
            Assert.AreEqual(length, pool.Array.Length);
        }

        [Test]
        public unsafe void Make_ReusesAndClearsReturnedListStorage()
        {
            void* storage;
            using (var pool = PooledNativeList<int>.Make())
            {
                var list = pool.List;
                list.ResizeUninitialized(32);
                for (var i = 0; i < list.Length; i++)
                {
                    list[i] = 42;
                }

                storage = list.GetUnsafePtr();
            }

            using (var pool = PooledNativeArray<int>.Make(16))
            {
                var array = pool.Array;
                Assert.IsTrue(storage == array.GetUnsafePtr());
                for (var i = 0; i < array.Length; i++)
                {
                    Assert.AreEqual(0, array[i]);
                    array[i] = i;
                }
            }

            using (var pool = PooledNativeList<int>.Make())
            {
                Assert.AreEqual(0, pool.List.Length);
                Assert.IsTrue(storage == pool.List.GetUnsafePtr());
            }
        }

        [Test]
        public unsafe void Make_UninitializedMemoryPreservesReusedStorage()
        {
            void* storage;
            using (var pool = PooledNativeArray<int>.Make(1))
            {
                var array = pool.Array;
                array[0] = 42;
                storage = array.GetUnsafePtr();
            }

            using (var pool = PooledNativeArray<int>.Make(1, NativeArrayOptions.UninitializedMemory))
            {
                Assert.IsTrue(storage == pool.Array.GetUnsafePtr());
                Assert.AreEqual(42, pool.Array[0]);
            }
        }

        [Test]
        public void Dispose_InvalidatesArrayView()
        {
            var pool = PooledNativeArray<int>.Make(1);
            var array = pool.Array;
            pool.Dispose();
            Assert.Catch<InvalidOperationException>(() => _ = array[0]);
        }

        [Test]
        public void Make_NegativeLengthThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PooledNativeArray<int>.Make(-1));
        }

        [Test]
        public void BurstParallelJobs_PreserveIndependentArrayContents()
        {
            using var results = new NativeArray<int>(64, Allocator.TempJob);
            new ReadPooledArraysJob { Results = results }.ScheduleParallel(results.Length, 8, default).Complete();
            for (var i = 0; i < results.Length; i++)
            {
                var count = (i % 16) + 1;
                Assert.AreEqual((i * count) + (count * (count - 1) / 2), results[i]);
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct ReadPooledArraysJob : IJobFor
        {
            public NativeArray<int> Results;

            public void Execute(int index)
            {
                using var pool = PooledNativeArray<int>.Make((index % 16) + 1, NativeArrayOptions.UninitializedMemory);
                var array = pool.Array;
                for (var i = 0; i < array.Length; i++)
                {
                    array[i] = index + i;
                }

                var sum = 0;
                for (var i = 0; i < array.Length; i++)
                {
                    sum += array[i];
                }

                Results[index] = sum;
            }
        }
    }
}
