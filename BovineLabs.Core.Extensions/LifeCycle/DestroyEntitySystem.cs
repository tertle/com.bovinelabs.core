// <copyright file="DestroyEntitySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using Unity.Burst;
    using Unity.Entities;

    [UpdateBefore(typeof(DestroyEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(DestroySystemGroup), OrderLast = true)]
    public partial struct DestroyEntitySystem : ISystem
    {
        private EntityQuery query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
#if UNITY_NETCODE
            // Client doesn't destroy ghosts, instead we'll disable them in
            this.query = Unity.NetCode.ClientServerWorldExtensions.IsClient(state.WorldUnmanaged)
                ? SystemAPI.QueryBuilder().WithAll<DestroyEntity>().WithNone<Unity.NetCode.GhostInstance>().Build()
                : SystemAPI.QueryBuilder().WithAll<DestroyEntity>().Build();
#else
            this.query = SystemAPI.QueryBuilder().WithAll<DestroyEntity>().Build();
#endif
            this.query.SetChangedVersionFilter(ComponentType.ReadOnly<DestroyEntity>());
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bufferSingleton = SystemAPI.GetSingleton<DestroyEntityCommandBufferSystem.Singleton>();
            new DestroyJob { CommandBuffer = bufferSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter() }.ScheduleParallel(this.query);
        }

        [WithChangeFilter(typeof(DestroyEntity))]
        [WithAll(typeof(DestroyEntity))]
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
