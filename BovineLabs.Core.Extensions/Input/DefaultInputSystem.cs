// <copyright file="DefaultInputSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using System;
    using System.Runtime.CompilerServices;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.Utilities;
    using Object = UnityEngine.Object;
    using Ray = Unity.Physics.Ray;

    [UpdateInGroup(typeof(InputSystemGroup))]
    public partial class DefaultInputSystem : SystemBase
    {
        private InputDefault input;
        private InputCommon inputCommon;

        private IDisposable? anyButtonPress;

        private OnApplicationFocusBehaviour focus = null!;

        /// <inheritdoc />
        protected override void OnCreate()
        {
            this.RequireForUpdate<Camera>();
            this.RequireForUpdate<InputDefault>();

            // Start out of the screen until actual input occurs
            // This stops being moused out and thinking you are near an edge in game view
            this.inputCommon.CameraScreenPoint = new float2(-1, -1);

            this.focus = new GameObject("Focus") {hideFlags = HideFlags.HideAndDontSave}.AddComponent<OnApplicationFocusBehaviour>();
            Object.DontDestroyOnLoad(this.focus.gameObject);
        }

        protected override void OnDestroy()
        {
            Object.Destroy(this.focus.gameObject);
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            this.input = SystemAPI.GetSingleton<InputDefault>();
            this.input.CursorPosition.Value.action.performed += this.OnCursorPositionPerformed;
            this.anyButtonPress = InputSystem.onAnyButtonPress.Call(this.OnButtonPressed);
        }

        protected override void OnStopRunning()
        {
            this.input.CursorPosition.Value.action.performed -= this.OnCursorPositionPerformed;
            this.anyButtonPress!.Dispose();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var camera = this.EntityManager.GetComponentObject<Camera>(SystemAPI.ManagedAPI.GetSingletonEntity<Camera>());

            this.inputCommon.ScreenSize = new int2(Screen.width, Screen.height);
            this.inputCommon.ViewPoint = this.inputCommon.CameraScreenPoint / this.inputCommon.ScreenSize;
            this.inputCommon.InViewPort = InViewPort(this.inputCommon.ViewPoint);

            this.inputCommon.CameraViewPoint = ((float3)camera.ScreenToViewportPoint((Vector2)this.inputCommon.CameraScreenPoint)).xy;
            this.inputCommon.InCameraViewPort = InViewPort(this.inputCommon.CameraViewPoint);

            this.inputCommon.InputOverUI = EventSystem.current.IsPointerOverGameObject();

            this.inputCommon.ApplicationFocus = this.focus.Value;

            // ScreenPointToRay fails if out of bounds so we clamp it.
            // Won't be accurate, but it's up to the user to determine if they want to use it or not with InViewPort
            var screenPointForRay = this.inputCommon.InCameraViewPort
                ? this.inputCommon.CameraScreenPoint
                : math.clamp(this.inputCommon.CameraScreenPoint, float2.zero, this.inputCommon.CameraScreenPoint);

            var cameraRay = camera.ScreenPointToRay((Vector2)screenPointForRay);
            this.inputCommon.CameraRay = new Ray { Origin = cameraRay.origin, Displacement = cameraRay.direction };

            SystemAPI.SetSingleton(this.inputCommon);

            this.inputCommon.AnyButtonPress = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool InViewPort(float2 point)
        {
            const float eps = 0.0001f;
            return point.x is >= -eps and <= 1 + eps && point.y is >= -eps and <= 1 + eps;
        }

        private void OnCursorPositionPerformed(InputAction.CallbackContext context)
        {
            this.inputCommon.CameraScreenPoint = context.ReadValue<Vector2>();
        }

        private void OnButtonPressed(InputControl button)
        {
            if (this.inputCommon.InViewPort)
            {
                this.inputCommon.AnyButtonPress = true;
            }
        }
    }
}
#endif
