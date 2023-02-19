// <copyright file="LoadWithBoundingVolume.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct LoadWithBoundingVolume : IComponentData
    {
        public AABB Bounds;
        public float LoadMaxDistanceOverrideSq;
        public float UnloadMaxDistanceOverrideSq;
    }
}
