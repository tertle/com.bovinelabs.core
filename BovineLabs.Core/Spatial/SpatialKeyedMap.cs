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
        private readonly float _quantizeStep;
        private readonly int _quantizeSize;
        private readonly int2 _halfSize;

        private NativeKeyedMap<int> _map;

        public SpatialKeyedMap(float quantizeStep, int size, Allocator allocator = Allocator.Persistent)
        {
            _quantizeStep = quantizeStep;
            _quantizeSize = (int)math.ceil(size / quantizeStep);
            _halfSize = new int2(size) / 2;

            _map = new NativeKeyedMap<int>(0, _quantizeSize * _quantizeSize, allocator);
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

        public JobHandle Build(NativeList<T> positions, JobHandle dependency, ResizeNativeKeyedMapJob resizeStub = default, QuantizeJob quantizeStub = default)
        {
            return Build(positions.AsDeferredJobArray(), dependency, resizeStub, quantizeStub);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Sneaky way to allow this to run in bursted ISystem")]
        public JobHandle Build(NativeArray<T> positions, JobHandle dependency, ResizeNativeKeyedMapJob resizeStub = default, QuantizeJob quantizeStub = default)
        {
            // Deferred native arrays are supported so we must part it into the job to get the length
            dependency = new ResizeNativeKeyedMapJob
            {
                Length = positions,
                Map = _map,
            }.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            dependency = new QuantizeJob
            {
                Positions = positions,
                Map = _map,
                QuantizeStep = _quantizeStep,
                QuantizeWidth = _quantizeSize,
                HalfSize = _halfSize,
                Workers = workers,
            }.ScheduleParallel(workers, 1, dependency);

            dependency = new SpatialKeyedMap.CalculateMap
            {
                SpatialHashMap = _map,
            }.Schedule(dependency);

            return dependency;
        }

        public SpatialKeyedMap.ReadOnly AsReadOnly()
        {
            return new SpatialKeyedMap.ReadOnly(_quantizeStep, _quantizeSize, _halfSize, _map);
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
                if (Map.Capacity < Length.Length)
                {
                    Map.Capacity = Length.Length;
                }

                Map.Clear();
                Map.SetLength(Length.Length);
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
                var length = Positions.Length / Workers;
                var start = index * length;
                var end = start + length;
                if (index == Workers - 1)
                {
                    // Last thread handles remainder
                    end += Positions.Length % Workers;
                }

                var keys = Map.GetUnsafeKeysPtr();
                var values = Map.GetUnsafeValuesPtr();

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = Positions[entityInQueryIndex].Position;
                    var quantized = SpatialKeyedMap.Quantized(position, QuantizeStep, HalfSize);

                    ValidatePosition(position, quantized);

                    var hashed = SpatialKeyedMap.Hash(quantized, QuantizeWidth);
                    keys[entityInQueryIndex] = hashed;
                    values[entityInQueryIndex] = entityInQueryIndex;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidatePosition(float2 position, int2 quantized)
            {
                if (math.any(quantized >= QuantizeWidth))
                {
                    var min = new int2(-HalfSize);
                    var max = new int2(HalfSize - 1);

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

        public readonly struct ReadOnly
        {
            private readonly float _quantizeStep;
            private readonly int _quantizeWidth;
            private readonly int2 _halfSize;

            public ReadOnly(float quantizeStep, int quantizeWidth, int2 halfSize, NativeKeyedMap<int> map)
            {
                _quantizeStep = quantizeStep;
                _quantizeWidth = quantizeWidth;
                _halfSize = halfSize;
                Map = map;
            }

            [field: ReadOnly]
            public NativeKeyedMap<int> Map { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int2 Quantized(float2 position)
            {
                return SpatialKeyedMap.Quantized(position, _quantizeStep, _halfSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Hash(int2 quantized)
            {
                return SpatialKeyedMap.Hash(quantized, _quantizeWidth);
            }
        }

        [BurstCompile]
        internal struct CalculateMap : IJob
        {
            public NativeKeyedMap<int> SpatialHashMap;

            public void Execute()
            {
                SpatialHashMap.RecalculateBuckets();
            }
        }
    }
}
