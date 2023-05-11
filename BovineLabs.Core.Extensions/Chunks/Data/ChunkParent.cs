// <copyright file="ChunkParent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using Unity.Entities;

    public struct ChunkParent : ISharedComponentData
    {
        // All the types of child chunks, this ensures 2 matching archetypes with different links don't end up in same chunk
        public byte ChildMask;
    }
}
#endif
