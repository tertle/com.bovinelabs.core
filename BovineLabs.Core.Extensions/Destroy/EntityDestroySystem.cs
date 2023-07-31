// <copyright file="EntityDestroySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_DESTROY
namespace BovineLabs.Core.Destroy
{
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(DestroySystemGroup), OrderLast = true)]
    public partial struct EntityDestroySystem : ISystem
    {
        private EntityQuery query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
#if UNITY_NETCODE
            // Client doesn't destroy ghosts, instead we'll disable them in
            this.query = Unity.NetCode.ClientServerWorldExtensions.IsClient(state.WorldUnmanaged)
                ? SystemAPI.QueryBuilder().WithAll<EntityDestroy>().WithNone<Unity.NetCode.GhostInstance>().Build()
                : SystemAPI.QueryBuilder().WithAll<EntityDestroy>().Build();
#else
            this.query = SystemAPI.QueryBuilder().WithAll<EntityDestroy>().Build();
#endif
            this.query.SetChangedVersionFilter(ComponentType.ReadOnly<EntityDestroy>());
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bufferSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            new DestroyJob { CommandBuffer = bufferSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter() }.ScheduleParallel(this.query);
        }

        [WithChangeFilter(typeof(EntityDestroy))]
        [WithAll(typeof(EntityDestroy))]
        [BurstCompile]
        private partial struct DestroyJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            private void Execute([ChunkIndexInQuery] int chunkIndexInQuery, Entity entity)
            {
                this.CommandBuffer.DestroyEntity(chunkIndexInQuery, entity);
            }
        }
    }
}
#endif
