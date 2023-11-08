// <copyright file="ChunkLinkedEntity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using Unity.Entities;

    /// <summary> The entity in the linked chunk. </summary>
    public struct ChunkLinkedEntity : IComponentData
    {
        // Must have no other fields
        public Entity Value;
    }
}
#endif
