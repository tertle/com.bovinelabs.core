// <copyright file="NativeLookupPerformanceTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.PerformanceTests.Collections
{
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.PerformanceTesting;

    internal class NativeLookupPerformanceTests
    {
        [Performance]
        // [TestCase(1048576)]
        [TestCase(10485760)]
        public void SingleThreadHashMap(int length)
        {
            var data = this.CreateKeyValue(length);
            var hashMap = new NativeParallelHashMap<int, int>(length, Allocator.Persistent);

            Measure
                .Method(() => new SingleHashMapJob { Hashmap = hashMap, Keys = data.Keys, Values = data.Values }.Run())
                .SetUp(() => hashMap.Clear())
                .Run();

            data.Keys.Dispose();
            data.Values.Dispose();
            hashMap.Dispose();
        }

        [Performance]
        // [TestCase(1048576)]
        [TestCase(10485760)]
        public void SingleThreadLookup(int length)
        {
            var data = this.CreateKeyValue(length);
            var lookup = new NativeLookup<int, int>(length, Allocator.Persistent);

            Measure
                .Method(() => new SingleLookupJob { Lookup = lookup, Keys = data.Keys, Values = data.Values }.Run())
                .SetUp(() => lookup.Clear())
                .Run();

            data.Keys.Dispose();
            data.Values.Dispose();
            lookup.Dispose();
        }

        [Performance]
        // [TestCase(1048576, 256)]
        [TestCase(10485760, 1024)]
        public void ParallelHashMap(int length, int perIndex)
        {
            var data = this.CreateKeyValue(length);
            var hashMap = new NativeParallelHashMap<int, int>(length, Allocator.Persistent);

            Measure
                .Method(() => new MultiHashMapJob
                {
                    PerIndex = perIndex,
                    Hashmap = hashMap.AsParallelWriter(),
                    Keys = data.Keys,
                    Values = data.Values,
                }
                    .ScheduleParallel(length / perIndex, 64, default).Complete())
                .SetUp(() => hashMap.Clear())
                .Run();

            data.Keys.Dispose();
            data.Values.Dispose();
            hashMap.Dispose();
        }

        [Performance]
        // [TestCase(1048576, 256)]
        [TestCase(10485760, 1024)]
        public void ParallelThreadLookup(int length, int perIndex)
        {
            var data = this.CreateKeyValue(length);
            var lookup = new NativeLookup<int, int>(length, Allocator.Persistent);

            Measure
                .Method(() => new MultiLookupJob
                    {
                        PerIndex = perIndex,
                        Lookup = lookup.AsParallelWriter(),
                        Keys = data.Keys,
                        Values = data.Values,
                    }
                    .ScheduleParallel(length / perIndex, 64, default).Complete())
                .SetUp(() => lookup.Clear())
                .Run();

            data.Keys.Dispose();
            data.Values.Dispose();
            lookup.Dispose();
        }
        //
        // [Performance]
        // // [TestCase(1048576, 256)]
        // [TestCase(10485760, 1024)]
        // public void TwoPhaseLookup(int length, int perIndex)
        // {
        //     var data = this.CreateKeyValue(length);
        //     var lookup = new NativeLookup<int, int>(length, Allocator.Persistent);
        //
        //     Measure
        //         .Method(() =>
        //         {
        //             var dependency = new MultiLookupPart1Job
        //                 {
        //                     PerIndex = perIndex,
        //                     Lookup = lookup.AsParallelWriter(),
        //                     Keys = data.Keys,
        //                     Values = data.Values,
        //                 }
        //                 .ScheduleParallel(length / perIndex, 64, default);
        //
        //             new MultiLookupPart2Job { Lookup = lookup }.Schedule(dependency).Complete();
        //         })
        //         .SetUp(() => lookup.Clear())
        //         .Run();
        //
        //     data.Keys.Dispose();
        //     data.Values.Dispose();
        //     lookup.Dispose();
        // }

        private (NativeArray<int> Keys, NativeArray<int> Values) CreateKeyValue(int length, Allocator allocator = Allocator.Persistent)
        {
            var keys = new NativeArray<int>(length, allocator);
            var values = new NativeArray<int>(length, allocator);

            for (var i = 0; i < length; i++)
            {
                keys[i] = i;
                values[i] = i;
            }

            return (keys, values);
        }

        [BurstCompile]
        private struct SingleHashMapJob : IJob
        {
            public NativeParallelHashMap<int, int> Hashmap;

            public NativeArray<int> Keys;

            public NativeArray<int> Values;

            public void Execute()
            {
                for (var i = 0; i < this.Keys.Length; i++)
                {
                    this.Hashmap.Add(this.Keys[i], this.Values[i]);
                }
            }
        }

        [BurstCompile]
        private struct SingleLookupJob : IJob
        {
            public NativeLookup<int, int> Lookup;

            public NativeArray<int> Keys;

            public NativeArray<int> Values;

            public void Execute()
            {
                this.Lookup.AddBatch(this.Keys, this.Values);
            }
        }

        [BurstCompile]
        private struct MultiHashMapJob : IJobFor
        {
            public int PerIndex;

            public NativeParallelHashMap<int, int>.ParallelWriter Hashmap;

            [ReadOnly]
            public NativeArray<int> Keys;

            [ReadOnly]
            public NativeArray<int> Values;

            public void Execute(int index)
            {
                var startIndex = index * this.PerIndex;
                var endIndex = startIndex + this.PerIndex;

                for (var i = startIndex; i < endIndex; i++)
                {
                    this.Hashmap.TryAdd(this.Keys[i], this.Values[i]);
                }
            }
        }

        [BurstCompile]
        private struct MultiLookupJob : IJobFor
        {
            public int PerIndex;

            public NativeLookup<int, int>.ParallelWriter Lookup;

            public NativeArray<int> Keys;

            public NativeArray<int> Values;

            public void Execute(int index)
            {
                var startIndex = index * this.PerIndex;

                this.Lookup.AddBatch(this.Keys.GetSubArray(startIndex, this.PerIndex), this.Values.GetSubArray(startIndex, this.PerIndex));
            }
        }

        [BurstCompile]
        private struct MultiLookupPart1Job : IJobFor
        {
            public int PerIndex;

            public NativeLookup<int, int>.ParallelWriter Lookup;

            public NativeArray<int> Keys;

            public NativeArray<int> Values;

            public void Execute(int index)
            {
                var startIndex = index * this.PerIndex;

                this.Lookup.AddKeyValues(this.Keys.GetSubArray(startIndex, this.PerIndex), this.Values.GetSubArray(startIndex, this.PerIndex));
            }
        }

        [BurstCompile]
        private struct MultiLookupPart2Job : IJob
        {
            public NativeLookup<int, int> Lookup;

            public void Execute()
            {
                this.Lookup.CalculateHashes();
            }
        }
    }
}
