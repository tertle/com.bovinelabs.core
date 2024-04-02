// <copyright file="CameraFrustumAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Authoring.Camera
{
    using BovineLabs.Core.Camera;
    using BovineLabs.Core.Extensions;
    using Unity.Entities;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class CameraFrustumAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CameraFrustumAuthoring>
        {
            public override void Bake(CameraFrustumAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.Dynamic);
                this.AddBuffer<CameraFrustumPlanes>(entity).ResizeInitialized(6);
                this.AddBuffer<CameraFrustumCorners>(entity).ResizeInitialized(8);
            }
        }
    }
}
#endif
