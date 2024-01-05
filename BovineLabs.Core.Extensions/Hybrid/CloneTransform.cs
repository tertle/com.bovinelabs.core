// <copyright file="CloneTransform.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_HYBRID
namespace BovineLabs.Core.Hybrid
{
    using Unity.Entities;

    public struct CloneTransform : IComponentData
    {
        public Entity Value;
    }
}
#endif
