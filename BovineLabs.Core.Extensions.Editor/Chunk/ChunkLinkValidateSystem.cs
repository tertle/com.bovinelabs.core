// <copyright file="ChunkLinkValidateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Editor.Chunk
{
    using BovineLabs.Core.Chunks;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using UnityEngine;

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(ChunkWriteLinksSystem))]
    public partial struct ChunkLinkValidateSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<ChunkLinkedEntity, ChunkChild>().Build();

            state.Dependency = new ValidateLinkedChunkJob
                {
                    EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                    ChunkOwnerHandle = SystemAPI.GetComponentTypeHandle<ChunkLinkedEntity>(true),
                    LinkedChunkHandle = SystemAPI.GetSharedComponentTypeHandle<ChunkChild>(),
                }
                .ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private unsafe struct ValidateLinkedChunkJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle EntityTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<ChunkLinkedEntity> ChunkOwnerHandle;

            [ReadOnly]
            public SharedComponentTypeHandle<ChunkChild> LinkedChunkHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var linkedChunk = chunk.GetSharedComponent(this.LinkedChunkHandle);
                var chunkOwners = chunk.GetComponentDataPtrRO(ref this.ChunkOwnerHandle);

                var entityParents = linkedChunk.Parent.GetEntityDataPtrRO(this.EntityTypeHandle);

                Debug.Assert(chunk.Count == linkedChunk.Parent.Count, "Length mismatch");

                var result = UnsafeUtility.MemCmp(chunkOwners, entityParents, chunk.Count * UnsafeUtility.SizeOf<Entity>());
                if (result != 0)
                {
                    Debug.LogError("Out of order");
                }
            }
        }
    }
}
#endif
