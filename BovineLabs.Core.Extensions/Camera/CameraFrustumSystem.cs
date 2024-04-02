// <copyright file="CameraFrustumSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Camera
{
    using BovineLabs.Core.Groups;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    [UpdateAfter(typeof(CameraMainSystem))]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    public partial class CameraFrustumSystem : SystemBase
    {
        private readonly Plane[] sourcePlanes = new Plane[6];
        private readonly Vector3[] frustumCornerArray = new Vector3[4];

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            foreach (var (frustumPlanes, frustumCorners, cameraWrapper) in SystemAPI.Query<
                         DynamicBuffer<CameraFrustumPlanes>, DynamicBuffer<CameraFrustumCorners>, SystemAPI.ManagedAPI.UnityEngineComponent<Camera>>())
            {
                var camera = cameraWrapper.Value;

                GeometryUtility.CalculateFrustumPlanes(camera.projectionMatrix * camera.worldToCameraMatrix, this.sourcePlanes);

                // Copied from FrustumPlanes but avoiding array allocation each frame
                var cameraToWorld = camera.cameraToWorldMatrix;
                var eyePos = cameraToWorld.MultiplyPoint(Vector3.zero);
                var viewDir = new float3(cameraToWorld.m02, cameraToWorld.m12, cameraToWorld.m22);
                viewDir = -math.normalizesafe(viewDir);

                // Near Plane
                this.sourcePlanes[4].SetNormalAndPosition(viewDir, eyePos);
                this.sourcePlanes[4].distance -= camera.nearClipPlane;

                // Far plane
                this.sourcePlanes[5].SetNormalAndPosition(-viewDir, eyePos);
                this.sourcePlanes[5].distance += camera.farClipPlane;

                for (var i = 0; i < 6; ++i)
                {
                    frustumPlanes.ElementAt(i) = new CameraFrustumPlanes { Value = new float4(this.sourcePlanes[i].normal, this.sourcePlanes[i].distance) };
                }

                camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, this.frustumCornerArray);
                for (var i = 0; i < 4; i++)
                {
                    frustumCorners.ElementAt(i) = new CameraFrustumCorners { Value = camera.transform.TransformPoint(this.frustumCornerArray[i]) };
                }

                camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, this.frustumCornerArray);
                for (var i = 0; i < 4; i++)
                {
                    frustumCorners.ElementAt(i + 4) = new CameraFrustumCorners { Value = camera.transform.TransformPoint(this.frustumCornerArray[i]) };
                }
            }
        }
    }
}
#endif
