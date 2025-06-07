// <copyright file="DestroyEntitySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
#if UNITY_NETCODE
    using Unity.NetCode;
#endif

    [UpdateAfter(typeof(DestroyEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(DestroySystemGroup), OrderLast = true)]
    public partial struct DestroyEntitySystem : ISystem
    {
        private EntityQuery legQuery;
        private EntityQuery query;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var queryBuilder = new EntityQueryBuilder(Allocator.Temp);

#if UNITY_NETCODE
            // Client doesn't destroy ghosts, instead we'll disable them in
            if (state.WorldUnmanaged.IsClient())
            {
                this.legQuery = queryBuilder.WithAll<DestroyEntity, LinkedEntityGroup>().WithNone<GhostInstance>().Build(ref state);
                queryBuilder.Reset();
                this.query = queryBuilder.WithAll<DestroyEntity>().WithNone<GhostInstance, LinkedEntityGroup>().Build(ref state);
            }
            else
#endif
            {
                this.legQuery = queryBuilder.WithAll<DestroyEntity, LinkedEntityGroup>().Build(ref state);
                queryBuilder.Reset();
                this.query = queryBuilder.WithAll<DestroyEntity>().WithNone<LinkedEntityGroup>().Build(ref state);
            }
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!this.query.IsEmpty)
            {
                state.EntityManager.DestroyEntity(this.query);
            }

            if (!this.legQuery.IsEmpty)
            {
                state.EntityManager.DestroyEntity(this.legQuery.ToEntityArray(Allocator.Temp));
            }
        }
    }
}
#endif
