namespace BovineLabs.Core.Spatial
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Collections;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    public unsafe struct LocalSpatialMap<T> : IDisposable
        where T : unmanaged, ISpatialPosition
    {
        private readonly float _quantizeStep;
        private readonly int _quantizeSize;
        private readonly int2 _halfSize;

        private UnsafePartialKeyedMap<T>* _map;
        private UnsafeList<int>* _keys;

        public LocalSpatialMap(float quantizeStep, int size, Allocator allocator = Allocator.Persistent)
        {
            _quantizeStep = quantizeStep;
            _quantizeSize = (int)math.ceil(size / quantizeStep);
            _halfSize = new int2(size) / 2;

            _map = UnsafePartialKeyedMap<T>.Create(null, null, 0, _quantizeSize * _quantizeSize, allocator);
            _keys = UnsafeList<int>.Create(0, allocator);
        }

        public bool IsCreated => _map != null;

        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            UnsafePartialKeyedMap<T>.Destroy(_map);
            UnsafeList<int>.Destroy(_keys);

            _map = null;
            _keys = null;
        }

        public JobHandle Build(NativeList<T> positions, JobHandle dependency, ResizeKeys resizeStub = default, QuantizeJob quantizeStub = default)
        {
            return Build(positions.AsDeferredJobArray(), dependency, resizeStub, quantizeStub);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Sneaky way to allow this to run in bursted ISystem")]
        public JobHandle Build(
            NativeArray<T> positions, JobHandle dependency, ResizeKeys resizeStub = default, QuantizeJob quantizeStub = default, UpdateMap updateMap = default)
        {
            // Deferred native arrays are supported so we must part it into the job to get the length
            dependency = new ResizeKeys
            {
                Keys = _keys,
                Values = positions,
            }.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            dependency = new QuantizeJob
            {
                Positions = positions,
                Keys = _keys,
                QuantizeStep = _quantizeStep,
                QuantizeWidth = _quantizeSize,
                HalfSize = _halfSize,
                Workers = workers,
            }.ScheduleParallel(workers, 1, dependency);

            dependency = new UpdateMap
            {
                SpatialHashMap = _map,
                Keys = _keys,
                Values = positions,
            }.Schedule(dependency);

            return dependency;
        }

        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(_quantizeStep, _quantizeSize, _halfSize, _map);
        }

        // Jobs outside to avoid generic issues
        [BurstCompile]
        public struct ResizeKeys : IJob
        {
            public UnsafeList<int>* Keys;

            [ReadOnly]
            public NativeArray<T> Values;

            public void Execute()
            {
                Keys->Resize(Values.Length);
            }
        }

        [BurstCompile]
        [NoAlias]
        public struct QuantizeJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<T> Positions;

            [NativeDisableParallelForRestriction]
            public UnsafeList<int>* Keys;

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

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = Positions[entityInQueryIndex].Position;
                    var quantized = PartialSpatialMap.Quantized(position, QuantizeStep, HalfSize);

                    ValidatePosition(position, quantized);

                    var hashed = PartialSpatialMap.Hash(quantized, QuantizeWidth);
                    (*Keys)[entityInQueryIndex] = hashed;
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

        [BurstCompile]
        public struct UpdateMap : IJob
        {
            public UnsafePartialKeyedMap<T>* SpatialHashMap;
            public UnsafeList<int>* Keys;
            public NativeArray<T> Values;

            public void Execute()
            {
                Check.Assume(Keys->Length == Values.Length);

                SpatialHashMap->Update(Keys->Ptr, (T*)Values.GetUnsafeReadOnlyPtr(), Values.Length);
            }
        }

        public readonly struct ReadOnly
        {
            private readonly float _quantizeStep;
            private readonly int _quantizeWidth;
            private readonly int2 _halfSize;

            public ReadOnly(float quantizeStep, int quantizeWidth, int2 halfSize, UnsafePartialKeyedMap<T>* map)
            {
                _quantizeStep = quantizeStep;
                _quantizeWidth = quantizeWidth;
                _halfSize = halfSize;
                Map = map;
            }

            [field: ReadOnly]
            public UnsafePartialKeyedMap<T>* Map { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int2 Quantized(float2 position)
            {
                return PartialSpatialMap.Quantized(position, _quantizeStep, _halfSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Hash(int2 quantized)
            {
                return PartialSpatialMap.Hash(quantized, _quantizeWidth);
            }
        }
    }

    public static class PartialSpatialMap
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
    }
}
