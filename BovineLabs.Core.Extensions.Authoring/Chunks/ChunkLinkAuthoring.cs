// // <copyright file="ChunkLinkAuthoring.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// #if !BL_DISABLE_LINKED_CHUNKS
// namespace BovineLabs.Core.Authoring.Chunks
// {
//     using BovineLabs.Core.Chunks;
//     using Unity.Entities;
//     using UnityEngine;
//
//     public class ChunkLinkAuthoring : MonoBehaviour
//     {
//         [Range(1, ChunkLinks.MaxGroupIDs - 1)]
//         public byte GroupID = 1;
//
//         public class Baker : Baker<ChunkLinkAuthoring>
//         {
//             public override void Bake(ChunkLinkAuthoring authoring)
//             {
//                 var entity = this.GetEntity(TransformUsageFlags.None);
//                 this.AddComponent(entity, default(ChunkLinkedEntity)); // TODO we can probably set parent here instead of ChunkOwnerBakingSystem?
//
//                 this.AddSharedComponent(entity, default(ChunkChild));
//                 this.AddSharedComponent(entity, new ChunkGroupID { Value = authoring.GroupID });
//             }
//         }
//     }
// }
// #endif
