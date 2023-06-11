// <copyright file="VirtualChunkMask.cs" company="BovineLabs">
//     Copyright (c) VirtualChunks. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks.Data
{
    using Unity.Entities;

    public struct VirtualChunkMask : ISharedComponentData
    {
        // All the types of child chunks, this ensures 2 matching archetypes with different links don't end up in same chunk
        public byte Mask;
    }
}
#endif
