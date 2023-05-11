// <copyright file="ChunkCloneTransformFromLinkSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Transforms;

    [UpdateAfter(typeof(LocalToWorldSystem))]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public partial struct ChunkCloneTransformFromLinkSystem : ISystem
    {
        private EntityQuery query;

        public void OnCreate(ref SystemState state)
        {
            this.query = SystemAPI.QueryBuilder().WithAllRW<LocalToWorld>().WithAll<ChunkCloneTransformFromLink, ChunkChild>().Build();
            state.RequireForUpdate(this.query);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new CloneTransformJob
                {
                    LocalToWorldHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(),
                    LinkedChunkHandle = SystemAPI.GetSharedComponentTypeHandle<ChunkChild>(),
                    LastSystemVersion = state.LastSystemVersion,
                }
                .ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private unsafe struct CloneTransformJob : IJobChunk
        {
            public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

            [ReadOnly]
            public SharedComponentTypeHandle<ChunkChild> LinkedChunkHandle;

            public uint LastSystemVersion;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var linkedChunk = chunk.GetSharedComponent(this.LinkedChunkHandle).Parent;

                if (!linkedChunk.Has(ref this.LocalToWorldHandle) && !linkedChunk.DidChange(ref this.LocalToWorldHandle, this.LastSystemVersion))
                {
                    return;
                }

                var localToWorlds = chunk.GetComponentDataPtrRW(ref this.LocalToWorldHandle);
                var linkedLocalToWorlds = linkedChunk.GetComponentDataPtrRO(ref this.LocalToWorldHandle);
                UnsafeUtility.MemCpy(localToWorlds, linkedLocalToWorlds, chunk.Count * UnsafeUtility.SizeOf<LocalToWorld>());
            }
        }
    }
}
#endif
