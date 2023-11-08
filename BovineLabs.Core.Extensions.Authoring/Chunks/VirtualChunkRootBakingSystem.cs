// <copyright file="VirtualChunkRootBakingSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Authoring.Chunks
{
    using System;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Chunks;
    using BovineLabs.Core.Chunks.Data;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEditor;

    /// <summary> System responsible for moving the virtual chunk components from the owner entity into its virtual chunks. </summary>
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct VirtualChunkRootBakingSystem : ISystem
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
                .WithAll<VirtualChunkRootBaking, LinkedEntityGroup>()
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities).Build();

            var entityHandle = SystemAPI.GetEntityTypeHandle();
            var linkedEntityGroupHandle = SystemAPI.GetBufferTypeHandle<LinkedEntityGroup>();

            // -1 because 0 is always parent and we don't create that
            var linkedEntities = CollectionHelper.CreateNativeArray<Entity>(ChunkLinks.MaxGroupIDs - 1, state.WorldUpdateAllocator);
            var allComponentsUnique = new NativeHashSet<ComponentType>(128, state.WorldUpdateAllocator);
            var componentMapping = new NativeParallelMultiHashMap<byte, ComponentType>(128, state.WorldUpdateAllocator);
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            var chunks = virtualChunkQuery.ToArchetypeChunkArray(state.WorldUpdateAllocator);

            // TODO we could group this by unique archetype chunk to only process each archetype once
            foreach (var chunk in chunks)
            {
                componentMapping.Clear();
                GetComponentsFromChunk(chunk, allComponentsUnique, componentMapping);

                var groupMask = CalculateGroupMask(componentMapping);

                var entities = chunk.GetEntityDataPtrRO(entityHandle);
                var linkedEntityGroups = chunk.GetBufferAccessor(ref linkedEntityGroupHandle);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var rootEntity = entities[i];
                    var leg = linkedEntityGroups[i];
                    ecb.AddSharedComponent(rootEntity, new VirtualChunkMask { Mask = groupMask }); // TODO set per chunk?

                    FindVirtualChunks(ref state, leg, linkedEntities);
                    MoveComponents(ref state, ecb, componentMapping, linkedEntities, rootEntity);
                    DestroyUnusedChunks(ecb, leg, linkedEntities, groupMask);
                }
            }

            ecb.Playback(state.EntityManager);

            // Remove all the original components from the parents
            using var uniqueEnumerator = allComponentsUnique.GetEnumerator();
            while (uniqueEnumerator.MoveNext())
            {
                state.EntityManager.RemoveComponent(virtualChunkQuery, uniqueEnumerator.Current);
            }

            // state.EntityManager.AddComponent<VirtualChunkMask>(virtualChunkQuery);
        }

        private static void GetComponentsFromChunk(ArchetypeChunk chunk, NativeHashSet<ComponentType> uniqueComponents, NativeParallelMultiHashMap<byte, ComponentType> componentMapping)
        {
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
        }

        /// <summary> Calculates and returns a bit mask of all the used virtual chunks. </summary>
        /// <param name="componentMapping"> The map of groups to components. </param>
        /// <returns> The bit mask of used chunks. </returns>
        private static byte CalculateGroupMask(NativeParallelMultiHashMap<byte, ComponentType> componentMapping)
        {
            byte groupMask = 1 << 0; // we are always 0
            var keys = componentMapping.GetUniqueKeyArray(Allocator.Temp); // TODO re-use below

            for (var i = 0; i < keys.Item2; i++)
            {
                var groupID = keys.Item1[i];

                var bit = 1 << groupID;
                Check.Assume((groupMask & bit) == 0, $"Multiple links with the same groupID {groupID}");
                groupMask = (byte)(groupMask | bit);
            }

            return groupMask;
        }

        /// <summary> Finds all the virtual chunks from the <see cref="LinkedEntityGroup"/> and store them in the <see cref="linkedEntities"/>. </summary>
        private static void FindVirtualChunks(ref SystemState state, DynamicBuffer<LinkedEntityGroup> linkedEntityGroup, NativeArray<Entity> linkedEntities)
        {
            linkedEntities.Clear();

            foreach (var linkedEntity in linkedEntityGroup.AsNativeArray().Reinterpret<Entity>())
            {
                if (state.EntityManager.HasComponent<ChunkGroupID>(linkedEntity))
                {
                    var groupID = state.EntityManager.GetSharedComponent<ChunkGroupID>(linkedEntity).Value;
                    linkedEntities[groupID - 1] = linkedEntity;
                }
            }
        }

        private static void MoveComponents(
            ref SystemState state, EntityCommandBuffer ecb, NativeParallelMultiHashMap<byte, ComponentType> componentMapping, NativeArray<Entity> linkedEntities, Entity rootEntity)
        {
            // TODO do all adds at once
            // var chunks = componentMapping.GetUniqueKeyArray(Allocator.Temp);

            // for (var i = 0; i < chunks.Item2; i++)
            // {
            //     // -1 because 0 is always parent and we don't create that
            //     var chunkValue = chunks.Item1[i] - 1;
            //     var linkedEntity = linkedEntities[chunkValue];
            //
            //     Assert.AreNotEqual(linkedEntity, Entity.Null);
            //
            //     var components = new FixedList128Bytes<ComponentType>();
            //
            //     var components = new ComponentTypeSet();
            //     components.
            // }

            using var e = componentMapping.GetEnumerator();
            while (e.MoveNext())
            {
                var current = e.Current;

                // -1 because 0 is always parent and we don't create that
                var linkedEntity = linkedEntities[current.Key - 1];
                Assert.AreNotEqual(linkedEntity, Entity.Null);

                if (current.Value is { IsComponent: true, IsZeroSized: false })
                {
                    var source = state.EntityManager.GetComponentDataRaw(rootEntity, current.Value);
                    var size = TypeManager.GetTypeInfo(current.Value.TypeIndex).ElementSize;
                    ecb.UnsafeAddComponent(linkedEntity, current.Value.TypeIndex, size, source);
                }
                else if (current.Value.IsBuffer)
                {
                    var source = state.EntityManager.GetUntypedBuffer(rootEntity, current.Value, true);
                    var dest = ecb.AddUntypedBuffer(linkedEntity, current.Value);
                    dest.AddRange(source.GetUnsafeReadOnlyPtr(), source.Length);
                }
                else
                {
                    ecb.AddComponent(linkedEntity, current.Value);
                }
            }
        }

        private static void DestroyUnusedChunks(
            EntityCommandBuffer ecb, DynamicBuffer<LinkedEntityGroup> leg, NativeArray<Entity> linkedEntities, byte groupMask)
        {
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

        private static void RemoveFromLinkedEntityGroup(DynamicBuffer<LinkedEntityGroup> linkedEntityGroup, Entity unusedEntity)
        {
            var entities = linkedEntityGroup.AsNativeArray();

            for (var index = 0; index < entities.Length; index++)
            {
                if (entities[index].Value == unusedEntity)
                {
                    linkedEntityGroup.RemoveAt(index);
                    break;
                }
            }
        }
    }
}
#endif
