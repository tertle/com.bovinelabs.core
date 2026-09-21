namespace BovineLabs.Core.Utility
{
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    public struct SyncEnableStateUtil<T, TP>
        where T : unmanaged, IComponentData, IEnableableComponent
        where TP : unmanaged, IComponentData, IEnableableComponent
    {
        private EntityQuery _query;
        private ComponentTypeHandle<TP> _activePreviousHandle;
        private ComponentTypeHandle<T> _activeHandle;

        public void OnCreate(ref SystemState state, bool includeDisabled = false)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithPresentRW<TP>().WithPresent<T>();

            if (includeDisabled)
            {
                builder = builder.WithOptions(EntityQueryOptions.IncludeDisabledEntities);
            }

            _query = builder.Build(ref state);

            _activePreviousHandle = state.GetComponentTypeHandle<TP>();
            _activeHandle = state.GetComponentTypeHandle<T>(true);
        }

        public void OnUpdate(ref SystemState state, SetPreviousJob job = default)
        {
            _activePreviousHandle.Update(ref state);
            _activeHandle.Update(ref state);

            job.ActivePreviousHandle = _activePreviousHandle;
            job.ActiveHandle = _activeHandle;
            state.Dependency = job.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        public struct SetPreviousJob : IJobChunk
        {
            public ComponentTypeHandle<TP> ActivePreviousHandle;

            [ReadOnly]
            public ComponentTypeHandle<T> ActiveHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                chunk.CopyEnableMaskFrom(ref ActivePreviousHandle, ref ActiveHandle);
            }
        }
    }
}
