// <copyright file="EntityBlobBakedData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Blobs
{
    using JetBrains.Annotations;
    using Unity.Entities;

    [BakingType]
    public struct EntityBlobBakedData : IComponentData
    {
        [UsedImplicitly]
        public Entity Target;

        public int Key;
        public BlobAssetReference<byte> Blob;
    }
}
