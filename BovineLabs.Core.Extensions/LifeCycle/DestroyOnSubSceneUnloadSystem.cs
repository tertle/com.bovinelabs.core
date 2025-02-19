// <copyright file="DestroyOnSubSceneUnloadSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Groups;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Entities;
    using Unity.Scenes;

    [BurstCompile]
    [UpdateInGroup(typeof(BeforeSceneSystemGroup))]
    [UpdateAfter(typeof(InstantiateCommandBufferSystem))]
    [UpdateBefore(typeof(DestroySystemGroup))] // This can't run in DestroySystemGroup because it stops execution if no DestroyEntity
    public partial struct DestroyOnSubSceneUnloadSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var s in SystemAPI
                .Query<SceneSectionData>()
                .WithAll<SceneEntityReference, SceneSectionStreamingSystem.StreamingState>()
                .WithNone<RequestSceneLoaded, DisableSceneResolveAndLoad>())
            {
                var destroyQuery = SystemAPI.QueryBuilder().WithDisabled<DestroyEntity>().WithAll<SceneSection>().Build();
                destroyQuery.SetSharedComponentFilter(new SceneSection
                {
                    SceneGUID = s.SceneGUID,
                    Section = s.SubSectionIndex,
                });

                state.Dependency = new DestroyOnSubSceneUnloadJob
                {
                    DestroyEntityHandle = SystemAPI.GetComponentTypeHandle<DestroyEntity>(),
                }.ScheduleParallel(destroyQuery, state.Dependency);
            }
        }

        [BurstCompile]
        private unsafe struct DestroyOnSubSceneUnloadJob : IJobChunk
        {
            public ComponentTypeHandle<DestroyEntity> DestroyEntityHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.DestroyEntityHandle, out var ptrChunkDisabledCount);
                chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                actives.ULong0 = ulong.MaxValue & mask0;
                actives.ULong1 = ulong.MaxValue & mask1;
                chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
            }
        }
    }
}
#endif
