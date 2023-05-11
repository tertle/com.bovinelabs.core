// <copyright file="ObjectCategoryComponents.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.AssetManagement
{
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    public struct ObjectCategoryComponents : IBufferElementData
    {
        public byte CategoryBit;
        public ulong StableTypeHash;
    }
}
