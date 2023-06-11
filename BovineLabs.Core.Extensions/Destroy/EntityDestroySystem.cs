// <copyright file="EntityDestroySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_DESTROY
namespace BovineLabs.Core.Destroy
{
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    [UpdateInGroup(typeof(DestroySystemGroup), OrderLast = true)]
    public partial struct EntityDestroySystem : ISystem
    {
        private EntityTypeHandle entityTypeHandle;
        private ComponentTypeHandle<EntityDestroy> entityDestroyHandle;
        private EntityQuery query;

        /// <inheritdoc />
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.entityTypeHandle = state.GetEntityTypeHandle();
            this.entityDestroyHandle = state.GetComponentTypeHandle<EntityDestroy>();

            this.query = SystemAPI.QueryBuilder().WithAllRW<EntityDestroy>().Build();
            this.query.SetChangedVersionFilter(ComponentType.ReadWrite<EntityDestroy>());
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.entityTypeHandle.Update(ref state);
            this.entityDestroyHandle.Update(ref state);

            var bufferSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();

            state.Dependency = new DestroyJob
                {
                    EntityHandle = this.entityTypeHandle,
                    EntityDestroyHandle = this.entityDestroyHandle,
                    CommandBuffer = bufferSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                }
                .ScheduleParallel(this.query, state.Dependency);
        }

        [BurstCompile]
        private unsafe struct DestroyJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle EntityHandle;

            public ComponentTypeHandle<EntityDestroy> EntityDestroyHandle;

            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetEntityDataPtrRO(this.EntityHandle);

                // The destroy system opens with RO even though it writes to avoid bumping change filters as we only want to react to external updates
                var entityDestroys = (EntityDestroy*)chunk.GetRequiredComponentDataPtrRO(ref this.EntityDestroyHandle);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
                var anyWrite = false;
#endif

                for (var i = 0; i < chunk.Count; i++)
                {
                    if (entityDestroys[i].Value == 0)
                    {
                        continue;
                    }

                    if (entityDestroys[i].Value < 0)
                    {
                        entityDestroys[i] = EntityDestroy.Reset;
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
                        anyWrite = true;
#endif
                        continue;
                    }

                    this.CommandBuffer.DestroyEntity(unfilteredChunkIndex, entities[i]);
                }

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
                if (anyWrite)
                {
                    fixed (ArchetypeChunk* chunkPtr = &chunk)
                    {
                        chunkPtr->JournalAddRecordGetComponentDataRW(ref this.EntityDestroyHandle, entityDestroys, sizeof(EntityDestroy) * chunk.Count);
                    }
                }
#endif
            }
        }
    }
}
#endif
