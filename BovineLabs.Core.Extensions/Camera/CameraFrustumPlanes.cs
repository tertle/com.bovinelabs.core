// <copyright file="CameraFrustumPlanes.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Camera
{
    using Unity.Entities;
    using Unity.Mathematics;

    [InternalBufferCapacity(6)]
    public struct CameraFrustumPlanes : IBufferElementData
    {
        public float4 Value;
    }
}
#endif
