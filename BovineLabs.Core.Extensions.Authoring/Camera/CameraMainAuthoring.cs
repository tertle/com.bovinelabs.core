// <copyright file="CameraMainAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Authoring.Camera
{
    using BovineLabs.Core.Camera;
    using Unity.Entities;
    using UnityEngine;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CameraFrustumAuthoring))]
    public class CameraMainAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CameraMainAuthoring>
        {
            public override void Bake(CameraMainAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.Dynamic);
                this.AddComponent<CameraMain>(entity);
            }
        }
    }
}
#endif
