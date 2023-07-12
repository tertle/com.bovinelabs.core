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
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bufferSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            new DestroyJob { CommandBuffer = bufferSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter() }.ScheduleParallel();
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
