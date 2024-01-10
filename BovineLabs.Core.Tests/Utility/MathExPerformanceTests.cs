// <copyright file="MathExPerformanceTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PERFORMANCE
namespace BovineLabs.Core.PerformanceTests.Utility
{
    using BovineLabs.Core.Utility;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.PerformanceTesting;

    public class MathExPerformanceTests
    {
        private const ushort Seed = 1234;
        private const int TestCase1 = 100001;
        private const int TestCase2 = 10000001;
        private const int AddValue = 3;

        [TestCase(TestCase1)]
        [TestCase(TestCase2)]
        [Performance]
        public void MaxTest(int length)
        {
            var input = new NativeArray<float>(length, Allocator.Persistent);
            var result = new NativeReference<float>(Allocator.Persistent);
            var run = 0;

            Measure
                .Method(() => new MathExMaxJob { Input = input, Result = result }.Run())
                .SetUp(() =>
                {
                    var random = Random.CreateFromIndex((uint)(Seed + run++));
                    for (var i = 0; i < input.Length; i++)
                    {
                        input[i] = random.NextFloat(-10000, 10000);
                    }
                })
                .Run();

            input.Dispose();
            result.Dispose();
        }

        [TestCase(TestCase1)]
        [TestCase(TestCase2)]
        [Performance]
        public void MaxTestComparison(int length)
        {
            var input = new NativeArray<float>(length, Allocator.Persistent);
            var result = new NativeReference<float>(Allocator.Persistent);
            var run = 0;

            Measure
                .Method(() => new MaxJob { Input = input, Result = result }.Run())
                .SetUp(() =>
                {
                    var random = Random.CreateFromIndex((uint)(Seed + run++));
                    for (var i = 0; i < input.Length; i++)
                    {
                        input[i] = random.NextFloat(-10000, 10000);
                    }
                })
                .Run();

            input.Dispose();
            result.Dispose();
        }

        [TestCase(TestCase1)]
        [TestCase(TestCase2)]
        [TestCase(TestCase1)]
        [TestCase(TestCase2)]
        [Performance]
        public void SumTest(int length)
        {
            var input = new NativeArray<float>(length, Allocator.Persistent);
            var result = new NativeReference<float>(Allocator.Persistent);
            var run = 0;

            Measure
                .Method(() => new MathExSumJob { Input = input, Result = result }.Run())
                .SetUp(() =>
                {
                    var random = Random.CreateFromIndex((uint)(Seed + run++));
                    for (var i = 0; i < input.Length; i++)
                    {
                        input[i] = random.NextFloat(-10000, 10000);
                    }
                })
                .Run();

            input.Dispose();
            result.Dispose();
        }

        [TestCase(TestCase1)]
        [TestCase(TestCase2)]
        [Performance]
        public void SumTestComparison(int length)
        {
            var input = new NativeArray<float>(length, Allocator.Persistent);
            var result = new NativeReference<float>(Allocator.Persistent);
            var run = 0;

            Measure
                .Method(() => new SumJob { Input = input, Result = result }.Run())
                .SetUp(() =>
                {
                    var random = Random.CreateFromIndex((uint)(Seed + run++));
                    for (var i = 0; i < input.Length; i++)
                    {
                        input[i] = random.NextFloat(-10000, 10000);
                    }
                })
                .Run();

            input.Dispose();
            result.Dispose();
        }

        [TestCase(TestCase1)]
        [TestCase(TestCase2)]
        [Performance]
        public void AddTest(int length)
        {
            var input = new NativeArray<int>(length, Allocator.Persistent);
            var output = new NativeArray<int>(length, Allocator.Persistent);
            var run = 0;

            Measure
                .Method(() => new MathExAddJob { Input = input, Result = output, Value = AddValue }.Run())
                .SetUp(() =>
                {
                    var random = Random.CreateFromIndex((uint)(Seed + run++));
                    for (var i = 0; i < input.Length; i++)
                    {
                        input[i] = random.NextInt(-10000, 10000);
                    }
                })
                .Run();

            input.Dispose();
            output.Dispose();
        }

        [TestCase(TestCase1)]
        [TestCase(TestCase2)]
        [Performance]
        public void AddTestComparison(int length)
        {
            var input = new NativeArray<int>(length, Allocator.Persistent);
            var output = new NativeArray<int>(length, Allocator.Persistent);
            var run = 0;

            Measure
                .Method(() => new AddJob { Input = input, Result = output, Value = AddValue }.Run())
                .SetUp(() =>
                {
                    var random = Random.CreateFromIndex((uint)(Seed + run++));
                    for (var i = 0; i < input.Length; i++)
                    {
                        input[i] = random.NextInt(-10000, 10000);
                    }
                })
                .Run();

            input.Dispose();
            output.Dispose();
        }

        [BurstCompile]
        private struct MathExMaxJob : IJob
        {
            [ReadOnly]
            public NativeArray<float> Input;

            [WriteOnly]
            public NativeReference<float> Result;

            public void Execute()
            {
                this.Result.Value = mathex.max(this.Input);
            }
        }

        [BurstCompile]
        private struct MaxJob : IJob
        {
            [ReadOnly]
            public NativeArray<float> Input;

            [WriteOnly]
            public NativeReference<float> Result;

            public void Execute()
            {
                var result = float.MinValue;

                for (var i = 0; i < this.Input.Length; i++)
                {
                    if (this.Input[i] > result)
                    {
                        result = this.Input[i];
                    }
                }

                this.Result.Value = result;
            }
        }

        [BurstCompile]
        private struct MathExSumJob : IJob
        {
            [ReadOnly]
            public NativeArray<float> Input;

            [WriteOnly]
            public NativeReference<float> Result;

            public void Execute()
            {
                this.Result.Value = mathex.sum(this.Input);
            }
        }

        [BurstCompile]
        private struct SumJob : IJob
        {
            [ReadOnly]
            public NativeArray<float> Input;

            [WriteOnly]
            public NativeReference<float> Result;

            public void Execute()
            {
                var result = 0f;

                for (var i = 0; i < this.Input.Length; i++)
                {
                    result += this.Input[i];
                }

                this.Result.Value = result;
            }
        }

        [BurstCompile]
        [NoAlias]
        private struct MathExAddJob : IJob
        {
            [ReadOnly]
            public NativeArray<int> Input;

            [WriteOnly]
            public NativeArray<int> Result;

            public int Value;

            public void Execute()
            {
                mathex.add(this.Result, this.Input, this.Value);
            }
        }

        [BurstCompile]
        [NoAlias]
        private struct AddJob : IJob
        {
            [ReadOnly]
            public NativeArray<int> Input;

            [WriteOnly]
            public NativeArray<int> Result;

            public int Value;

            public void Execute()
            {
                for (var i = 0; i < this.Input.Length; i++)
                {
                    this.Result[i] = this.Input[i] + this.Value;
                }
            }
        }
    }
}
#endif
