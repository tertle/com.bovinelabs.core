// <copyright file="SpatialKeyedMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Spatial
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Collections;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    public struct SpatialKeyedMap<T> : IDisposable
        where T : unmanaged, ISpatialPosition
    {
        private readonly float quantizeStep;
        private readonly int quantizeSize;
        private readonly int2 halfSize;

        private NativeKeyedMap<int> map;

        public SpatialKeyedMap(float quantizeStep, int size, Allocator allocator = Allocator.Persistent)
        {
            this.quantizeStep = quantizeStep;
            this.quantizeSize = (int)math.ceil(size / quantizeStep);
            this.halfSize = new int2(size) / 2;

            this.map = new NativeKeyedMap<int>(0, this.quantizeSize * this.quantizeSize, allocator);
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

        public JobHandle Build(NativeList<T> positions, JobHandle dependency, ResizeNativeKeyedMapJob resizeStub = default, QuantizeJob quantizeStub = default)
        {
            return this.Build(positions.AsDeferredJobArray(), dependency, resizeStub, quantizeStub);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Sneaky way to allow this to run in bursted ISystem")]
        public JobHandle Build(NativeArray<T> positions, JobHandle dependency, ResizeNativeKeyedMapJob resizeStub = default, QuantizeJob quantizeStub = default)
        {
            // Deferred native arrays are supported so we must part it into the job to get the length
            dependency = new ResizeNativeKeyedMapJob
            {
                Length = positions,
                Map = this.map,
            }.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            dependency = new QuantizeJob
            {
                Positions = positions,
                Map = this.map,
                QuantizeStep = this.quantizeStep,
                QuantizeWidth = this.quantizeSize,
                HalfSize = this.halfSize,
                Workers = workers,
            }.ScheduleParallel(workers, 1, dependency);

            dependency = new SpatialKeyedMap.CalculateMap
            {
                SpatialHashMap = this.map,
            }.Schedule(dependency);

            return dependency;
        }

        /// <summary> Gets a readonly copy of the struct that can be used to query the spatial hash map. Also includes methods to quantize and hash. </summary>
        /// <returns> A readonly container. </returns>
        public SpatialKeyedMap.ReadOnly AsReadOnly()
        {
            return new SpatialKeyedMap.ReadOnly(this.quantizeStep, this.quantizeSize, this.halfSize, this.map);
        }

        // Jobs outside to avoid generic issues
        [BurstCompile]
        public struct ResizeNativeKeyedMapJob : IJob
        {
            public NativeKeyedMap<int> Map;

            [ReadOnly]
            public NativeArray<T> Length;

            public void Execute()
            {
                if (this.Map.Capacity < this.Length.Length)
                {
                    this.Map.Capacity = this.Length.Length;
                }

                this.Map.Clear();
                this.Map.SetLength(this.Length.Length);
            }
        }

        [BurstCompile]
        [NoAlias]
        public unsafe struct QuantizeJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<T> Positions;

            [NativeDisableParallelForRestriction]
            public NativeKeyedMap<int> Map;

            public float QuantizeStep;
            public int QuantizeWidth;
            public int2 HalfSize;

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

                var keys = this.Map.GetUnsafeKeysPtr();
                var values = this.Map.GetUnsafeValuesPtr();

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = this.Positions[entityInQueryIndex].Position;
                    var quantized = SpatialKeyedMap.Quantized(position, this.QuantizeStep, this.HalfSize);

                    this.ValidatePosition(position, quantized);

                    var hashed = SpatialKeyedMap.Hash(quantized, this.QuantizeWidth);
                    keys[entityInQueryIndex] = hashed;
                    values[entityInQueryIndex] = entityInQueryIndex;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidatePosition(float2 position, int2 quantized)
            {
                if (math.any(quantized >= this.QuantizeWidth))
                {
                    var min = new int2(-this.HalfSize);
                    var max = new int2(this.HalfSize - 1);

                    BLGlobalLogger.LogError512($"Position {position} is outside the size of the world, min={min} max={max}");
                    throw new ArgumentException($"Position {position} is outside the size of the world, min={min} max={max}");
                }
            }
        }
    }

    public static class SpatialKeyedMap
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

        /// <summary> Readonly copy for querying the map. </summary>
        public readonly struct ReadOnly
        {
            private readonly float quantizeStep;
            private readonly int quantizeWidth;
            private readonly int2 halfSize;

            public ReadOnly(float quantizeStep, int quantizeWidth, int2 halfSize, NativeKeyedMap<int> map)
            {
                this.quantizeStep = quantizeStep;
                this.quantizeWidth = quantizeWidth;
                this.halfSize = halfSize;
                this.Map = map;
            }

            [field: ReadOnly]
            public NativeKeyedMap<int> Map { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int2 Quantized(float2 position)
            {
                return SpatialKeyedMap.Quantized(position, this.quantizeStep, this.halfSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Hash(int2 quantized)
            {
                return SpatialKeyedMap.Hash(quantized, this.quantizeWidth);
            }
        }

        [BurstCompile]
        internal struct CalculateMap : IJob
        {
            public NativeKeyedMap<int> SpatialHashMap;

            public void Execute()
            {
                this.SpatialHashMap.RecalculateBuckets();
            }
        }
    }
}
