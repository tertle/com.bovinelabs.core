// <copyright file="LoadWithBoundingVolumeConfig.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;

    public struct LoadWithBoundingVolumeConfig : IComponentData
    {
        public float LoadMaxDistance;
        public float UnloadMaxDistance;
    }
}
