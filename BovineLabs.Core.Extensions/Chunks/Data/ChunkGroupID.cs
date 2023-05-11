// <copyright file="ChunkGroupID.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using Unity.Entities;

    public struct ChunkGroupID : ISharedComponentData
    {
        public byte Value;
    }
}
#endif
