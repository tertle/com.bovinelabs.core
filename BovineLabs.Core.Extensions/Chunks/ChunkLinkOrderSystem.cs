// <copyright file="ChunkLinkOrderSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using BovineLabs.Core.Chunks.Data;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> Enforces the correct order of linked chunks as well as setting the correct chunks in ChunkLinks. </summary>
    /// <remarks>
    /// If it detects child entities out of order it sets their ChunkChild shared component to default then goes through 1 at a time
    /// setting each entity in order to the correct shared component.
    /// This is obviously costly and effort should be made to avoid order breaking structural changes in the parent.
    /// </remarks>
    [UpdateAfter(typeof(ChunkLinkSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct ChunkLinkOrderSystem : ISystem
    {
        private EntityQuery changedQuery;
        private NativeList<Entity> entityList;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.entityList = new NativeList<Entity>(Allocator.Persistent);

            this.changedQuery = SystemAPI.QueryBuilder().WithAll<VirtualChunkMask, LinkedEntityGroup>().Build();
            this.changedQuery.SetOrderVersionFilter();
        }

        public void OnDestroy(ref SystemState state)
        {
            this.entityList.Dispose();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public unsafe void OnUpdate(ref SystemState state)
        {
            if (this.changedQuery.IsEmpty)
            {
                return;
            }

            var entityTypeHandle = SystemAPI.GetEntityTypeHandle();
            var linkedEntityGroupHandle = SystemAPI.GetBufferTypeHandle<LinkedEntityGroup>(true);
            var chunkOwnerHandle = SystemAPI.GetComponentTypeHandle<ChunkLinkedEntity>(true);
            var entityStorageInfoLookup = SystemAPI.GetEntityStorageInfoLookup();

            var chunks = this.changedQuery.ToArchetypeChunkArray(Allocator.Temp);
            foreach (var chunk in chunks)
            {
                // Structural changes invalidate this
                entityTypeHandle.Update(ref state);
                linkedEntityGroupHandle.Update(ref state);
                chunkOwnerHandle.Update(ref state);
                entityStorageInfoLookup.Update(ref state);

                var entities = chunk.GetEntityDataPtrRO(entityTypeHandle);
                var linkedEntityGroupAccessor = chunk.GetBufferAccessor(ref linkedEntityGroupHandle);

                var outOfOrder = CheckEntitiesOutOfOrder(ref state, chunk, ref UnsafeUtility.AsRef<Entity>(entities), linkedEntityGroupAccessor, entityStorageInfoLookup, ref chunkOwnerHandle);

                // We need to fix ordering
                if (outOfOrder)
                {
                    this.FixedOutOfOrder(chunk, ref state, ref linkedEntityGroupHandle);
                }
            }
        }

        private static unsafe bool CheckEntitiesOutOfOrder(
            ref SystemState state,
            ArchetypeChunk chunk,
            ref Entity entities,
            BufferAccessor<LinkedEntityGroup> linkedEntityGroupAccessor,
            EntityStorageInfoLookup entityStorageInfoLookup,
            ref ComponentTypeHandle<ChunkLinkedEntity> chunkOwnerHandle)
        {
            // We only need to look at the first entity as all linked entities will be in the correct chunk, but potentially out of order
            var linkedEntityGroup = linkedEntityGroupAccessor[0].AsNativeArray().Reinterpret<Entity>();
            foreach (var entity in linkedEntityGroup)
            {
                if (!state.EntityManager.HasComponent<ChunkGroupID>(entity))
                {
                    continue;
                }

                var linkedChunk = entityStorageInfoLookup[entity].Chunk;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (chunk.Count != linkedChunk.Count)
                {
                    // Debug.LogError("Length mismatch. You probably have a child chunk with less capacity than the parent chunk.");
                    return true;
                }
#endif

                var chunkLinkedEntities = linkedChunk.GetComponentDataPtrRO(ref chunkOwnerHandle);

                var result = UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref entities), chunkLinkedEntities, chunk.Count * UnsafeUtility.SizeOf<Entity>());
                if (result != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void FixedOutOfOrder(ArchetypeChunk chunk, ref SystemState state, ref BufferTypeHandle<LinkedEntityGroup> linkedEntityGroupHandle)
        {
            var linkedChunk = new ChunkChild { Parent = chunk };

            // Invalidate the old shared component
            var removeQuery = SystemAPI.QueryBuilder().WithAll<ChunkChild>().Build();
            removeQuery.SetSharedComponentFilter(linkedChunk);
            state.EntityManager.SetSharedComponent(removeQuery, default(ChunkChild));

            // Structural changes invalidated this
            linkedEntityGroupHandle.Update(ref state);
            var linkedEntityGroupAccessor = chunk.GetBufferAccessor(ref linkedEntityGroupHandle);

            this.entityList.Clear();

            // Now we re-build all our chunks in the correct order
            for (var i = 0; i < linkedEntityGroupAccessor.Length; i++)
            {
                foreach (var entity in linkedEntityGroupAccessor[i].AsNativeArray().Reinterpret<Entity>())
                {
                    if (!state.EntityManager.HasComponent<ChunkChild>(entity))
                    {
                        continue;
                    }

                    this.entityList.Add(entity);
                }
            }

            state.EntityManager.SetSharedComponent(this.entityList.AsArray(), linkedChunk);
        }
    }
}
#endif
