// <copyright file="BlobCurveCachePerformanceTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections.Blobs
{
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.PerformanceTesting;
    using UnityEngine;
    using Random = Unity.Mathematics.Random;

    public class BlobCurveCachePerformanceTests
    {
        private const int KeyframeCount = 512;
        private const int SampleCount = 1 << 15;
        private const float TinyDelta = 1e-8f;

        [Test]
        [Performance]
        public void Evaluate_WithSamplerAndWithout()
        {
            var curve = BuildTinySegmentCurve(KeyframeCount, TinyDelta);
            var blob = BlobCurve.Create(curve);

            try
            {
                var sampleTimes = BuildSampleTimes(blob, SampleCount);
                try
                {
                    var withSampler = RunSampler(blob, sampleTimes);
                    var withoutSampler = RunWithoutCache(blob, sampleTimes);

                    Assert.AreEqual(withSampler, withoutSampler, "Sampler and non-cached evaluation must produce identical sums.");

                    Measure
                        .Method(() =>
                        {
                            _ = RunSampler(blob, sampleTimes);
                        })
                        .SampleGroup(new SampleGroup("WithSampler", SampleUnit.Microsecond))
                        .WarmupCount(1)
                        .MeasurementCount(10)
                        .IterationsPerMeasurement(1)
                        .Run();

                    Measure
                        .Method(() =>
                        {
                            _ = RunWithoutCache(blob, sampleTimes);
                        })
                        .SampleGroup(new SampleGroup("WithoutSampler", SampleUnit.Microsecond))
                        .WarmupCount(1)
                        .MeasurementCount(10)
                        .IterationsPerMeasurement(1)
                        .Run();
                }
                finally
                {
                    sampleTimes.Dispose();
                }
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        [Performance]
        public void Evaluate_WithSampler_Incremental()
        {
            var curve = BuildTinySegmentCurve(KeyframeCount, TinyDelta);
            var blob = BlobCurve.Create(curve);

            try
            {
                var sampleTimes = BuildSequentialSampleTimes(blob, SampleCount);
                try
                {
                    var withSampler = RunSampler(blob, sampleTimes);
                    var withoutSampler = RunWithoutCache(blob, sampleTimes);

                    Assert.AreEqual(withSampler, withoutSampler);

                    Measure
                        .Method(() =>
                        {
                            _ = RunSampler(blob, sampleTimes);
                        })
                        .SampleGroup(new SampleGroup("WithSamplerSequential", SampleUnit.Microsecond))
                        .WarmupCount(1)
                        .MeasurementCount(10)
                        .IterationsPerMeasurement(1)
                        .Run();

                    Measure
                        .Method(() =>
                        {
                            _ = RunWithoutCache(blob, sampleTimes);
                        })
                        .SampleGroup(new SampleGroup("WithoutSamplerSequential", SampleUnit.Microsecond))
                        .WarmupCount(1)
                        .MeasurementCount(10)
                        .IterationsPerMeasurement(1)
                        .Run();
                }
                finally
                {
                    sampleTimes.Dispose();
                }
            }
            finally
            {
                blob.Dispose();
            }
        }

        private static NativeArray<float> BuildSampleTimes(BlobAssetReference<BlobCurve> blob, int sampleCount)
        {
            ref var curve = ref blob.Value;
            var times = new NativeArray<float>(sampleCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var random = Random.CreateFromIndex(0x1234u);
            var startTime = curve.StartTime;
            var endTime = curve.EndTime;

            if (startTime >= endTime)
            {
                for (int i = 0; i < sampleCount; ++i)
                {
                    times[i] = startTime;
                }

                return times;
            }

            for (int i = 0; i < sampleCount; ++i)
            {
                times[i] = random.NextFloat(startTime, endTime);
            }

            return times;
        }

        private static AnimationCurve BuildTinySegmentCurve(int keyCount, float delta)
        {
            var keys = new Keyframe[keyCount];
            var time = 0f;
            for (int i = 0; i < keyCount; ++i)
            {
                keys[i] = new Keyframe(time, math.sin(i));
                if (i < keyCount - 1)
                {
                    time += delta;
                }
            }

            return new AnimationCurve(keys);
        }

        private static NativeArray<float> BuildSequentialSampleTimes(BlobAssetReference<BlobCurve> blob, int sampleCount)
        {
            ref var curve = ref blob.Value;
            var times = new NativeArray<float>(sampleCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var startTime = curve.StartTime;
            var endTime = curve.EndTime;
            if (startTime >= endTime)
            {
                for (int i = 0; i < sampleCount; ++i)
                {
                    times[i] = startTime;
                }

                return times;
            }

            var countMinusOne = math.max(sampleCount - 1, 1);
            var step = (endTime - startTime) / countMinusOne;
            var time = startTime;
            for (int i = 0; i < sampleCount; ++i, time += step)
            {
                times[i] = math.min(time, endTime);
            }

            return times;
        }

        private static double RunSampler(BlobAssetReference<BlobCurve> blob, NativeArray<float> sampleTimes)
        {
            var sum = new NativeArray<double>(1, Allocator.TempJob);
            try
            {
                var job = new SampleWithCacheJob
                {
                    Times = sampleTimes,
                    Sampler = new BlobCurveSampler(blob),
                    Sum = sum,
                };

                job.Run();
                return sum[0];
            }
            finally
            {
                sum.Dispose();
            }
        }

        private static double RunWithoutCache(BlobAssetReference<BlobCurve> blob, NativeArray<float> sampleTimes)
        {
            var sum = new NativeArray<double>(1, Allocator.TempJob);
            try
            {
                var job = new SampleWithoutCacheJob
                {
                    Times = sampleTimes,
                    Curve = blob,
                    Sum = sum,
                };

                job.Run();
                return sum[0];
            }
            finally
            {
                sum.Dispose();
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct SampleWithCacheJob : IJob
        {
            [ReadOnly]
            public NativeArray<float> Times;

            public BlobCurveSampler Sampler;

            public NativeArray<double> Sum;

            public void Execute()
            {
                var sampler = this.Sampler;
                double sum = 0;

                for (int i = 0; i < this.Times.Length; ++i)
                {
                    sum += sampler.Evaluate(this.Times[i]);
                }

                this.Sum[0] = sum;
                this.Sampler = sampler;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct SampleWithoutCacheJob : IJob
        {
            [ReadOnly]
            public NativeArray<float> Times;

            [ReadOnly]
            public BlobAssetReference<BlobCurve> Curve;

            public NativeArray<double> Sum;

            public void Execute()
            {
                ref var curve = ref this.Curve.Value;
                double sum = 0;

                for (int i = 0; i < this.Times.Length; ++i)
                {
                    sum += curve.Evaluate(this.Times[i]);
                }

                this.Sum[0] = sum;
            }
        }
    }
}
