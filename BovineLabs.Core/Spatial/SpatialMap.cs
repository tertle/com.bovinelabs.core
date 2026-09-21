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

    public struct SpatialMap<T> : IDisposable
        where T : unmanaged, ISpatialPosition
    {
        private readonly float _quantizeStep;
        private readonly int _quantizeSize;
        private readonly int2 _halfSize;

        private NativeParallelMultiHashMap<int, int> _map;

        public SpatialMap(float quantizeStep, int size, Allocator allocator = Allocator.Persistent)
        {
            _quantizeStep = quantizeStep;
            _quantizeSize = (int)math.ceil(size / quantizeStep);
            _halfSize = new int2(size) / 2;

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
            NativeList<T> positions, JobHandle dependency, ResizeNativeParallelHashMapJob resizeStub = default, QuantizeJob quantizeJob = default)
        {
            return Build(positions.AsDeferredJobArray(), dependency, resizeStub, quantizeJob);
        }

        public JobHandle BuildWithPositionOutput(
            NativeList<T> positions, NativeArray<float2> positionOutput, JobHandle dependency, ResizeNativeParallelHashMapJob resizeStub = default,
            QuantizePositionJob quantizeJob = default)
        {
            return BuildWithPositionOutput(positions.AsDeferredJobArray(), positionOutput, dependency, resizeStub, quantizeJob);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Sneaky way to allow this to run in bursted ISystem")]
        public JobHandle Build(
            NativeArray<T> positions, JobHandle dependency, ResizeNativeParallelHashMapJob resizeJob = default, QuantizeJob quantizeJob = default)
        {
            resizeJob.Length = positions;
            resizeJob.Map = _map;
            dependency = resizeJob.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            quantizeJob.Positions = positions;
            quantizeJob.Map = _map;
            quantizeJob.QuantizeStep = _quantizeStep;
            quantizeJob.QuantizeWidth = _quantizeSize;
            quantizeJob.HalfSize = _halfSize;
            quantizeJob.Workers = workers;
            dependency = quantizeJob.ScheduleParallel(workers, 1, dependency);

            dependency = new SpatialMap.CalculateMap
            {
                SpatialHashMap = _map,
            }.Schedule(dependency);

            return dependency;
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Sneaky way to allow this to run in bursted ISystem")]
        public JobHandle BuildWithPositionOutput(
            NativeArray<T> positions, NativeArray<float2> positionOutput, JobHandle dependency, ResizeNativeParallelHashMapJob resizeJob = default,
            QuantizePositionJob quantizeJob = default)
        {
            resizeJob.Length = positions;
            resizeJob.Map = _map;
            dependency = resizeJob.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            quantizeJob.Positions = positions;
            quantizeJob.Map = _map;
            quantizeJob.PositionOutput = positionOutput;
            quantizeJob.QuantizeStep = _quantizeStep;
            quantizeJob.QuantizeWidth = _quantizeSize;
            quantizeJob.HalfSize = _halfSize;
            quantizeJob.Workers = workers;
            dependency = quantizeJob.ScheduleParallel(workers, 1, dependency);

            dependency = new SpatialMap.CalculateMap
            {
                SpatialHashMap = _map,
            }.Schedule(dependency);

            return dependency;
        }

        public SpatialMap.ReadOnly AsReadOnly()
        {
            return new SpatialMap.ReadOnly(_quantizeStep, _quantizeSize, _halfSize, _map);
        }

        // Jobs outside to avoid generic issues
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

            public float QuantizeStep;
            public int QuantizeWidth;
            public int2 HalfSize;

            public int Workers;

            public void Execute(int index)
            {
                var length = Positions.Length / Workers;
                var start = index * length;
                var end = start + length;
                if (index == Workers - 1)
                {
                    // Last thread handles remainder
                    end += Positions.Length % Workers;
                }

                var keys = (int*)Map.GetUnsafeBucketData().keys;
                var values = (int*)Map.GetUnsafeBucketData().values;

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = Positions[entityInQueryIndex].Position;
                    var quantized = SpatialMap.Quantized(position, QuantizeStep, HalfSize);

                    ValidatePosition(position, quantized);

                    var hashed = SpatialMap.Hash(quantized, QuantizeWidth);
                    keys[entityInQueryIndex] = hashed;
                    values[entityInQueryIndex] = entityInQueryIndex;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidatePosition(float2 position, int2 quantized)
            {
                if (SpatialMap.IsWithinBounds(quantized, QuantizeWidth))
                {
                    return;
                }

                var min = new int2(-HalfSize);
                var max = new int2(HalfSize - 1);

                BLGlobalLogger.LogError512($"Position {position} is outside the size of the world, min={min} max={max}");
                throw new ArgumentException($"Position {position} is outside the size of the world, min={min} max={max}");
            }
        }

        [BurstCompile]
        [NoAlias]
        public unsafe struct QuantizePositionJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<T> Positions;

            [NativeDisableParallelForRestriction]
            public NativeParallelMultiHashMap<int, int> Map;

            [NativeDisableParallelForRestriction]
            [WriteOnly]
            public NativeArray<float2> PositionOutput;

            public float QuantizeStep;
            public int QuantizeWidth;
            public int2 HalfSize;

            public int Workers;

            public void Execute(int index)
            {
                var length = Positions.Length / Workers;
                var start = index * length;
                var end = start + length;
                if (index == Workers - 1)
                {
                    // Last thread handles remainder
                    end += Positions.Length % Workers;
                }

                var keys = (int*)Map.GetUnsafeBucketData().keys;
                var values = (int*)Map.GetUnsafeBucketData().values;

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = Positions[entityInQueryIndex].Position;
                    var quantized = SpatialMap.Quantized(position, QuantizeStep, HalfSize);

                    ValidatePosition(position, quantized);

                    var hashed = SpatialMap.Hash(quantized, QuantizeWidth);
                    keys[entityInQueryIndex] = hashed;
                    values[entityInQueryIndex] = entityInQueryIndex;
                    PositionOutput[entityInQueryIndex] = position;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidatePosition(float2 position, int2 quantized)
            {
                if (SpatialMap.IsWithinBounds(quantized, QuantizeWidth))
                {
                    return;
                }

                var min = new int2(-HalfSize);
                var max = new int2(HalfSize - 1);

                BLGlobalLogger.LogError512($"Position {position} is outside the size of the world, min={min} max={max}");
                throw new ArgumentException($"Position {position} is outside the size of the world, min={min} max={max}");
            }
        }
    }

    public static class SpatialMap
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 Quantized(float2 position, float step, int2 halfSize)
        {
            return new int2(math.floor((position + halfSize) / step));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Hash(int2 quantized, int width)
        {
            return quantized.x + (quantized.y * width);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithinBounds(int2 quantized, int width)
        {
            return math.all(quantized >= int2.zero) && math.all(quantized < width);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 Clamp(int2 quantized, int width)
        {
            return math.clamp(quantized, int2.zero, new int2(width - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CellMinDistanceSq(float2 position, int2 cell, float step, int2 halfSize)
        {
            var cellMin = (new float2(cell.x, cell.y) * step) - new float2(halfSize.x, halfSize.y);
            var cellMax = cellMin + step;
            var delta = math.max(math.max(cellMin - position, position - cellMax), 0f);
            return math.lengthsq(delta);
        }

        public readonly struct ReadOnly
        {
            private readonly float _quantizeStep;
            private readonly int _quantizeWidth;
            private readonly int2 _halfSize;

            public ReadOnly(float quantizeStep, int quantizeWidth, int2 halfSize, NativeParallelMultiHashMap<int, int> map)
            {
                _quantizeStep = quantizeStep;
                _quantizeWidth = quantizeWidth;
                _halfSize = halfSize;
                Map = map;
            }

            [field: ReadOnly]
            public NativeParallelMultiHashMap<int, int> Map { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int2 Quantized(float2 position)
            {
                return SpatialMap.Quantized(position, _quantizeStep, _halfSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Hash(int2 quantized)
            {
                return SpatialMap.Hash(quantized, _quantizeWidth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsWithinBounds(int2 quantized)
            {
                return SpatialMap.IsWithinBounds(quantized, _quantizeWidth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int2 Clamp(int2 quantized)
            {
                return SpatialMap.Clamp(quantized, _quantizeWidth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float CellMinDistanceSq(float2 position, int2 cell)
            {
                return SpatialMap.CellMinDistanceSq(position, cell, _quantizeStep, _halfSize);
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
    }
}
