// <copyright file="TimerEnableableTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Models
{
    using BovineLabs.Core.Assertions;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using Unity.PerformanceTesting;

    [BurstCompile]
    public unsafe class TimerEnableableTests
    {
        [TestCase(0)]
        [TestCase(7)]
        [TestCase(16)]
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(45)]
        [TestCase(64)]
        [TestCase(111)]
        [TestCase(128)]
        [TestCase(254)]
        public void CalculateTest(int count)
        {
            Allocate(count, Allocator.Temp, out var remainings, out var isOn);

            for (var i = 0; i < count; i++)
            {
                if (i % 3 == 0)
                {
                    remainings[i] = 1f;
                }
                else if (i % 2 == 0)
                {
                    remainings[i] = 0.5f;
                }
                else
                {
                    remainings[i] = 0;
                }
            }

            TimerEnableable.CalculateOn(remainings, isOn, count, 0.6f);

            for (var i = 0; i < count; i++)
            {
                var on = isOn + (i / 64);

                var expected = i % 3 == 0;
                var actual = ((1ul << i) & *on) != 0;
                Assert.IsTrue(expected == actual, $"{i}");
            }
        }

        [TestCase(128 * 256 * 8)]
        [Performance]
        public void CalculatePerformance(int count)
        {
            Allocate(count, Allocator.Persistent, out var remainings, out var isOn);

            Measure
                .Method(() =>
                {
                    CalculateNew(remainings, isOn, count, 0.6f);
                })
                .Run();

            UnsafeUtility.Free(remainings, Allocator.Persistent);
            UnsafeUtility.Free(isOn, Allocator.Persistent);
        }

        [TestCase(128 * 256 * 8)]
        [Performance]
        public void CalculatePerformanceOld(int count)
        {
            Allocate(count, Allocator.Persistent, out var remainings, out var isOn);

            Measure
                .Method(() =>
                {
                    CalculateOld(remainings, isOn, count, 0.6f);
                })
                .Run();

            UnsafeUtility.Free(remainings, Allocator.Persistent);
            UnsafeUtility.Free(isOn, Allocator.Persistent);
        }

        private static void Allocate(int count, Allocator allocator, out float* remainings, out ulong* isOn)
        {
            remainings = (float*)UnsafeUtility.Malloc(count * sizeof(float), UnsafeUtility.AlignOf<float>(), allocator);
            var isOnCount = (int)math.ceil(count / 64f);
            isOn = (ulong*)UnsafeUtility.Malloc(isOnCount * sizeof(ulong), UnsafeUtility.AlignOf<ulong>(), allocator);

            for (var i = 0; i < count; i++)
            {
                if (i % 3 == 0)
                {
                    remainings[i] = 1f;
                }
                else if (i % 2 == 0)
                {
                    remainings[i] = 0.5f;
                }
                else
                {
                    remainings[i] = 0;
                }
            }
        }

        // [BurstCompile]
        // private static void CalculateNew([NoAlias] float* remainings, [NoAlias] ulong* isOn, int length, float deltaTime)
        // {
        //     const int perIt = 128;
        //
        //     var count = length / perIt;
        //
        //     for (var i = 0; i < count; i++)
        //     {
        //         TimerEnableable.CalculateOn(remainings + (i * perIt), isOn + (i * 2), length / 128, deltaTime);
        //     }
        //
        //     var r = length % perIt;
        //     TimerEnableable.CalculateOn(remainings + (count * perIt), isOn + (count * 2), r, deltaTime);
        // }

        [BurstCompile]
        private static void CalculateOld([NoAlias] float* remainings, [NoAlias] ulong* isOn, int length, float deltaTime)
        {
            const int perIt = 128;

            var count = length / perIt;

            for (var i = 0; i < count; i++)
            {
                CalculateOld128(remainings + (i * perIt), isOn + (i * 2), length / 128, deltaTime);
            }

            var r = length % perIt;
            CalculateOld128(remainings + (count * perIt), isOn + (count * 2), r, deltaTime);
        }

        private static void CalculateOld128([NoAlias] float* remainings, [NoAlias] ulong* isOn, int length, float deltaTime)
        {
            var length0 = math.min(64, length);

            for (var i = 0; i < length0; i++)
            {
                remainings[i] = math.max(0, remainings[i] - deltaTime);
                UnsafeBitArray.Set(isOn, i, remainings[i] != 0);
            }

            isOn += 1;
            var length1 = length - 64;
            remainings += 64;

            for (var i = 0; i < length1; i++)
            {
                remainings[i] = math.max(0, remainings[i] - deltaTime);
                UnsafeBitArray.Set(isOn, i, remainings[i] != 0);
            }
        }

        [BurstCompile]
        private static void CalculateNew([NoAlias] float* remainings, [NoAlias] ulong* isOn, int length, float deltaTime)
        {
            const int perIt = 128;

            var count = length / perIt;

            for (var i = 0; i < count; i++)
            {
                CalculateNew128(remainings + (i * perIt), isOn + (i * 2), length / 128, deltaTime);
            }

            var r = length % perIt;
            CalculateNew128(remainings + (count * perIt), isOn + (count * 2), r, deltaTime);
        }

        private static void CalculateNew128([NoAlias] float* remainings, [NoAlias] ulong* isOn, int length, float deltaTime)
        {
            var u0 = isOn;
            var length0 = math.min(64, length);

            for (var i = 0; i < length0; i++)
            {
                remainings[i] = math.max(0, remainings[i] - deltaTime);
                UnsafeBitArray.Set(u0, i, remainings[i] != 0);
            }

            var u1 = isOn + 1;
            var length1 = math.min(64, length - 64);

            for (var i = 0; i < length1; i++)
            {
                remainings[i + 64] = math.max(0, remainings[i + 64] - deltaTime);
                UnsafeBitArray.Set(u1, i, remainings[i + 64] != 0);
            }
        }

        private static class TimerEnableable
        {
            public static void CalculateOn([NoAlias] float* remainings, [NoAlias] ulong* isOn, [AssumeRange(0, int.MaxValue)] int length, float deltaTime)
            {
                const int intBits = 32;

                var l32 = length / intBits;
                var r32 = length % intBits;
                var isOn32 = (uint*)isOn;

                for (var i = 0; i < l32; i++)
                {
                    CalculateFull(remainings + (i * intBits), isOn32 + i, intBits, deltaTime);
                }

                Calculate(remainings + (l32 * intBits), isOn32 + l32, r32, deltaTime);
            }

            private static void CalculateFull([NoAlias] float* remainings, [NoAlias] uint* isOn, [AssumeRange(0, 32)] int length, float deltaTime)
            {
                Check.Assume(length % 4 == 0);
                var length4 = length / 4;

                var remaining4 = (float4*)remainings;

                for (var i = 0; i < length4; i++)
                {
                    remaining4[i] = math.max(0, remaining4[i] - deltaTime);

                    var value = (uint)math.bitmask(remaining4[i] != 0);
                    var mask = 15u << (i * 4);
                    *isOn = (*isOn & ~mask) | (value << (i * 4));
                }
            }

            private static void Calculate([NoAlias] float* remainings, [NoAlias] uint* isOn, [AssumeRange(0, 32)] int length, float deltaTime)
            {
                var length4 = length / 4;
                var remainder = length % 4;

                var remaining4 = (float4*)remainings;

                for (var i = 0; i < length4; i++)
                {
                    remaining4[i] = math.max(0, remaining4[i] - deltaTime);

                    var value = (uint)math.bitmask(remaining4[i] != 0);
                    var mask = 15u << (i * 4);
                    *isOn = (*isOn & ~mask) | (value << (i * 4));
                }

                for (var i = 0; i < remainder; i++)
                {
                    var j = (length4 * 4) + i;
                    remainings[j] = math.max(0, remainings[j] - deltaTime);
                    var v = remainings[j] != 0 ? 1 : 0;
                    var mask = 1u << j;
                    *isOn = (*isOn & ~mask) | ((uint)-v & mask);
                }
            }
        }
    }
}
