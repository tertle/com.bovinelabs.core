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

        float2 ISpatialPosition.Position => Position.xz;

        float3 ISpatialPosition3.Position => Position;
    }

    public struct PositionBuilder
    {
        private EntityQuery _query;
        private ComponentTypeHandle<LocalTransform> _transformHandle;

        public PositionBuilder(ref SystemState state, EntityQuery query)
        {
            _query = query;

            _transformHandle = state.GetComponentTypeHandle<LocalTransform>(true);
        }

        public JobHandle Gather(ref SystemState state, JobHandle dependency, out NativeArray<SpatialPosition> positions)
        {
            _transformHandle.Update(ref state);

            positions = state.WorldRewindableAllocator.AllocateNativeArray<SpatialPosition>(_query.CalculateEntityCount());

            var firstEntityIndices = _query.CalculateBaseEntityIndexArrayAsync(state.WorldUpdateAllocator, dependency, out dependency);

            dependency = new GatherPositionsJob
            {
                TransformHandle = _transformHandle,
                Positions = positions,
                FirstEntityIndices = firstEntityIndices,
            }.ScheduleParallel(_query, dependency);

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

                var ptr = (float3*)Positions.GetUnsafePtr();
                var dst = ptr + FirstEntityIndices[unfilteredChunkIndex];

                var size = UnsafeUtility.SizeOf<float3>();
                var positions = chunk.GetNativeArray(ref TransformHandle).Slice().SliceWithStride<float3>(0);

                UnsafeUtility.MemCpyStride(dst, size, positions.GetUnsafeReadOnlyPtr(), positions.Stride, size, positions.Length);
            }
        }
    }
}
