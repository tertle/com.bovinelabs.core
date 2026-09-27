#if UNITY_PERFORMANCE_TESTING
namespace BovineLabs.Core.Tests.Utility
{
    using System.IO;
    using BovineLabs.Core.Utility;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.PerformanceTesting;
    using UnityEngine;

    public class PooledNativeArrayPerformanceTests
    {
        private bool _previousBurst;
        private bool _previousSafetyChecks;
        private bool _previousForceSafetyChecks;
        private readonly PooledNativeList<float4x4>[] _held = new PooledNativeList<float4x4>[PooledNativeList.MaxPoolSizePerThread];
        private int _heldCount;

        [OneTimeSetUp]
        public void SetUp()
        {
            _previousBurst = BurstCompiler.Options.EnableBurstCompilation;
            _previousSafetyChecks = BurstCompiler.Options.EnableBurstSafetyChecks;
            _previousForceSafetyChecks = BurstCompiler.Options.ForceEnableBurstSafetyChecks;
            BurstCompiler.Options.EnableBurstCompilation = true;
            BurstCompiler.Options.EnableBurstSafetyChecks = false;
            BurstCompiler.Options.ForceEnableBurstSafetyChecks = false;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            ReturnHeldLists();
            BurstCompiler.Options.ForceEnableBurstSafetyChecks = _previousForceSafetyChecks;
            BurstCompiler.Options.EnableBurstSafetyChecks = _previousSafetyChecks;
            BurstCompiler.Options.EnableBurstCompilation = _previousBurst;
        }

        [TestCase(32, false, 1)]
        [TestCase(256, false, 1)]
        [TestCase(4096, false, 1)]
        [TestCase(65536, false, 1)]
        [TestCase(32, true, 1)]
        [TestCase(256, true, 1)]
        [TestCase(4096, true, 1)]
        [TestCase(65536, true, 1)]
        [TestCase(4, false, 1000)]
        [TestCase(32, false, 1000)]
        [TestCase(256, false, 1000)]
        [TestCase(4, true, 1000)]
        [TestCase(32, true, 1000)]
        [TestCase(256, true, 1000)]
        [Performance]
        public unsafe void UninitializedScratch(int count, bool copy, int entityCount)
        {
            // Matches the 64-byte stride of Quill's LineVertex and SolidVertex.
            using var source = new NativeArray<float4x4>(count * entityCount, Allocator.Persistent);
            using var output = new NativeArray<float4x4>(count * entityCount, Allocator.Persistent);
            using var results = new NativeArray<Result>(1, Allocator.Persistent);
            var sourceData = source;
            for (var i = 0; i < source.Length; i++)
            {
                sourceData[i] = new float4x4(i + 1);
            }

            var job = new ScratchJob
            {
                Count = count,
                EntityCount = entityCount,
                Copy = copy,
                Source = source,
                Output = output,
                Results = results,
            };

            foreach (var backend in new[] { Backend.EmptyJob, Backend.TempArray, Backend.TempList, Backend.PooledList, Backend.PooledArray })
            {
                job.Backend = backend;
                ulong expectedStorage = 0;
                if (backend == Backend.PooledList || backend == Backend.PooledArray)
                {
                    // Seed through the shared list pool, before either array or list measurements.
                    using (var seed = PooledNativeList<float4x4>.Make())
                    {
                        var list = seed.List;
                        list.ResizeUninitialized(count);
                        expectedStorage = (ulong)list.GetUnsafePtr();
                    }
                }

                // Compile and validate outside measurement. Run must reset Temp, not accumulate it across samples.
                job.Run();
                Assert.AreEqual(1, results[0].BurstExecuted);
                if (backend == Backend.TempArray || backend == Backend.TempList)
                {
                    expectedStorage = results[0].FirstStorage;
                }

                job.Run();
                Validate();
                Measure.Method(() => job.Run()).SampleGroup(new SampleGroup($"{backend}.Batch{entityCount}", SampleUnit.Microsecond))
                    .CleanUp(Validate).WarmupCount(5).MeasurementCount(20).IterationsPerMeasurement(1).Run();

                if (copy && backend != Backend.EmptyJob)
                {
                    for (var i = 0; i < source.Length; i++)
                    {
                        Assert.AreEqual(source[i], output[i]);
                    }
                }

                void Validate()
                {
                    Assert.AreEqual(1, results[0].BurstExecuted);
                    if (backend == Backend.EmptyJob)
                    {
                        return;
                    }

                    Assert.AreEqual(count, results[0].Length);
                    Assert.AreEqual(expectedStorage, results[0].FirstStorage, "Warm storage must be reused; Temp storage must reset between job invocations.");
                    Assert.AreEqual(entityCount, results[0].Uses);
                    Assert.AreEqual(backend == Backend.TempArray || backend == Backend.TempList ? entityCount : 1, results[0].BufferChanges);
                }
            }

            // Holding every cached lease forces a pool miss without changing the global pool or timing its setup.
            job.Backend = Backend.PooledArray;
            Measure.Method(() => job.Run()).SampleGroup(new SampleGroup($"PooledArrayCold.Batch{entityCount}", SampleUnit.Microsecond))
                .SetUp(HoldCachedLists).CleanUp(() =>
                {
                    ReturnHeldLists();
                    Assert.AreEqual(1, results[0].BurstExecuted);
                    Assert.AreEqual(count, results[0].Length);
                    Assert.AreEqual(entityCount, results[0].Uses);
                    Assert.AreEqual(1, results[0].BufferChanges, "A cold batch allocates on its first entity, then reuses that buffer.");
                }).WarmupCount(5).MeasurementCount(20).IterationsPerMeasurement(1).Run();

            if (copy)
            {
                for (var i = 0; i < source.Length; i++)
                {
                    Assert.AreEqual(source[i], output[i]);
                }
            }

            PerformanceTest.Active.CalculateStatisticalValues();
            File.WriteAllText($"Temp/PooledNativeArrayBenchmark-{count}-{copy}-{entityCount}.json", JsonUtility.ToJson(PerformanceTest.Active));
        }

