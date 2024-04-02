// <copyright file="CameraFrustumCorners.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Camera
{
    using Unity.Entities;
    using Unity.Mathematics;

    // First 4 near plane, second 4 far plane
    [InternalBufferCapacity(8)]
    public struct CameraFrustumCorners : IBufferElementData
    {
        public float3 Value;
    }
}
#endif
