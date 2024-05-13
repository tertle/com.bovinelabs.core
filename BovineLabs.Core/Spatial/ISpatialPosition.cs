// <copyright file="ISpatialPosition.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Spatial
{
    using Unity.Mathematics;

    public interface ISpatialPosition
    {
        float2 Position { get; }
    }

    public interface ISpatialPosition3
    {
        float3 Position { get; }
    }
}
