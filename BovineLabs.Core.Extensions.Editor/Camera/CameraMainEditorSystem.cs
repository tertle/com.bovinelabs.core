// <copyright file="CameraMainEditorSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Editor.Camera
{
    using BovineLabs.Core.Camera;
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEditor;
    using Camera = UnityEngine.Camera;

    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(CameraSystemGroup))]
    public partial class CameraMainEditorSystem : SystemBase
    {
        private EntityArchetype archetype;
        private Entity localCameraEntity;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.archetype = this.EntityManager.CreateArchetype(typeof(CameraMain), typeof(LocalTransform), typeof(Camera));
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var cameraQuery = SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<CameraMain>().Build();

            var cameras = cameraQuery.CalculateEntityCount();
            if (cameras == 0)
            {
                this.localCameraEntity = this.EntityManager.CreateEntity(this.archetype);
            }
            else if (cameras > 1)
            {
                if (this.localCameraEntity != Entity.Null)
                {
                    this.EntityManager.DestroyEntity(this.localCameraEntity);
                    this.localCameraEntity = Entity.Null;
                }
            }

            // check again after potential changes
            cameras = cameraQuery.CalculateEntityCount();

            if (cameras != 1)
            {
                // Use has more than 1 camera in scene
                return;
            }

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return;
            }

            var camera = sceneView.camera;
            if (camera == null)
            {
                return;
            }

            var entity = cameraQuery.GetSingletonEntity();

            this.EntityManager.AddComponentObject(entity, camera);
            var tr = camera.transform;
            this.EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(tr.position, tr.rotation));
        }
    }
}
#endif