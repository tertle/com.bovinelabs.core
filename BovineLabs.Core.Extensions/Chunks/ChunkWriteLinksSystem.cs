// <copyright file="ChunkWriteLinksSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Chunks.Data;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary> Writes the linked chunks to the parent. </summary>
    [UpdateAfter(typeof(ChunkLinkOrderSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct ChunkWriteLinksSystem : ISystem
    {
        private EntityQuery changedQuery;
        private SharedComponentLookup<ChunkGroupID> chunkGroupIDs;

        public void OnCreate(ref SystemState state)
        {
            this.chunkGroupIDs = state.GetSharedComponentLookup<ChunkGroupID>();

            this.changedQuery = SystemAPI.QueryBuilder().WithAll<VirtualChunkMask, LinkedEntityGroup>().WithAllChunkComponentRW<ChunkLinks>().Build();
            this.changedQuery.SetOrderVersionFilter();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var missingLinksQuery = SystemAPI.QueryBuilder().WithAny<VirtualChunkMask, ChunkChild>().WithNoneChunkComponent<ChunkLinks>().Build();
            state.EntityManager.AddChunkComponentData(missingLinksQuery, default(ChunkLinks));

            if (this.changedQuery.IsEmpty)
            {
                return;
            }

            this.chunkGroupIDs.Update(ref state);

            state.Dependency = new UpdateChunkLinksJob
                {
                    LinkedEntityGroupHandle = SystemAPI.GetBufferTypeHandle<LinkedEntityGroup>(true),
                    ChunkLinksHandle = SystemAPI.GetComponentTypeHandle<ChunkLinks>(),
                    ChunkGroupIDs = this.chunkGroupIDs,
                    EntityStorageInfos = SystemAPI.GetEntityStorageInfoLookup(),
                }
                .Schedule(this.changedQuery, state.Dependency);
        }

        [BurstCompile]
        private struct UpdateChunkLinksJob : IJobChunk
        {
            [ReadOnly]
            public BufferTypeHandle<LinkedEntityGroup> LinkedEntityGroupHandle;

            public ComponentTypeHandle<ChunkLinks> ChunkLinksHandle;

            [ReadOnly]
            public SharedComponentLookup<ChunkGroupID> ChunkGroupIDs;

            public EntityStorageInfoLookup EntityStorageInfos;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var linkedEntityGroupAccessor = chunk.GetBufferAccessor(ref this.LinkedEntityGroupHandle);
                var linkedEntityGroup = linkedEntityGroupAccessor[0].AsNativeArray().Reinterpret<Entity>();

                var chunkLinks = default(ChunkLinks);
                chunkLinks[0] = chunk; // Parent is always 0

                var linkedChunks = default(FixedList128Bytes<ArchetypeChunk>);
                Check.Assume(linkedChunks.Capacity >= ChunkLinks.MaxGroupIDs - 1);

                foreach (var entity in linkedEntityGroup)
                {
                    if (!this.ChunkGroupIDs.TryGetComponent(entity, out var chunkGroup))
                    {
                        continue;
                    }

                    Debug.Assert(chunkLinks[chunkGroup.Value] == default, "Duplicate ChunkGroupID values");

                    var chunkLink = this.EntityStorageInfos[entity].Chunk;
                    chunkLinks[chunkGroup.Value] = chunkLink;

                    linkedChunks.Add(chunkLink);
                }

                chunk.SetChunkComponentData(ref this.ChunkLinksHandle, chunkLinks);
                foreach (var chunkLink in linkedChunks)
                {
                    chunkLink.SetChunkComponentData(ref this.ChunkLinksHandle, chunkLinks);
                }
            }
        }
    }
}
#endif
