namespace BovineLabs.Core.Tests.Collections
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BovineLabs.Core.Collections;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public class UnsafeListPoolTests
    {
        [Test]
        public void TryAdd_ParallelProducers_ReturnsEachValueExactlyOnce()
        {
            const int count = 16 * 1024;
            var pool = new UnmanagedPool<int>(count, Allocator.Persistent);

            try
            {
                var ticket = 0;
                Parallel.For(
                    0,
                    Math.Max(2, Environment.ProcessorCount),
                    _ =>
                    {
                        while (true)
                        {
                            var value = Interlocked.Increment(ref ticket) - 1;
                            if (value >= count)
                            {
                                return;
                            }

                            if (!pool.TryAdd(value))
                            {
                                throw new InvalidOperationException($"Failed to add value {value} to UnmanagedPool.");
                            }
                        }
                    });

                var seen = new int[count];

                for (var i = 0; i < count; i++)
                {
                    Assert.IsTrue(pool.TryGet(out var value), $"Expected pooled value at index {i}.");
                    Assert.That(value, Is.InRange(0, count - 1));
                    seen[value] += 1;
                }

                Assert.IsFalse(pool.TryGet(out _));

                for (var i = 0; i < count; i++)
                {
                    Assert.AreEqual(1, seen[i], $"Unexpected add/pop count for value {i}.");
                }
            }
            finally
            {
                pool.Dispose();
            }
        }

        [Test]
        [TestLeakDetection]
        public void Dispose_WhenPoolContainsLists_DisposesReturnedLists()
        {
            var pool = new UnsafeListPool<int>(8, Allocator.Persistent);
            var disposed = false;

            try
            {
                for (var i = 0; i < 8; i++)
                {
                    Assert.IsTrue(pool.TryAdd(CreateList(i)));
                }

                pool.Dispose();
                disposed = true;
            }
            finally
            {
                if (!disposed)
                {
                    DisposePool(pool);
                }
            }
        }

        [Test]
        public void GetOrCreate_WhenPoolIsEmpty_ReturnsValidList()
        {
            var pool = new UnsafeListPool<int>(4, Allocator.Persistent);

            try
            {
                var list = pool.GetOrCreate(8, Allocator.Persistent);

                Assert.IsTrue(list.IsCreated);
                Assert.AreEqual(0, list.Length);

                pool.ReturnOrDispose(list);
            }
            finally
            {
                DisposePool(pool);
            }
        }

        [Test]
        public void ReturnOrDispose_WhenPoolIsFull_DisposesList()
        {
            var pool = new UnsafeListPool<int>(1, Allocator.Persistent);

            try
            {
                var i = 0;
                while (true)
                {
                    var list = CreateList(1000 + i);
                    if (!pool.TryAdd(list))
                    {
                        list.Dispose();
                        break;
                    }

                    i++;
                    if (i > 1024)
                    {
                        Assert.Fail("Pool did not reject adds within expected bounds.");
                    }
                }

                const int marker = 99_999;
                pool.ReturnOrDispose(CreateList(marker));

                var foundMarker = false;
                while (pool.TryGet(out var pooled))
                {
                    foundMarker |= pooled[0] == marker;
                    pooled.Dispose();
                }

                Assert.IsFalse(foundMarker, "Overflow list should have been disposed instead of pooled.");
            }
            finally
            {
                DisposePool(pool);
            }
        }

        [Test]
        public void TryGet_ParallelConsumers_ReturnsEachListExactlyOnce()
        {
            const int count = 16 * 1024;
            var pool = new UnsafeListPool<int>(count, Allocator.Persistent);

            try
            {
                for (var i = 0; i < count; i++)
                {
                    Assert.IsTrue(pool.TryAdd(CreateList(i)));
                }

                var seen = new int[count];
                var nextTicket = 0;
                var workerCount = Math.Max(2, Environment.ProcessorCount);

                Parallel.For(
                    0,
                    workerCount,
                    _ =>
                    {
                        while (true)
                        {
                            var ticket = Interlocked.Increment(ref nextTicket) - 1;
                            if (ticket >= count)
                            {
                                return;
                            }

                            UnsafeList<int> list;
                            while (!pool.TryGet(out list))
                            {
                                Thread.SpinWait(1);
                            }

                            var value = list[0];
                            Interlocked.Increment(ref seen[value]);
                            list.Dispose();
                        }
                    });

                Assert.IsFalse(pool.TryGet(out _));

                for (var i = 0; i < count; i++)
                {
                    Assert.AreEqual(1, seen[i], $"Unexpected pop count for value {i}");
                }
            }
            finally
            {
                DisposePool(pool);
            }
        }

        [Test]
        public unsafe void TryGet_BurstIJobFor_ParallelConsumers_ReturnEachListExactlyOnce()
        {
            const int count = 16 * 1024;
            var pool = new UnsafeListPool<int>(count, Allocator.Persistent);
            var seen = new NativeArray<int>(count, Allocator.TempJob);
            var failures = new NativeArray<int>(1, Allocator.TempJob);

            try
            {
                for (var i = 0; i < count; i++)
                {
                    Assert.IsTrue(pool.TryAdd(CreateList(i)));
                }

                var job = new BurstPopJob
                {
                    Pool = pool,
                    Seen = seen,
                    Failures = failures,
                };

                job.ScheduleParallel(count, 64, default).Complete();

                Assert.AreEqual(0, failures[0], "Some job workers failed to pop from the pool.");
                for (var i = 0; i < count; i++)
                {
                    Assert.AreEqual(1, seen[i], $"Unexpected pop count for value {i}");
                }
            }
            finally
            {
                seen.Dispose();
                failures.Dispose();
                DisposePool(pool);
            }
        }

        private static UnsafeList<int> CreateList(int value)
        {
            var list = new UnsafeList<int>(1, Allocator.Persistent);
            list.Add(value);
            return list;
        }

        private static void DisposePool(UnsafeListPool<int> pool)
        {
            while (pool.TryGet(out var list))
            {
                if (list.IsCreated)
                {
                    list.Dispose();
                }
            }

            pool.Dispose();
        }

        [BurstCompile]
        private unsafe struct BurstPopJob : IJobFor
        {
            public UnsafeListPool<int> Pool;

            [NativeDisableParallelForRestriction]
            public NativeArray<int> Seen;

            [NativeDisableParallelForRestriction]
            public NativeArray<int> Failures;

            public void Execute(int index)
            {
                if (!Pool.TryGet(out var list))
                {
                    var failurePtr = (int*)Failures.GetUnsafePtr();
                    Interlocked.Increment(ref failurePtr[0]);
                    return;
                }

                var value = list[0];
                var seenPtr = (int*)Seen.GetUnsafePtr();
                Interlocked.Increment(ref seenPtr[value]);
                list.Dispose();
            }
        }
    }
}
