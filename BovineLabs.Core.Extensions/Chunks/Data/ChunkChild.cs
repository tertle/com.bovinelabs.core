// <copyright file="ChunkChild.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using Unity.Entities;

    [ChunkSerializable]
    public struct ChunkChild : ISharedComponentData
    {
        public ArchetypeChunk Parent;
    }
}
#endif
