// <copyright file="IJobNativeHashMapVisitKeyValueTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Jobs
{
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.Jobs;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary> Tests for <see cref="IJobNativeParallelHashMapVisitKeyValue{TKey,TValue}"/>. </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Matching interface its testing.")]
    public class IJobNativeHashMapVisitKeyValueTests
    {
        /// <summary> Tests that the job visits every key. </summary>
        [Test]
        public void VisitsEveryKey()
        {
            var input = new NativeParallelHashMap<int, int>(128, Allocator.TempJob);
            var output = new NativeParallelHashMap<int, int>(128, Allocator.TempJob);

            var random = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

            for (var i = 0; i < 128; i++)
            {
                input.Add(i, random.NextInt());
            }

            var job = new TestJob
                {
                    Output = output.AsParallelWriter(),
                }
                .ScheduleParallel(input, 4);

            job.Complete();

            var e = output.GetEnumerator();
            while (e.MoveNext())
            {
                var v = e.Current;
                Assert.IsTrue(input.TryGetValue(v.Key, out var val));
                Assert.AreEqual(val, v.Value);
            }

            e.Dispose();
            input.Dispose();
            output.Dispose();
        }

        [BurstCompile]
        private struct TestJob : IJobNativeParallelHashMapVisitKeyValue<int, int>
        {
            public NativeParallelHashMap<int, int>.ParallelWriter Output;

            public void ExecuteNext(int key, int progress)
            {
                this.Output.TryAdd(key, progress);
            }
        }
    }
}