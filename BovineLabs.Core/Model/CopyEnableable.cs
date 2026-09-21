namespace BovineLabs.Core.Model
{
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    public struct CopyEnableable<TTo, TFrom>
        where TTo : unmanaged, IComponentData, IEnableableComponent
        where TFrom : unmanaged, IComponentData, IEnableableComponent
    {
        private EntityQuery _query;
        private ComponentTypeHandle<TTo> _toHandle;
        private ComponentTypeHandle<TFrom> _fromHandle;

        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp).WithPresentRW<TTo>().WithPresent<TFrom>().Build(ref state);

            _query.AddChangedVersionFilter(ComponentType.ReadOnly<TFrom>());

            _toHandle = state.GetComponentTypeHandle<TTo>();
            _fromHandle = state.GetComponentTypeHandle<TFrom>(true);
        }

        public void OnUpdate(ref SystemState state, SetPreviousJob job = default)
        {
            _toHandle.Update(ref state);
            _fromHandle.Update(ref state);

            job.ToHandle = _toHandle;
            job.FromHandle = _fromHandle;

            state.Dependency = job.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        public struct SetPreviousJob : IJobChunk
        {
            public ComponentTypeHandle<TTo> ToHandle;

            [ReadOnly]
            public ComponentTypeHandle<TFrom> FromHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                chunk.CopyEnableMaskFrom(ref ToHandle, ref FromHandle);
            }
        }
    }
}
