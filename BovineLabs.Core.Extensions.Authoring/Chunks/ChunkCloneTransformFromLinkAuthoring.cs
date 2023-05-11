// <copyright file="ChunkCloneTransformFromLinkAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Authoring.Chunks
{
    using BovineLabs.Core.Chunks;
    using Unity.Entities;
    using UnityEngine;

    public class ChunkCloneTransformFromLinkAuthoring : MonoBehaviour
    {
        public class Baker : Baker<ChunkCloneTransformFromLinkAuthoring>
        {
            public override void Bake(ChunkCloneTransformFromLinkAuthoring authoring)
            {
                this.AddComponent<ChunkCloneTransformFromLink>(this.GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.WorldSpace));
            }
        }
    }
}
#endif
