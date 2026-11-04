namespace BovineLabs.Core.Authoring.Entities
{
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Entities.Hybrid.Baking;

    /// <summary>
    /// Hides each <see cref="HideInHierarchyRequest"/> entity and its <see cref="LinkedEntityGroup"/> members, which a baker cannot reach. Members
    /// without a parent, such as a static presentation's children, would otherwise still be listed as roots.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    internal partial struct HideInHierarchyBakingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var memberMask = SystemAPI
                .QueryBuilder()
                .WithNone<BakingOnlyEntity, RemoveUnusedEntityInBake>()
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .Build()
                .GetEntityQueryMask();

            var members = new NativeList<Entity>(state.WorldUpdateAllocator);
            foreach (var linkedEntityGroup in SystemAPI
                .Query<DynamicBuffer<LinkedEntityGroupBakingData>>()
                .WithAll<HideInHierarchyRequest>()
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                // The first member is the requesting entity, which the request query covers.
                for (var index = 1; index < linkedEntityGroup.Length; index++)
                {
                    var linkedEntity = linkedEntityGroup[index].Value;
                    if (memberMask.MatchesIgnoreFilter(linkedEntity))
                    {
                        members.Add(linkedEntity);
                    }
                }
            }

            var requests = SystemAPI
                .QueryBuilder()
                .WithAll<HideInHierarchyRequest>()
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                .Build();

            state.EntityManager.AddComponent(requests, HideInHierarchy.Type);
            state.EntityManager.AddComponent(members.AsArray(), HideInHierarchy.Type);
        }
    }
}
