// <copyright file="ChunkOwnerAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Authoring.Chunks
{
    using BovineLabs.Core.Chunks;
    using Unity.Entities;
    using Unity.Entities.Hybrid.Baking;
    using UnityEngine;

    [TemporaryBakingType]
    public struct ChunkOwnerBaking : IComponentData
    {
    }

    [TemporaryBakingType]
    public struct GroupIDBaking : IComponentData
    {
    }

    [RequireComponent(typeof(LinkedEntityGroupAuthoring))]
    public class VirtualChunkAuthoring : MonoBehaviour
    {
        public class Baker : Baker<VirtualChunkAuthoring>
        {
            public override void Bake(VirtualChunkAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                this.AddComponent<ChunkOwnerBaking>(entity);

                for (var i = 0; i < ChunkLinks.MaxGroupIDs - 1; i++)
                {
                    var groupEntity = this.CreateAdditionalEntity(TransformUsageFlags.None);

                    this.AddComponent(groupEntity, new ChunkLinkedEntity { Value = entity });
                    this.AddSharedComponent(groupEntity, default(ChunkChild));
                    this.AddSharedComponent(groupEntity, new ChunkGroupID { Value = (byte)(i + 1) });
                }

                // var chunkLinks = authoring.GetComponentsInChildren<ChunkLinkAuthoring>();
                //
                // byte groupMask = 1 << 0; // we are always 0
                // foreach (var link in chunkLinks)
                // {
                //     var bit = 1 << link.GroupID;
                //     if ((groupMask & bit) != 0)
                //     {
                //         Debug.LogError($"Multiple links with the same groupID {link.GroupID}");
                //     }
                //
                //     groupMask = (byte)(groupMask | bit);
                // }
                //
                // this.AddSharedComponent(this.GetEntity(TransformUsageFlags.None), new ChunkParent { ChildMask = groupMask });
            }
        }
    }
}
#endif
