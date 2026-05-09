// <copyright file="HexSpatialMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Spatial
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    public struct SpatialHexMap<T> : IDisposable
        where T : unmanaged, ISpatialPosition
    {
        private const int BoundsPadding = 2;

        private readonly float outerRadius;
        private readonly float worldHalfSize;
        private readonly int2 boundsMin;
        private readonly int2 boundsSize;

        private NativeParallelMultiHashMap<int, int> map;

        public SpatialHexMap(float quantizeStep, int size, Allocator allocator = Allocator.Persistent)
        {
            this.outerRadius = quantizeStep / SpatialHexMap.Sqrt3;
            this.worldHalfSize = size / 2f;

            CalculateBounds(this.outerRadius, this.worldHalfSize, out this.boundsMin, out this.boundsSize);

            this.map = new NativeParallelMultiHashMap<int, int>(0, allocator);
        }

        public bool IsCreated => this.map.IsCreated;

        public void Dispose()
        {
            this.map.Dispose();
        }

        public void Dispose(JobHandle dependency)
        {
            this.map.Dispose(dependency);
        }

        public JobHandle Build(
            NativeList<T> positions, JobHandle dependency, ResizeNativeParallelHashMapJob resizeStub = default, QuantizeJob quantizeStub = default)
        {
            return this.Build(positions.AsDeferredJobArray(), dependency, resizeStub, quantizeStub);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Sneaky way to allow this to run in bursted ISystem")]
        public JobHandle Build(
            NativeArray<T> positions, JobHandle dependency, ResizeNativeParallelHashMapJob resizeStub = default, QuantizeJob quantizeStub = default)
        {
            dependency = new ResizeNativeParallelHashMapJob
            {
                Length = positions,
                Map = this.map,
            }.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            dependency = new QuantizeJob
            {
                Positions = positions,
                Map = this.map,
                OuterRadius = this.outerRadius,
                BoundsMin = this.boundsMin,
                BoundsSize = this.boundsSize,
                WorldHalfSize = this.worldHalfSize,
                Workers = workers,
            }.ScheduleParallel(workers, 1, dependency);

            dependency = new SpatialHexMap.CalculateMap
            {
                SpatialHashMap = this.map,
            }.Schedule(dependency);

            return dependency;
        }

        public SpatialHexMap.ReadOnly AsReadOnly()
        {
            return new SpatialHexMap.ReadOnly(this.outerRadius, this.boundsMin, this.boundsSize, this.map);
        }

        private static void CalculateBounds(float outerRadius, float worldHalfSize, out int2 boundsMin, out int2 boundsSize)
        {
            var min = SpatialHexMap.Quantized(new float2(-worldHalfSize, -worldHalfSize), outerRadius);
            var max = min;

            ExpandBounds(SpatialHexMap.Quantized(new float2(-worldHalfSize, worldHalfSize), outerRadius), ref min, ref max);
            ExpandBounds(SpatialHexMap.Quantized(new float2(worldHalfSize, -worldHalfSize), outerRadius), ref min, ref max);
            ExpandBounds(SpatialHexMap.Quantized(new float2(worldHalfSize, worldHalfSize), outerRadius), ref min, ref max);

            boundsMin = min - new int2(BoundsPadding);
            var boundsMax = max + new int2(BoundsPadding);
            boundsSize = (boundsMax - boundsMin) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ExpandBounds(int2 candidate, ref int2 min, ref int2 max)
        {
            min = math.min(min, candidate);
            max = math.max(max, candidate);
        }

        [BurstCompile]
        public struct ResizeNativeParallelHashMapJob : IJob
        {
            public NativeParallelMultiHashMap<int, int> Map;

            [ReadOnly]
            public NativeArray<T> Length;

            public void Execute()
            {
                if (this.Map.Capacity < this.Length.Length)
                {
                    this.Map.Capacity = this.Length.Length;
                }

                this.Map.Clear();
                this.Map.SetAllocatedIndexLength(this.Length.Length);
            }
        }

        [BurstCompile]
        [NoAlias]
        public unsafe struct QuantizeJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<T> Positions;

            [NativeDisableParallelForRestriction]
            public NativeParallelMultiHashMap<int, int> Map;

            public float OuterRadius;
            public int2 BoundsMin;
            public int2 BoundsSize;
            public float WorldHalfSize;
            public int Workers;

            public void Execute(int index)
            {
                var length = this.Positions.Length / this.Workers;
                var start = index * length;
                var end = start + length;
                if (index == this.Workers - 1)
                {
                    end += this.Positions.Length % this.Workers;
                }

                var bucketData = this.Map.GetUnsafeBucketData();
                var keys = (int*)bucketData.keys;
                var values = (int*)bucketData.values;

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = this.Positions[entityInQueryIndex].Position;
                    var axial = SpatialHexMap.Quantized(position, this.OuterRadius);

                    this.ValidatePosition(position, axial);

                    var hashed = SpatialHexMap.Hash(axial, this.BoundsMin, this.BoundsSize.x);
                    keys[entityInQueryIndex] = hashed;
                    values[entityInQueryIndex] = entityInQueryIndex;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidatePosition(float2 position, int2 axial)
            {
                if (SpatialHexMap.IsWithinBounds(axial, this.BoundsMin, this.BoundsSize))
                {
                    return;
                }

                var worldMin = new float2(-this.WorldHalfSize);
                var worldMax = new float2(this.WorldHalfSize);
                var axialMax = this.BoundsMin + this.BoundsSize - 1;

                BLGlobalLogger.LogError512(
                    $"Position {position} quantized to {axial} is outside the hex map bounds, worldMin={worldMin} worldMax={worldMax} axialMin={this.BoundsMin} axialMax={axialMax}");
                throw new ArgumentException(
                    $"Position {position} quantized to {axial} is outside the hex map bounds, worldMin={worldMin} worldMax={worldMax} axialMin={this.BoundsMin} axialMax={axialMax}");
            }
        }
    }

    public static class SpatialHexMap
    {
        internal const float Sqrt3 = 1.7320508f;
        private const float Sqrt3Over3 = Sqrt3 / 3f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 Quantized(float2 position, float outerRadius)
        {
            var q = ((Sqrt3Over3 * position.x) - (position.y / 3f)) / outerRadius;
            var r = ((2f / 3f) * position.y) / outerRadius;
            return Round(q, r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Center(int2 axial, float outerRadius)
        {
            return new float2(
                outerRadius * Sqrt3 * (axial.x + (axial.y * 0.5f)),
                outerRadius * 1.5f * axial.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Hash(int2 axial, int2 boundsMin, int boundsWidth)
        {
            var local = axial - boundsMin;
            return local.x + (local.y * boundsWidth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithinBounds(int2 axial, int2 boundsMin, int2 boundsSize)
        {
            var local = axial - boundsMin;
            return math.all(local >= int2.zero) && math.all(local < boundsSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SearchRange(float radius, float outerRadius)
        {
            var centerSpacing = Sqrt3 * outerRadius;
            return (int)math.ceil((radius + (2f * outerRadius)) / centerSpacing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CellMinDistanceSq(float2 position, int2 axial, float outerRadius)
        {
            var center = Center(axial, outerRadius);
            var minDistance = math.max(0f, math.distance(position, center) - outerRadius);
            return minDistance * minDistance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 Direction(int index)
        {
            switch (index)
            {
                case 0:
                    return new int2(1, 0);
                case 1:
                    return new int2(1, -1);
                case 2:
                    return new int2(0, -1);
                case 3:
                    return new int2(-1, 0);
                case 4:
                    return new int2(-1, 1);
                default:
                    return new int2(0, 1);
            }
        }

        public readonly struct ReadOnly
        {
            private readonly float outerRadius;
            private readonly int2 boundsMin;
            private readonly int2 boundsSize;

            public ReadOnly(float outerRadius, int2 boundsMin, int2 boundsSize, NativeParallelMultiHashMap<int, int> map)
            {
                this.outerRadius = outerRadius;
                this.boundsMin = boundsMin;
                this.boundsSize = boundsSize;
                this.Map = map;
            }

            [field: ReadOnly]
            public NativeParallelMultiHashMap<int, int> Map { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int2 Quantized(float2 position)
            {
                return SpatialHexMap.Quantized(position, this.outerRadius);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float2 Center(int2 axial)
            {
                return SpatialHexMap.Center(axial, this.outerRadius);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Hash(int2 axial)
            {
                return SpatialHexMap.Hash(axial, this.boundsMin, this.boundsSize.x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsWithinBounds(int2 axial)
            {
                return SpatialHexMap.IsWithinBounds(axial, this.boundsMin, this.boundsSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int SearchRange(float radius)
            {
                return SpatialHexMap.SearchRange(radius, this.outerRadius);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float CellMinDistanceSq(float2 position, int2 axial)
            {
                return SpatialHexMap.CellMinDistanceSq(position, axial, this.outerRadius);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int2 Direction(int index)
            {
                return SpatialHexMap.Direction(index);
            }
        }

        [BurstCompile]
        internal struct CalculateMap : IJob
        {
            public NativeParallelMultiHashMap<int, int> SpatialHashMap;

            public void Execute()
            {
                this.SpatialHashMap.RecalculateBuckets();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int2 Round(float q, float r)
        {
            var x = q;
            var z = r;
            var y = -x - z;

            var rx = (int)math.round(x);
            var ry = (int)math.round(y);
            var rz = (int)math.round(z);

            var xDiff = math.abs(rx - x);
            var yDiff = math.abs(ry - y);
            var zDiff = math.abs(rz - z);

            if (xDiff > yDiff && xDiff > zDiff)
            {
                rx = -ry - rz;
            }
            else if (yDiff > zDiff)
            {
                ry = -rx - rz;
            }
            else
            {
                rz = -rx - ry;
            }

            return new int2(rx, rz);
        }
    }
}
