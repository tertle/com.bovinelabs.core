// <copyright file="PositionBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Spatial
{
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Transforms;

    public struct SpatialPosition : ISpatialPosition, ISpatialPosition3
    {
        public float3 Position;

        float2 ISpatialPosition.Position => this.Position.xz;

        float3 ISpatialPosition3.Position => this.Position;
    }

    public struct PositionBuilder
    {
        private EntityQuery query;
        private ComponentTypeHandle<LocalTransform> transformHandle;

        /// <summary> Initializes a new instance of the <see cref="PositionBuilder" /> struct. </summary>
        /// <param name="state"> The owning state. </param>
        /// <param name="query"> Query of entities to use. </param>
        public PositionBuilder(ref SystemState state, EntityQuery query)
        {
            this.query = query;

            this.transformHandle = state.GetComponentTypeHandle<LocalTransform>(true);
        }

        public JobHandle Gather(ref SystemState state, JobHandle dependency, out NativeArray<SpatialPosition> positions)
        {
            this.transformHandle.Update(ref state);

            positions = state.WorldRewindableAllocator.AllocateNativeArray<SpatialPosition>(this.query.CalculateEntityCount());

            var firstEntityIndices = this.query.CalculateBaseEntityIndexArrayAsync(state.WorldUpdateAllocator, dependency, out dependency);

            dependency = new GatherPositionsJob
            {
                TransformHandle = this.transformHandle,
                Positions = positions,
                FirstEntityIndices = firstEntityIndices,
            }.ScheduleParallel(this.query, dependency);

            return dependency;
        }

        [BurstCompile]
        private unsafe struct GatherPositionsJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<LocalTransform> TransformHandle;

            [NativeDisableParallelForRestriction]
            public NativeArray<SpatialPosition> Positions;

            [ReadOnly]
            public NativeArray<int> FirstEntityIndices;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Assert.IsFalse(useEnabledMask, "PositionBuilder does not support enable components");

                var ptr = (float3*)this.Positions.GetUnsafePtr();
                var dst = ptr + this.FirstEntityIndices[unfilteredChunkIndex];

                var size = UnsafeUtility.SizeOf<float3>();
                var positions = chunk.GetNativeArray(ref this.TransformHandle).Slice().SliceWithStride<float3>(0);

                UnsafeUtility.MemCpyStride(dst, size, positions.GetUnsafeReadOnlyPtr(), positions.Stride, size, positions.Length);
            }
        }
    }
}
