// <copyright file="CameraFrustumCorners.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Camera
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CameraFrustumCorners : IComponentData
    {
        public float3x4 NearPlane;
        public float3x4 FarPlane;
    }
}
#endif
