// <copyright file="PositionBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Spatial
{
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Transforms;

    public struct SpatialPosition : ISpatialPosition
    {
        public float3 Position;

        float2 ISpatialPosition.Position => this.Position.xz;
    }

    public struct PositionBuilder
    {
        private EntityQuery query;
        private TransformAspect.TypeHandle transformHandle;

        /// <summary> Initializes a new instance of the <see cref="PositionBuilder" /> struct. </summary>
        /// <param name="state"> The owning state. </param>
        /// <param name="query"> Query of entities to use. </param>
        public PositionBuilder(ref SystemState state, EntityQuery query)
        {
            this.query = query;

            this.transformHandle = new TransformAspect.TypeHandle(ref state, true);
        }

        public JobHandle Gather(ref SystemState state, JobHandle dependency, out NativeArray<SpatialPosition> positions)
        {
            this.transformHandle.Update(ref state);

            positions = state.WorldRewindableAllocator.AllocateNativeArray<SpatialPosition>(this.query.CalculateEntityCount());

            var firstEntityIndices = this.query.CalculateBaseEntityIndexArrayAsync(Allocator.TempJob, dependency, out var dependency1);
            dependency = JobHandle.CombineDependencies(dependency, dependency1);

            dependency = new GatherPositionsJob
                {
                    TransformHandle = this.transformHandle,
                    Positions = positions,
                    FirstEntityIndices = firstEntityIndices,
                }
                .ScheduleParallel(this.query, dependency);

            return dependency;
        }

        [BurstCompile]
        private unsafe struct GatherPositionsJob : IJobChunk
        {
            [ReadOnly]
            public TransformAspect.TypeHandle TransformHandle;

            [NativeDisableParallelForRestriction]
            public NativeArray<SpatialPosition> Positions;

            [ReadOnly]
            public NativeArray<int> FirstEntityIndices;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var ptr = (float3*)this.Positions.GetUnsafePtr();
                var dst = ptr + this.FirstEntityIndices[unfilteredChunkIndex];

                var size = UnsafeUtility.SizeOf<float3>();

                var transforms = this.TransformHandle.Resolve(chunk);
                var positions = transforms.HasWorldTransforms()
                    ? transforms.WorldTransforms().Slice().SliceWithStride<float3>(0)
                    : transforms.LocalTransforms().Slice().SliceWithStride<float3>(0);

                UnsafeUtility.MemCpyStride(dst, size, positions.GetUnsafeReadOnlyPtr(), positions.Stride, size, positions.Length);
            }
        }
    }
}
