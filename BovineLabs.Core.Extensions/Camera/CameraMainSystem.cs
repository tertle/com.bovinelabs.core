// <copyright file="CameraMainSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Camera
{
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine;

    [UpdateBefore(typeof(CameraFrustumSystem))]
    [UpdateInGroup(typeof(CameraSystemGroup))]
    public partial class CameraMainSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var cameraQuery = SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<CameraMain, Camera>().Build();

            Entity entity;

            if (cameraQuery.IsEmptyIgnoreFilter)
            {
                var noCameraQuery = SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<CameraMain>().WithNone<Camera>().Build();

                if (noCameraQuery.IsEmptyIgnoreFilter)
                {
                    // User hasn't setup an entity, create our own
                    entity = this.EntityManager.CreateEntity(typeof(CameraMain), typeof(LocalTransform), typeof(CameraFrustumPlanes),
                        typeof(CameraFrustumCorners), typeof(Camera));
                }
                else
                {
                    entity = noCameraQuery.GetSingletonEntity();
                }

                var cam = Camera.main;
                if (cam == null)
                {
                    SystemAPI.GetSingleton<BLLogger>().LogError("No main camera found");
                    return;
                }

                this.EntityManager.AddComponentObject(entity, cam);
            }
            else
            {
                entity = cameraQuery.GetSingletonEntity();
            }

            var camera = cameraQuery.GetSingleton<Camera>();
            var tr = camera.transform;
            this.EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(tr.position, tr.rotation));
        }
    }
}
#endif
