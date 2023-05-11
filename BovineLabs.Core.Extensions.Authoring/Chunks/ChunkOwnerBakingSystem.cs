// <copyright file="ChunkOwnerBakingSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Authoring.Chunks
{
    using System;
    using BovineLabs.Core.Chunks;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Entities.Hybrid.Baking;
    using UnityEngine;

    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct ChunkOwnerBakingSystem : ISystem
    {
        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            TypeMangerEx.Initialize();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var virtualChunkQuery = SystemAPI.QueryBuilder()
                .WithAll<ChunkOwnerBaking, LinkedEntityGroup>()
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities).Build();

            var uniqueComponents = new NativeHashSet<ComponentType>(128, Allocator.Temp);
            var chunks = virtualChunkQuery.ToArchetypeChunkArray(Allocator.Temp);

            var componentMapping = new NativeParallelMultiHashMap<byte, ComponentType>(128, Allocator.Temp);

            // -1 because 0 is always parent and we don't create that
            Span<Entity> linkedEntities = stackalloc Entity[ChunkLinks.MaxGroupIDs - 1];

            var entityHandle = SystemAPI.GetEntityTypeHandle();
            var linkedEntityGroupHandle = SystemAPI.GetBufferTypeHandle<LinkedEntityGroup>();

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var chunk in chunks)
            {
                componentMapping.Clear();

                var components = chunk.Archetype.GetComponentTypes();

                foreach (var component in components)
                {
                    var groupIndex = TypeMangerEx.GetGroupIndex(component);
                    if (groupIndex == 0)
                    {
                        continue;
                    }

                    uniqueComponents.Add(component);
                    componentMapping.Add(groupIndex, component);
                }

                byte groupMask = 1 << 0; // we are always 0
                var keys = componentMapping.GetUniqueKeyArray(Allocator.Temp); // TODO re-use below

                for (var i = 0; i < keys.Item2; i++)
                {
                    var groupID = keys.Item1[i];

                    var bit = 1 << groupID;
                    if ((groupMask & bit) != 0)
                    {
                        Debug.LogError($"Multiple links with the same groupID {groupID}");
                    }

                    groupMask = (byte)(groupMask | bit);
                }

                var entities = chunk.GetEntityDataPtrRO(entityHandle);
                var linkedEntityGroups = chunk.GetBufferAccessor(ref linkedEntityGroupHandle);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];

                    // TODO set per chunk?
                    ecb.AddSharedComponent(entity, new ChunkParent { ChildMask = groupMask });

                    linkedEntities.Clear();

                    var leg = linkedEntityGroups[i];
                    foreach (var linkedEntity in leg.AsNativeArray().Reinterpret<Entity>())
                    {
                        if (state.EntityManager.HasComponent<ChunkGroupID>(linkedEntity))
                        {
                            var groupID = state.EntityManager.GetSharedComponent<ChunkGroupID>(linkedEntity).Value;
                            linkedEntities[groupID - 1] = linkedEntity;
                        }
                    }

                    using var e = componentMapping.GetEnumerator();
                    while (e.MoveNext())
                    {
                        var current = e.Current;

                        // -1 because 0 is always parent and we don't create that
                        var linkedEntity = linkedEntities[current.Key - 1];
                        Assert.AreNotEqual(linkedEntity, Entity.Null);

                        ecb.AddComponent(linkedEntity, current.Value);
                        // TODO how do we copy data?
                        ecb.RemoveComponent(entity, current.Value);
                    }

                    for (var index = 0; index < linkedEntities.Length; index++)
                    {
                        // If it wasn't used
                        if ((groupMask & (1 << (index + 1))) == 0)
                        {
                            var unusedEntity = linkedEntities[index];
                            ecb.DestroyEntity(unusedEntity);

                            RemoveFromLinkedEntityGroup(leg, unusedEntity);
                        }
                    }
                }
            }

            ecb.Playback(state.EntityManager);

            // Remove all the original components from the parents
            using var uniqueEnumerator = uniqueComponents.GetEnumerator();
            while (uniqueEnumerator.MoveNext())
            {
                state.EntityManager.RemoveComponent(virtualChunkQuery, uniqueEnumerator.Current);
            }

            state.EntityManager.AddComponent<ChunkParent>(virtualChunkQuery);
        }

        private static void RemoveFromLinkedEntityGroup(DynamicBuffer<LinkedEntityGroup> leg, Entity unusedEntity)
        {
            var entities = leg.AsNativeArray();

            for (var index = 0; index < entities.Length; index++)
            {
                if (entities[index].Value == unusedEntity)
                {
                    leg.RemoveAt(index);
                    break;
                }
            }
        }
    }
}
#endif
