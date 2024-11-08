// <copyright file="LoadWithBoundingVolume.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct LoadWithBoundingVolume : IComponentData
    {
        public AABB Bounds;
        public float LoadMaxDistanceSq;
        public float UnloadMaxDistanceSq;
    }
}
#endif
