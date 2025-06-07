// <copyright file="LocalSpatialMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        private readonly float quantizeStep;
        private readonly int quantizeSize;
        private readonly int2 halfSize;

        private UnsafePartialKeyedMap<T>* map;
        private UnsafeList<int>* keys;

        public LocalSpatialMap(float quantizeStep, int size, Allocator allocator = Allocator.Persistent)
        {
            this.quantizeStep = quantizeStep;
            this.quantizeSize = (int)math.ceil(size / quantizeStep);
            this.halfSize = new int2(size) / 2;

            this.map = UnsafePartialKeyedMap<T>.Create(null, null, 0, this.quantizeSize * this.quantizeSize, allocator);
            this.keys = UnsafeList<int>.Create(0, allocator);
        }

        public bool IsCreated => this.map != null;

        /// <inheritdoc />
        public void Dispose()
        {
            if (!this.IsCreated)
            {
                return;
            }

            UnsafePartialKeyedMap<T>.Destroy(this.map);
            UnsafeList<int>.Destroy(this.keys);

            this.map = null;
            this.keys = null;
        }

        public JobHandle Build(NativeList<T> positions, JobHandle dependency, ResizeKeys resizeStub = default, QuantizeJob quantizeStub = default)
        {
            return this.Build(positions.AsDeferredJobArray(), dependency, resizeStub, quantizeStub);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Sneaky way to allow this to run in bursted ISystem")]
        public JobHandle Build(
            NativeArray<T> positions, JobHandle dependency, ResizeKeys resizeStub = default, QuantizeJob quantizeStub = default, UpdateMap updateMap = default)
        {
            // Deferred native arrays are supported so we must part it into the job to get the length
            dependency = new ResizeKeys
            {
                Keys = this.keys,
                Values = positions,
            }.Schedule(dependency);

            var workers = math.max(1, JobsUtility.JobWorkerCount);
            dependency = new QuantizeJob
            {
                Positions = positions,
                Keys = this.keys,
                QuantizeStep = this.quantizeStep,
                QuantizeWidth = this.quantizeSize,
                HalfSize = this.halfSize,
                Workers = workers,
            }.ScheduleParallel(workers, 1, dependency);

            dependency = new UpdateMap
            {
                SpatialHashMap = this.map,
                Keys = this.keys,
                Values = positions,
            }.Schedule(dependency);

            return dependency;
        }

        /// <summary> Gets a readonly copy of the struct that can be used to query the spatial hash map. Also includes methods to quantize and hash. </summary>
        /// <returns> A readonly container. </returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(this.quantizeStep, this.quantizeSize, this.halfSize, this.map);
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
                this.Keys->Resize(this.Values.Length);
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
                var length = this.Positions.Length / this.Workers;
                var start = index * length;
                var end = start + length;
                if (index == this.Workers - 1)
                {
                    // Last thread handles remainder
                    end += this.Positions.Length % this.Workers;
                }

                for (var entityInQueryIndex = start; entityInQueryIndex < end; entityInQueryIndex++)
                {
                    var position = this.Positions[entityInQueryIndex].Position;
                    var quantized = PartialSpatialMap.Quantized(position, this.QuantizeStep, this.HalfSize);

                    this.ValidatePosition(position, quantized);

                    var hashed = PartialSpatialMap.Hash(quantized, this.QuantizeWidth);
                    (*this.Keys)[entityInQueryIndex] = hashed;
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

        [BurstCompile]
        public struct UpdateMap : IJob
        {
            public UnsafePartialKeyedMap<T>* SpatialHashMap;
            public UnsafeList<int>* Keys;
            public NativeArray<T> Values;

            public void Execute()
            {
                Check.Assume(this.Keys->Length == this.Values.Length);

                this.SpatialHashMap->Update(this.Keys->Ptr, (T*)this.Values.GetUnsafeReadOnlyPtr(), this.Values.Length);
            }
        }

        /// <summary> Readonly copy for querying the map. </summary>
        public readonly struct ReadOnly
        {
            private readonly float quantizeStep;
            private readonly int quantizeWidth;
            private readonly int2 halfSize;

            public ReadOnly(float quantizeStep, int quantizeWidth, int2 halfSize, UnsafePartialKeyedMap<T>* map)
            {
                this.quantizeStep = quantizeStep;
                this.quantizeWidth = quantizeWidth;
                this.halfSize = halfSize;
                this.Map = map;
            }

            [field: ReadOnly]
            public UnsafePartialKeyedMap<T>* Map { get; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int2 Quantized(float2 position)
            {
                return PartialSpatialMap.Quantized(position, this.quantizeStep, this.halfSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Hash(int2 quantized)
            {
                return PartialSpatialMap.Hash(quantized, this.quantizeWidth);
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
