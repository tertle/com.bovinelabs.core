// <copyright file="SubSceneLoad.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Entities.Serialization;

    [InternalBufferCapacity(0)]
    public struct SubSceneLoad : IBufferElementData
    {
        public FixedString64Bytes Name;
        public EntitySceneReference Scene;
        public WorldFlags TargetWorld;
        public SubSceneLoadMode LoadingMode;
        public bool IsRequired;
        public float LoadMaxDistance;
        public float UnloadMaxDistance;
    }
}
#endif
