// <copyright file="NativeParallelHashMapFallbackTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using System.Collections.Generic;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Jobs;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    public class NativeParallelHashMapFallbackTests
    {
        [Test]
        public void Add_DuplicateKeyWithinCapacity_ReadsSingleValue()
        {
            var map = new NativeParallelHashMapFallback<int, int>(2, Allocator.TempJob);

            try
            {
                var writer = map.AsWriter();
                Assert.IsTrue(writer.TryAdd(7, 10));
                Assert.IsFalse(writer.TryAdd(7, 20));

                var handle = map.Apply(default, out var reader);
                handle.Complete();

                Assert.AreEqual(1, reader.Count());
                Assert.IsTrue(reader.TryGetValue(7, out var value));
                Assert.AreEqual(10, value);
            }
            finally
            {
                map.Dispose();
            }
        }

        [Test]
        public void Add_DuplicateFallbackKey_ReadsSingleValue()
        {
            var map = new NativeParallelHashMapFallback<int, int>(1, Allocator.TempJob);

            try
            {
                var writer = map.AsWriter();
                writer.Add(1, 10);
                writer.Add(2, 20);
                writer.Add(2, 30);

                var handle = map.Apply(default, out var reader);
                handle.Complete();

                Assert.AreEqual(2, reader.Count());
                Assert.IsTrue(reader.TryGetValue(1, out var first));
                Assert.IsTrue(reader.TryGetValue(2, out var second));
                Assert.AreEqual(10, first);
                Assert.AreEqual(20, second);
            }
            finally
            {
                map.Dispose();
            }
        }

        [Test]
        public void Add_FromParallelJob_DeduplicatesReadEntries()
        {
            var map = new NativeParallelHashMapFallback<int, int>(2, Allocator.TempJob);
            var keys = new NativeArray<int>(new[] { 1, 2, 2, 3, 3, 4, 1, 4 }, Allocator.TempJob);
            var readKeys = new NativeQueue<int>(Allocator.TempJob);

            try
            {
                var handle = new WriteJob
                {
                    Keys = keys,
                    Writer = map.AsWriter(),
                }.ScheduleParallel(keys.Length, 1, default);

                handle = map.Apply(handle, out var reader);

                handle = new ReadKeysJob
                {
                    Map = reader,
                    Keys = readKeys.AsParallelWriter(),
                }.ScheduleParallel(reader, 1, handle);

                handle.Complete();

                var count = 0;
                var results = new HashSet<int>();
                while (readKeys.TryDequeue(out var key))
                {
                    count++;
                    results.Add(key);
                }

                Assert.AreEqual(4, count);
                Assert.AreEqual(4, results.Count);
                Assert.IsTrue(results.SetEquals(new[] { 1, 2, 3, 4 }));
            }
            finally
            {
                readKeys.Dispose();
                keys.Dispose();
                map.Dispose();
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<int> Keys;

            public NativeParallelHashMapFallback<int, int>.ParallelWriter Writer;

            public void Execute(int index)
            {
                var key = this.Keys[index];
                this.Writer.Add(key, key * 10);
            }
        }

        [BurstCompile]
        private struct ReadKeysJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<int, int>.ReadOnly Map;

            public NativeQueue<int>.ParallelWriter Keys;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.Map, entryIndex, out var key, out _);
                this.Keys.Enqueue(key);
            }
        }
    }
}
