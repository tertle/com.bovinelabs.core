// <copyright file="SpatialMap3.cs" company="BovineLabs">
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

    public struct SpatialMap3<T> : IDisposable
        where T : unmanaged, ISpatialPosition3
    {
        private readonly float quantizeStep;
        private readonly int quantizeSize;
        private readonly int3 halfSize;

        private NativeParallelMultiHashMap<long, int> map;

        public SpatialMap3(float quantizeStep, int size, Allocator allocator = Allocator.Persistent)
        {
            this.quantizeStep = quantizeStep;
            this.quantizeSize = (int)math.ceil(size / quantizeStep);
            this.halfSize = new int3(size) / 2;

            this.map = new NativeParallelMultiHashMap<long, int>(0, allocator);
        }

        public bool IsCreated => this.map.IsCreated;

        /// <inheritdoc />
        public void Dispose()
        {
            this.map.Dispose();
        }

        /// <summary> Queue disposing after a dependency. </summary>
        /// <param name="dependency"> The dependency. </param>
        public void Dispose(JobHandle dependency)
        {
            this.map.Dispose(dependency);
        }

        public JobHandle Build(
            NativeList<T> positions, JobHandle dependency, ResizeNativeParallelHashMapJob resizeStub = default, QuantizeJob quantizeStub = default)
        {
            return this.Build(positions.AsDeferredJobArray(), dependency, resizeStub, quantizeStub);
        }

        public JobHandle BuildWithPositionOutput(
            NativeList<T> positions,
            NativeArray<float2> positionOutput,
            JobHandle dependency,
            ResizeNativeParallelHashMapJob resizeStub = default,
            QuantizePositionJob quantizeJob = default)
        {
            return this.BuildWithPositionOutput(positions.AsDeferredJobArray(), positionOutput, dependency, resizeStub, quantizeJob);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Sneaky way to allow this to run in bursted ISystem")]
        public JobHandle Build(
            NativeArray<T> positions, JobHandle dependency, ResizeNativeParallelHashMapJob resizeJob = default, QuantizeJob quantizeJob = default)
        {
            // Deferred native arrays are supported so we must part it into the job to get the length
            resizeJob.Length = positions;
            resizeJob.Map = this.map;
            dependency = resizeJob.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            quantizeJob.Positions = positions;
            quantizeJob.Map = this.map;
            quantizeJob.QuantizeStep = this.quantizeStep;
            quantizeJob.QuantizeWidth = this.quantizeSize;
            quantizeJob.QuantizeDepth = this.quantizeSize;
            quantizeJob.HalfSize = this.halfSize;
            quantizeJob.Workers = workers;
            dependency = quantizeJob.ScheduleParallel(workers, 1, dependency);

            dependency = new SpatialMap3.CalculateMap
            {
                SpatialHashMap = this.map,
            }.Schedule(dependency);

            return dependency;
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Sneaky way to allow this to run in bursted ISystem")]
        public JobHandle BuildWithPositionOutput(
            NativeArray<T> positions,
            NativeArray<float2> positionOutput,
            JobHandle dependency,
            ResizeNativeParallelHashMapJob resizeJob = default,
            QuantizePositionJob quantizeJob = default)
        {
            resizeJob.Length = positions;
            resizeJob.Map = this.map;
            dependency = resizeJob.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            quantizeJob.Positions = positions;
            quantizeJob.Map = this.map;
            quantizeJob.PositionOutput = positionOutput;
            quantizeJob.QuantizeStep = this.quantizeStep;
            quantizeJob.QuantizeWidth = this.quantizeSize;
            quantizeJob.QuantizeDepth = this.quantizeSize;
            quantizeJob.HalfSize = this.halfSize;
            quantizeJob.Workers = workers;
            dependency = quantizeJob.ScheduleParallel(workers, 1, dependency);

            dependency = new SpatialMap3.CalculateMap
            {
                SpatialHashMap = this.map,
            }.Schedule(dependency);

            return dependency;
        }

        /// <summary> Gets a readonly copy of the struct that can be used to query the spatial hash map. Also includes methods to quantize and hash. </summary>
        /// <returns> A readonly container. </returns>
        public SpatialMap3.ReadOnly AsReadOnly()
        {
            return new SpatialMap3.ReadOnly(this.quantizeStep, this.quantizeSize, this.quantizeSize, this.halfSize, this.map);
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
            public NativeParallelMultiHashMap<long, int> Map;

            public float QuantizeStep;
            public int QuantizeWidth;
            public int QuantizeDepth;
            public int3 HalfSize;

            public int Workers;

            public void Execute(int index)
            {
                var length = this.Positions.Length / this.Workers;
                var start = index * length;
                var end = start + length;
                if (index == this.Workers - 1)
                {
                    // Last thread handles remainder
                    end += this.Positions.Length % this.Workers;
                }

                var keys = (long*)this.Map.GetUnsafeBucketData().keys;
                var values = (int*)this.Map.GetUnsafeBucketData().values;

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = this.Positions[entityInQueryIndex].Position;
                    var quantized = SpatialMap3.Quantized(position, this.QuantizeStep, this.HalfSize);

                    this.ValidatePosition(position, quantized);

                    var hashed = SpatialMap3.Hash(quantized, this.QuantizeWidth, this.QuantizeDepth);
                    keys[entityInQueryIndex] = hashed;
                    values[entityInQueryIndex] = entityInQueryIndex;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidatePosition(float3 position, int3 quantized)
            {
                if (SpatialMap3.IsWithinBounds(quantized, this.QuantizeWidth))
                {
                    return;
                }

                var min = new int3(-this.HalfSize);
                var max = new int3(this.HalfSize - 1);

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
                var length = this.Positions.Length / this.Workers;
                var start = index * length;
                var end = start + length;
                if (index == this.Workers - 1)
                {
                    // Last thread handles remainder
                    end += this.Positions.Length % this.Workers;
                }

                var keys = (long*)this.Map.GetUnsafeBucketData().keys;
                var values = (int*)this.Map.GetUnsafeBucketData().values;

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = this.Positions[entityInQueryIndex].Position;
                    var quantized = SpatialMap3.Quantized(position, this.QuantizeStep, this.HalfSize);

                    this.ValidatePosition(position, quantized);

                    var hashed = SpatialMap3.Hash(quantized, this.QuantizeWidth, this.QuantizeDepth);
                    keys[entityInQueryIndex] = hashed;
                    values[entityInQueryIndex] = entityInQueryIndex;
                    this.PositionOutput[entityInQueryIndex] = position.xz;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidatePosition(float3 position, int3 quantized)
            {
                if (SpatialMap3.IsWithinBounds(quantized, this.QuantizeWidth))
                {
                    return;
                }

                var min = new int3(-this.HalfSize);
                var max = new int3(this.HalfSize - 1);

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

        /// <summary> Readonly copy for querying the map. </summary>
        public readonly struct ReadOnly
        {
            private readonly float quantizeStep;
            private readonly int quantizeWidth;
            private readonly int quantizeDepth;
            private readonly int3 halfSize;

            public ReadOnly(float quantizeStep, int quantizeWidth, int quantizeDepth, int3 halfSize, NativeParallelMultiHashMap<long, int> map)
            {
                this.quantizeStep = quantizeStep;
                this.quantizeWidth = quantizeWidth;
                this.quantizeDepth = quantizeDepth;
                this.halfSize = halfSize;
                this.Map = map;
            }

            [field: ReadOnly]
            public NativeParallelMultiHashMap<long, int> Map { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int3 Quantized(float3 position)
            {
                return SpatialMap3.Quantized(position, this.quantizeStep, this.halfSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public long Hash(int3 quantized)
            {
                return SpatialMap3.Hash(quantized, this.quantizeWidth, this.quantizeDepth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsWithinBounds(int3 quantized)
            {
                return SpatialMap3.IsWithinBounds(quantized, this.quantizeWidth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int3 Clamp(int3 quantized)
            {
                return SpatialMap3.Clamp(quantized, this.quantizeWidth);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float CellMinDistanceSqXZ(float2 position, int3 cell)
            {
                return SpatialMap3.CellMinDistanceSqXZ(position, cell, this.quantizeStep, this.halfSize);
            }
        }

        [BurstCompile]
        internal struct CalculateMap : IJob
        {
            public NativeParallelMultiHashMap<long, int> SpatialHashMap;

            public void Execute()
            {
                this.SpatialHashMap.RecalculateBuckets();
            }
        }
    }
}
