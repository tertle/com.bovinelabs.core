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

    public struct SpatialMap3<T> : IDisposable
        where T : unmanaged, ISpatialPosition3
    {
        private readonly float _quantizeStep;
        private readonly int _quantizeSize;
        private readonly int3 _halfSize;

        private NativeParallelMultiHashMap<long, int> _map;

        public SpatialMap3(float quantizeStep, int size, Allocator allocator = Allocator.Persistent)
        {
            _quantizeStep = quantizeStep;
            _quantizeSize = (int)math.ceil(size / quantizeStep);
            _halfSize = new int3(size) / 2;

            _map = new NativeParallelMultiHashMap<long, int>(0, allocator);
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
            // Deferred native arrays are supported so we must part it into the job to get the length
            resizeJob.Length = positions;
            resizeJob.Map = _map;
            dependency = resizeJob.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            quantizeJob.Positions = positions;
            quantizeJob.Map = _map;
            quantizeJob.QuantizeStep = _quantizeStep;
            quantizeJob.QuantizeWidth = _quantizeSize;
            quantizeJob.QuantizeDepth = _quantizeSize;
            quantizeJob.HalfSize = _halfSize;
            quantizeJob.Workers = workers;
            dependency = quantizeJob.ScheduleParallel(workers, 1, dependency);

            dependency = new SpatialMap3.CalculateMap
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
            quantizeJob.QuantizeDepth = _quantizeSize;
            quantizeJob.HalfSize = _halfSize;
            quantizeJob.Workers = workers;
            dependency = quantizeJob.ScheduleParallel(workers, 1, dependency);

            dependency = new SpatialMap3.CalculateMap
            {
                SpatialHashMap = _map,
            }.Schedule(dependency);

            return dependency;
        }

        public SpatialMap3.ReadOnly AsReadOnly()
        {
            return new SpatialMap3.ReadOnly(_quantizeStep, _quantizeSize, _quantizeSize, _halfSize, _map);
        }

        // Jobs outside to avoid generic issues
        [BurstCompile]
        public struct ResizeNativeParallelHashMapJob : IJob
        {
            public NativeParallelMultiHashMap<long, int> Map;

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
            public NativeParallelMultiHashMap<long, int> Map;

            public float QuantizeStep;
            public int QuantizeWidth;
            public int QuantizeDepth;
            public int3 HalfSize;

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

                var keys = (long*)Map.GetUnsafeBucketData().keys;
                var values = (int*)Map.GetUnsafeBucketData().values;

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = Positions[entityInQueryIndex].Position;
                    var quantized = SpatialMap3.Quantized(position, QuantizeStep, HalfSize);

                    ValidatePosition(position, quantized);

                    var hashed = SpatialMap3.Hash(quantized, QuantizeWidth, QuantizeDepth);
                    keys[entityInQueryIndex] = hashed;
                    values[entityInQueryIndex] = entityInQueryIndex;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidatePosition(float3 position, int3 quantized)
            {
                if (SpatialMap3.IsWithinBounds(quantized, QuantizeWidth))
                {
                    return;
                }

                var min = new int3(-HalfSize);
                var max = new int3(HalfSize - 1);

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
            public NativeParallelMultiHashMap<long, int> Map;

            [NativeDisableParallelForRestriction]
            [WriteOnly]
            public NativeArray<float2> PositionOutput;

            public float QuantizeStep;
            public int QuantizeWidth;
            public int QuantizeDepth;
            public int3 HalfSize;

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

                var keys = (long*)Map.GetUnsafeBucketData().keys;
                var values = (int*)Map.GetUnsafeBucketData().values;

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = Positions[entityInQueryIndex].Position;
                    var quantized = SpatialMap3.Quantized(position, QuantizeStep, HalfSize);

                    ValidatePosition(position, quantized);

                    var hashed = SpatialMap3.Hash(quantized, QuantizeWidth, QuantizeDepth);
                    keys[entityInQueryIndex] = hashed;
                    values[entityInQueryIndex] = entityInQueryIndex;
                    PositionOutput[entityInQueryIndex] = position.xz;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidatePosition(float3 position, int3 quantized)
            {
                if (SpatialMap3.IsWithinBounds(quantized, QuantizeWidth))
                {
                    return;
                }

                var min = new int3(-HalfSize);
                var max = new int3(HalfSize - 1);

                BLGlobalLogger.LogError512($"Position {position} is outside the size of the world, min={min} max={max}");
                throw new ArgumentException($"Position {position} is outside the size of the world, min={min} max={max}");
            }
        }
    }

    public static class SpatialMap3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Quantized(float3 position, float step, int3 halfSize)
        {
            return new int3(math.floor((position + halfSize) / step));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Hash(int3 quantized, int width, int depth)
        {
            return quantized.x + (quantized.y * width) + (quantized.z * width * depth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithinBounds(int3 quantized, int width)
        {
            return math.all(quantized >= int3.zero) && math.all(quantized < width);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Clamp(int3 quantized, int width)
        {
            return math.clamp(quantized, int3.zero, new int3(width - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CellMinDistanceSqXZ(float2 position, int3 cell, float step, int3 halfSize)
        {
            var cellMin = (new float2(cell.x, cell.z) * step) - new float2(halfSize.x, halfSize.z);
            var cellMax = cellMin + step;
            var delta = math.max(math.max(cellMin - position, position - cellMax), 0f);
            return math.lengthsq(delta);
        }

        public readonly struct ReadOnly
        {
            private readonly float _quantizeStep;
            private readonly int _quantizeWidth;
            private readonly int _quantizeDepth;
            private readonly int3 _halfSize;

            public ReadOnly(float quantizeStep, int quantizeWidth, int quantizeDepth, int3 halfSize, NativeParallelMultiHashMap<long, int> map)
            {
                _quantizeStep = quantizeStep;
                _quantizeWidth = quantizeWidth;
                _quantizeDepth = quantizeDepth;
                _halfSize = halfSize;
                Map = map;
            }

            [field: ReadOnly]
            public NativeParallelMultiHashMap<long, int> Map { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int3 Quantized(float3 position)
            {
                return SpatialMap3.Quantized(position, _quantizeStep, _halfSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public long Hash(int3 quantized)
            {
                return SpatialMap3.Hash(quantized, _quantizeWidth, _quantizeDepth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsWithinBounds(int3 quantized)
            {
                return SpatialMap3.IsWithinBounds(quantized, _quantizeWidth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int3 Clamp(int3 quantized)
            {
                return SpatialMap3.Clamp(quantized, _quantizeWidth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float CellMinDistanceSqXZ(float2 position, int3 cell)
            {
                return SpatialMap3.CellMinDistanceSqXZ(position, cell, _quantizeStep, _halfSize);
            }
        }

        [BurstCompile]
        internal struct CalculateMap : IJob
        {
            public NativeParallelMultiHashMap<long, int> SpatialHashMap;

            public void Execute()
            {
                SpatialHashMap.RecalculateBuckets();
            }
        }
    }
}
