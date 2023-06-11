// <copyright file="ObjectCategoryComponents.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    public struct ObjectCategoryComponents : IBufferElementData
    {
        public byte CategoryBit;
        public ulong StableTypeHash;
    }
}
#endif