        private void HoldCachedLists()
        {
            for (; _heldCount < _held.Length; _heldCount++)
            {
                _held[_heldCount] = PooledNativeList<float4x4>.Make();
            }

            Assert.Zero(PooledNativeList.Pool.Data.GetThreadList().Length);
        }

        [TearDown]
        public void ReturnHeldLists()
        {
            while (_heldCount > 0)
            {
                _held[--_heldCount].Dispose();
            }
        }

        private enum Backend
        {
            EmptyJob,
            TempArray,
            TempList,
            PooledList,
            PooledArray,
        }

        private struct Result
        {
            public int BurstExecuted;
            public int Length;
            public int Uses;
            public int BufferChanges;
            public ulong FirstStorage;
            public ulong Storage;
        }

        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        private unsafe struct ScratchJob : IJob
        {
            public Backend Backend;
            public int Count;
            public int EntityCount;
            public bool Copy;
            [ReadOnly]
            public NativeArray<float4x4> Source;
            public NativeArray<float4x4> Output;
            public NativeArray<Result> Results;

            public void Execute()
            {
                var result = new Result { BurstExecuted = 1 };
                MarkManaged(ref result.BurstExecuted);
                for (var entity = 0; entity < EntityCount; entity++)
                {
                    switch (Backend)
                    {
                        case Backend.EmptyJob:
                            break;
                        case Backend.TempArray:
                            var array = new NativeArray<float4x4>(Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            Use(array, entity, ref result);
                            break;
                        case Backend.TempList:
                            var list = new NativeList<float4x4>(Count, Allocator.Temp);
                            list.ResizeUninitialized(Count);
                            Use(list.AsArray(), entity, ref result);
                            break;
                        case Backend.PooledList:
                            using (var pool = PooledNativeList<float4x4>.Make())
                            {
                                var pooledList = pool.List;
                                pooledList.ResizeUninitialized(Count);
                                Use(pooledList.AsArray(), entity, ref result);
                            }

                            break;
                        case Backend.PooledArray:
                            using (var pool = PooledNativeArray<float4x4>.Make(Count, NativeArrayOptions.UninitializedMemory))
                            {
                                Use(pool.Array, entity, ref result);
                            }

                            break;
                    }
                }

                Results[0] = result;
            }

            private void Use(NativeArray<float4x4> scratch, int entity, ref Result result)
            {
                var dst = (byte*)scratch.GetUnsafePtr();
                result.Length = scratch.Length;
                if (result.Uses == 0)
                {
                    result.FirstStorage = (ulong)dst;
                }

                result.BufferChanges += result.Storage != (ulong)dst ? 1 : 0;
                result.Uses++;
                result.Storage = (ulong)dst; // Observe the allocation even when no copy is requested.
                if (!Copy)
                {
                    return;
                }

                var bytes = Count * UnsafeUtility.SizeOf<float4x4>();
                var src = (byte*)Source.GetUnsafeReadOnlyPtr() + (entity * bytes);
                var segmentBytes = bytes / 8;
                for (var offset = 0; offset < bytes; offset += segmentBytes)
                {
                    UnsafeUtility.MemCpy(dst + offset, src + offset, segmentBytes);
                }

                UnsafeUtility.MemCpy((byte*)Output.GetUnsafePtr() + (entity * bytes), dst, bytes);
            }

            [BurstDiscard]
            private static void MarkManaged(ref int compiled)
            {
                compiled = 0;
            }
        }
    }
}
#endif
