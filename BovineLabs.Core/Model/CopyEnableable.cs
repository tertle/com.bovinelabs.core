// <copyright file="CopyEnableable.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        private EntityQuery query;
        private ComponentTypeHandle<TTo> toHandle;
        private ComponentTypeHandle<TFrom> fromHandle;

        public void OnCreate(ref SystemState state)
        {
            this.query = new EntityQueryBuilder(Allocator.Temp).WithPresentRW<TTo>().WithPresent<TFrom>().Build(ref state);

            this.query.AddChangedVersionFilter(ComponentType.ReadOnly<TFrom>());

            this.toHandle = state.GetComponentTypeHandle<TTo>();
            this.fromHandle = state.GetComponentTypeHandle<TFrom>(true);
        }

        public void OnUpdate(ref SystemState state, SetPreviousJob job = default)
        {
            this.toHandle.Update(ref state);
            this.fromHandle.Update(ref state);

            job.ToHandle = this.toHandle;
            job.FromHandle = this.fromHandle;

            state.Dependency = job.ScheduleParallel(this.query, state.Dependency);
        }

        [BurstCompile]
        public struct SetPreviousJob : IJobChunk
        {
            public ComponentTypeHandle<TTo> ToHandle;

            [ReadOnly]
            public ComponentTypeHandle<TFrom> FromHandle;

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                chunk.CopyEnableMaskFrom(ref this.ToHandle, ref this.FromHandle);
            }
        }
    }
}
