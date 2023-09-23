// <copyright file="VirtualChunkDataUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks.Iterators
{
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    internal static unsafe class VirtualChunkDataUtility
    {
        // ArchetypeType.GetChunkComponentData
        public static ArchetypeChunk GetChunk(ref ArchetypeChunk chunk, int groupIndex, TypeIndex chunkLinksTypeIndex, ref LookupCache chunkLinksLookupCache)
        {
            var ptr = chunk.m_EntityComponentStore->GetOptionalComponentDataWithTypeRO(
                chunk.m_Chunk.MetaChunkEntity, chunkLinksTypeIndex, ref chunkLinksLookupCache);

            if (Hint.Unlikely(ptr == null))
            {
                return chunk;
            }

            ref var chunkLinks = ref UnsafeUtility.AsRef<ChunkLinks>(ptr);
            return chunkLinks[groupIndex];
        }

        public static ChunkIndex GetChunk(EntityComponentStore* entityComponentStore, ChunkIndex chunk, int groupIndex, TypeIndex chunkLinksTypeIndex, ref LookupCache chunkLinksLookupCache)
        {
            var ptr = entityComponentStore->GetOptionalComponentDataWithTypeRO(chunk.MetaChunkEntity, chunkLinksTypeIndex, ref chunkLinksLookupCache);

            if (Hint.Unlikely(ptr == null))
            {
                return chunk;
            }

            ref var chunkLinks = ref UnsafeUtility.AsRef<ChunkLinks>(ptr);
            return chunkLinks[groupIndex].m_Chunk;
        }
    }
}
#endif
