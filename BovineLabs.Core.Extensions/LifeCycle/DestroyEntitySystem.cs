// <copyright file="DestroyEntitySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using Unity.Burst;
    using Unity.Entities;
#if UNITY_NETCODE
    using Unity.NetCode;
#endif

    [UpdateBefore(typeof(DestroyEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(DestroySystemGroup), OrderLast = true)]
    public partial struct DestroyEntitySystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
#if UNITY_NETCODE
            // Client doesn't destroy ghosts, instead we'll disable them in
            var query = state.WorldUnmanaged.IsClient()
                ? SystemAPI.QueryBuilder().WithAll<DestroyEntity>().WithNone<GhostInstance>().Build()
                : SystemAPI.QueryBuilder().WithAll<DestroyEntity>().Build();
#else
            var query = SystemAPI.QueryBuilder().WithAll<DestroyEntity>().Build();
#endif

            if (query.IsEmpty)
            {
                return;
            }

            state.EntityManager.DestroyEntity(query.ToEntityArray(state.WorldUpdateAllocator));
        }
    }
}
#endif
