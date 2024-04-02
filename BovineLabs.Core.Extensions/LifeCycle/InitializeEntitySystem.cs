// <copyright file="InitializeEntitySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializeSystemGroup), OrderLast = true)]
    public partial struct InitializeEntitySystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAny<InitializeEntity, InitializeSubSceneEntity>().Build();

            state.Dependency = new MarkInitializedJob
                {
                    InitializeEntityHandle = SystemAPI.GetComponentTypeHandle<InitializeEntity>(),
                    InitializeSubSceneEntityHandle = SystemAPI.GetComponentTypeHandle<InitializeSubSceneEntity>(),
                }
                .ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private unsafe struct MarkInitializedJob : IJobChunk
        {
            public ComponentTypeHandle<InitializeEntity> InitializeEntityHandle;
            public ComponentTypeHandle<InitializeSubSceneEntity> InitializeSubSceneEntityHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (chunk.Has(ref this.InitializeEntityHandle))
                {
                    ref var initialized = ref chunk.GetRequiredEnabledBitsRW(ref this.InitializeEntityHandle, out var ptrChunkDisabledCount);
                    initialized.ULong0 = 0;
                    initialized.ULong1 = 0;
                    *ptrChunkDisabledCount = chunk.Count;
                }
                else
                {
                    ref var initialized = ref chunk.GetRequiredEnabledBitsRW(ref this.InitializeSubSceneEntityHandle, out var ptrChunkDisabledCount);
                    initialized.ULong0 = 0;
                    initialized.ULong1 = 0;
                    *ptrChunkDisabledCount = chunk.Count;
                }
            }
        }
    }
}
#endif
