// <copyright file="SyncEnableStateUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        private EntityQuery query;
        private ComponentTypeHandle<TP> activePreviousHandle;
        private ComponentTypeHandle<T> activeHandle;

        public void OnCreate(ref SystemState state, bool includeDisabled = false)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithPresentRW<TP>().WithPresent<T>();

            if (includeDisabled)
            {
                builder = builder.WithOptions(EntityQueryOptions.IncludeDisabledEntities);
            }

            this.query = builder.Build(ref state);

            this.activePreviousHandle = state.GetComponentTypeHandle<TP>();
            this.activeHandle = state.GetComponentTypeHandle<T>(true);
        }

        public void OnUpdate(ref SystemState state, SetPreviousJob job = default)
        {
            this.activePreviousHandle.Update(ref state);
            this.activeHandle.Update(ref state);

            job.ActivePreviousHandle = this.activePreviousHandle;
            job.ActiveHandle = this.activeHandle;
            state.Dependency = job.ScheduleParallel(this.query, state.Dependency);
        }

        [BurstCompile]
        public struct SetPreviousJob : IJobChunk
        {
            public ComponentTypeHandle<TP> ActivePreviousHandle;

            [ReadOnly]
            public ComponentTypeHandle<T> ActiveHandle;

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                chunk.CopyEnableMaskFrom(ref this.ActivePreviousHandle, ref this.ActiveHandle);
            }
        }
    }
}
