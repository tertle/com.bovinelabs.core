// <copyright file="RemoveLinkedEntityGroupSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_REMOVE_LINKED_ENTITY_GROUP
namespace BovineLabs.Core.Authoring
{
    using Unity.Burst;
    using Unity.Entities;
#if UNITY_NETCODE
    using Unity.NetCode;
#endif

    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct RemoveLinkedEntityGroupSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (linkedEntityGroup, entity) in SystemAPI
                         .Query<DynamicBuffer<LinkedEntityGroup>>()
#if UNITY_NETCODE
                         .WithNone<GhostAuthoringComponentBakingData>()
#endif
                         .WithEntityAccess()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                if (linkedEntityGroup.Length > 1)
                {
                    continue;
                }

                ecb.RemoveComponent<LinkedEntityGroup>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
#endif
