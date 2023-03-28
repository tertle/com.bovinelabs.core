// <copyright file="DefaultInputSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using Ray = Unity.Physics.Ray;

    [UpdateInGroup(typeof(InputSystemGroup))]
    public partial class DefaultInputSystem : SystemBase
    {
        private InputDefault input = null!;
        private InputCommon inputCommon;

        /// <inheritdoc />
        protected override void OnCreate()
        {
            this.RequireForUpdate<Camera>();
            this.RequireForUpdate<InputDefault>();
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            this.input = SystemAPI.ManagedAPI.GetSingleton<InputDefault>();
            this.input.Asset.Enable();
            this.input.CursorPosition.action.performed += this.OnCursorPositionPerformed;
        }

        protected override void OnStopRunning()
        {
            this.input.CursorPosition.action.performed -= this.OnCursorPositionPerformed;
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var camera = this.EntityManager.GetComponentObject<Camera>(SystemAPI.ManagedAPI.GetSingletonEntity<Camera>());

            this.inputCommon.ViewPoint = ((float3)camera.ScreenToViewportPoint((Vector2)this.inputCommon.ScreenPoint)).xy;
            this.inputCommon.InViewPort = new Rect(0, 0, 1, 1).Contains(this.inputCommon.ViewPoint);
            this.inputCommon.InputOverUI = EventSystem.current.IsPointerOverGameObject();

            // ScreenPointToRay fails if out of bounds so we clamp it.
            // // Won't be accurate but it's up to the user to determine if they want to use it or not with InViewPort
            var screenPointForRay = this.inputCommon.InViewPort
                ? this.inputCommon.ScreenPoint
                : math.clamp(this.inputCommon.ScreenPoint, float2.zero, this.inputCommon.ScreenPoint);

            var cameraRay = camera.ScreenPointToRay((Vector2)screenPointForRay);
            this.inputCommon.CameraRay = new Ray { Origin = cameraRay.origin, Displacement = cameraRay.direction };

            SystemAPI.SetSingleton(this.inputCommon);
        }

        private void OnCursorPositionPerformed(InputAction.CallbackContext context)
        {
            this.inputCommon.ScreenPoint = context.ReadValue<Vector2>();
        }
    }
}
#endif
