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

        private readonly float _outerRadius;
        private readonly float _worldHalfSize;
        private readonly int2 _boundsMin;
        private readonly int2 _boundsSize;

        private NativeParallelMultiHashMap<int, int> _map;

        public SpatialHexMap(float quantizeStep, int size, Allocator allocator = Allocator.Persistent)
        {
            _outerRadius = quantizeStep / SpatialHexMap.Sqrt3;
            _worldHalfSize = size / 2f;

            CalculateBounds(_outerRadius, _worldHalfSize, out _boundsMin, out _boundsSize);

            _map = new NativeParallelMultiHashMap<int, int>(0, allocator);
        }

        public bool IsCreated => _map.IsCreated;

        public void Dispose()
        {
            _map.Dispose();
        }

        public void Dispose(JobHandle dependency)
        {
            _map.Dispose(dependency);
        }

        public JobHandle Build(
            NativeList<T> positions, JobHandle dependency, ResizeNativeParallelHashMapJob resizeStub = default, QuantizeJob quantizeStub = default)
        {
            return Build(positions.AsDeferredJobArray(), dependency, resizeStub, quantizeStub);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Sneaky way to allow this to run in bursted ISystem")]
        public JobHandle Build(
            NativeArray<T> positions, JobHandle dependency, ResizeNativeParallelHashMapJob resizeStub = default, QuantizeJob quantizeStub = default)
        {
            dependency = new ResizeNativeParallelHashMapJob
            {
                Length = positions,
                Map = _map,
            }.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            dependency = new QuantizeJob
            {
                Positions = positions,
                Map = _map,
                OuterRadius = _outerRadius,
                BoundsMin = _boundsMin,
                BoundsSize = _boundsSize,
                WorldHalfSize = _worldHalfSize,
                Workers = workers,
            }.ScheduleParallel(workers, 1, dependency);

            dependency = new SpatialHexMap.CalculateMap
            {
                SpatialHashMap = _map,
            }.Schedule(dependency);

            return dependency;
        }

        public SpatialHexMap.ReadOnly AsReadOnly()
        {
            return new SpatialHexMap.ReadOnly(_outerRadius, _boundsMin, _boundsSize, _map);
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
                if (Map.Capacity < Length.Length)
                {
                    Map.Capacity = Length.Length;
                }

                Map.Clear();
                Map.SetAllocatedIndexLength(Length.Length);
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
                var length = Positions.Length / Workers;
                var start = index * length;
                var end = start + length;
                if (index == Workers - 1)
                {
                    end += Positions.Length % Workers;
                }

                var bucketData = Map.GetUnsafeBucketData();
                var keys = (int*)bucketData.keys;
                var values = (int*)bucketData.values;

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = Positions[entityInQueryIndex].Position;
                    var axial = SpatialHexMap.Quantized(position, OuterRadius);

                    ValidatePosition(position, axial);

                    var hashed = SpatialHexMap.Hash(axial, BoundsMin, BoundsSize.x);
                    keys[entityInQueryIndex] = hashed;
                    values[entityInQueryIndex] = entityInQueryIndex;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidatePosition(float2 position, int2 axial)
            {
                if (SpatialHexMap.IsWithinBounds(axial, BoundsMin, BoundsSize))
                {
                    return;
                }

                var worldMin = new float2(-WorldHalfSize);
                var worldMax = new float2(WorldHalfSize);
                var axialMax = BoundsMin + BoundsSize - 1;

                BLGlobalLogger.LogError512(
                    $"Position {position} quantized to {axial} is outside the hex map bounds, worldMin={worldMin} worldMax={worldMax} axialMin={BoundsMin} axialMax={axialMax}");
                throw new ArgumentException(
                    $"Position {position} quantized to {axial} is outside the hex map bounds, worldMin={worldMin} worldMax={worldMax} axialMin={BoundsMin} axialMax={axialMax}");
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
        public static int Distance(int2 a, int2 b)
        {
            var delta = a - b;
            return math.max(math.max(math.abs(delta.x), math.abs(delta.y)), math.abs(delta.x + delta.y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float OuterRadiusFromCellWidth(float cellWidth)
        {
            return cellWidth / Sqrt3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CellWidthFromOuterRadius(float outerRadius)
        {
            return outerRadius * Sqrt3;
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
            private readonly float _outerRadius;
            private readonly int2 _boundsMin;
            private readonly int2 _boundsSize;

            public ReadOnly(float outerRadius, int2 boundsMin, int2 boundsSize, NativeParallelMultiHashMap<int, int> map)
            {
                _outerRadius = outerRadius;
                _boundsMin = boundsMin;
                _boundsSize = boundsSize;
                Map = map;
            }

            [field: ReadOnly]
            public NativeParallelMultiHashMap<int, int> Map { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int2 Quantized(float2 position)
            {
                return SpatialHexMap.Quantized(position, _outerRadius);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float2 Center(int2 axial)
            {
                return SpatialHexMap.Center(axial, _outerRadius);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Hash(int2 axial)
            {
                return SpatialHexMap.Hash(axial, _boundsMin, _boundsSize.x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsWithinBounds(int2 axial)
            {
                return SpatialHexMap.IsWithinBounds(axial, _boundsMin, _boundsSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int SearchRange(float radius)
            {
                return SpatialHexMap.SearchRange(radius, _outerRadius);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float CellMinDistanceSq(float2 position, int2 axial)
            {
                return SpatialHexMap.CellMinDistanceSq(position, axial, _outerRadius);
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
                SpatialHashMap.RecalculateBuckets();
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
