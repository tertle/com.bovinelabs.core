// <copyright file="AssetLoad.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;
    using UnityEngine;

    [InternalBufferCapacity(0)]
    public struct AssetLoad : IBufferElementData
    {
        public WorldFlags TargetWorld;
        public UnityObjectRef<GameObject> Asset;
    }
}
#endif
