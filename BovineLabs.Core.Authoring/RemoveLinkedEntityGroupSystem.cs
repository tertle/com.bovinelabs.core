// <copyright file="RemoveLinkedEntityGroupSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Entities.Hybrid.Baking;

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct RemoveLinkedEntityGroupSystem : ISystem
    {
        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
        }

        /// <inheritdoc/>
        public void OnDestroy(ref SystemState state)
        {
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (linkedEntityGroupBaking, additionalEntitiesBakingData, entity) in SystemAPI
                         .Query<DynamicBuffer<LinkedEntityGroupBakingData>, DynamicBuffer<AdditionalEntitiesBakingData>>()
                         .WithEntityAccess()
                         .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                if (linkedEntityGroupBaking.Length > 1 || additionalEntitiesBakingData.Length > 0)
                {
                    continue;
                }

                ecb.RemoveComponent<LinkedEntityGroupBakingData>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
