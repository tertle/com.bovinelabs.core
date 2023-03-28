// <copyright file="CopyTransformToGameObject.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_COPY_TRANSFORM
namespace BovineLabs.Core.Hybrid
{
    using Unity.Entities;

    public struct CopyTransformToGameObject : IComponentData
    {
    }
}
#endif
