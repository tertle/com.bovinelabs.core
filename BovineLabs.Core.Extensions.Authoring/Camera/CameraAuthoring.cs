// <copyright file="CameraAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Authoring.Camera
{
    using BovineLabs.Core.Camera;
    using Unity.Entities;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class CameraAuthoring : MonoBehaviour
    {
        [SerializeField]
        private bool isMainCamera = true;

        private class Baker : Baker<CameraAuthoring>
        {
            public override void Bake(CameraAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.Dynamic);
                if (authoring.isMainCamera)
                {
                    this.AddComponent<CameraMain>(entity);
                }

                this.AddComponent<CameraFrustumPlanes>(entity);
                this.AddComponent<CameraFrustumCorners>(entity);
            }
        }
    }
}
#endif
