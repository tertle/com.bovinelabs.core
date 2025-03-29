// <copyright file="SubSceneEntity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;

    [InternalBufferCapacity(16)]
    public struct SubSceneEntity : IBufferElementData, IEnableableComponent
    {
        public Entity Entity;
        public Hash128 Scene;
    }
}
#endif
