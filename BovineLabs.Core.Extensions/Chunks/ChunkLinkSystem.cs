// <copyright file="ChunkLinkSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using BovineLabs.Core.Chunks.Data;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
#if UNITY_NETCODE
    using Unity.NetCode;
#endif
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary> Iterates parent chunks on OrderVersion change and updates any linked chunk. </summary>
#if UNITY_NETCODE
    [UpdateAfter(typeof(NetworkReceiveSystemGroup))]
#endif
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct ChunkLinkSystem : ISystem
    {
        private EntityQuery changedQuery;
        private SharedComponentLookup<ChunkChild> linkedChunks;

        public void OnCreate(ref SystemState state)
        {
            this.linkedChunks = state.GetSharedComponentLookup<ChunkChild>();

            this.changedQuery = SystemAPI.QueryBuilder().WithAll<VirtualChunkMask, LinkedEntityGroup>().Build();
            this.changedQuery.SetOrderVersionFilter();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.linkedChunks.Update(ref state);

            // var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            new CalculateChunkJob
                {
                    LinkedEntityGroupHandle = SystemAPI.GetBufferTypeHandle<LinkedEntityGroup>(true),
                    LinkedChunks = this.linkedChunks,
                    CommandBuffer = ecb.AsParallelWriter(),
                }
                .Run(this.changedQuery); // TODO parallel

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        private struct CalculateChunkJob : IJobChunk
        {
            [ReadOnly]
            public BufferTypeHandle<LinkedEntityGroup> LinkedEntityGroupHandle;

            [ReadOnly]
            public SharedComponentLookup<ChunkChild> LinkedChunks;

            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var linkedEntityGroupAccessor = chunk.GetBufferAccessor(ref this.LinkedEntityGroupHandle);

                for (var index = 0; index < linkedEntityGroupAccessor.Length; index++)
                {
                    var linkedEntityGroup = linkedEntityGroupAccessor[index].AsNativeArray().Reinterpret<Entity>();

                    foreach (var entity in linkedEntityGroup)
                    {
                        if (!this.LinkedChunks.TryGetComponent(entity, out var linkedChunk))
                        {
                            continue;
                        }

                        if (linkedChunk.Parent.Equals(chunk))
                        {
                            continue;
                        }

                        linkedChunk.Parent = chunk;
                        this.CommandBuffer.SetSharedComponent(unfilteredChunkIndex, entity, linkedChunk);
                    }
                }
            }
        }
    }
}
#endif
