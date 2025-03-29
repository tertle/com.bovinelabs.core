// <copyright file="SubSceneBuffer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Entities.Serialization;

    [InternalBufferCapacity(16)]
    public struct SubSceneBuffer : IBufferElementData
    {
        public FixedString64Bytes Name;
        public EntitySceneReference Scene;
    }
}
#endif
