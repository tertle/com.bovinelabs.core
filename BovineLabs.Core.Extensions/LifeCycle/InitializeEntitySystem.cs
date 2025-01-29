// <copyright file="InitializeEntitySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializeSystemGroup), OrderLast = true)]
    public partial struct InitializeEntitySystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAny<InitializeEntity, InitializeSubSceneEntity>().Build();

            state.Dependency = new MarkInitializedJob
            {
                InitializeEntityHandle = SystemAPI.GetComponentTypeHandle<InitializeEntity>(),
                InitializeSubSceneEntityHandle = SystemAPI.GetComponentTypeHandle<InitializeSubSceneEntity>(),
            }.ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private struct MarkInitializedJob : IJobChunk
        {
            public ComponentTypeHandle<InitializeEntity> InitializeEntityHandle;
            public ComponentTypeHandle<InitializeSubSceneEntity> InitializeSubSceneEntityHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (chunk.Has(ref this.InitializeEntityHandle))
                {
                    chunk.SetComponentEnabledForAll(ref this.InitializeEntityHandle, false);
                }
                else
                {
                    chunk.SetComponentEnabledForAll(ref this.InitializeSubSceneEntityHandle, false);
                }
            }
        }
    }
}
#endif
