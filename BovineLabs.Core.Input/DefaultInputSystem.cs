// <copyright file="DefaultInputSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Camera;
    using BovineLabs.Core.Groups;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.Utilities;
    using Object = UnityEngine.Object;

    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateAfter(typeof(CameraSystemGroup))]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
    public partial class DefaultInputSystem : SystemBase
    {
        private InputCommon inputCommon;

        private IDisposable anyButtonPress;

        private OnApplicationFocusBehaviour focus = null!;

        /// <inheritdoc />
        protected override void OnCreate()
        {
            this.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<CameraMain, Camera>().Build());
            this.EntityManager.CreateEntity(typeof(InputCommon));

            // Start out of the screen until actual input occurs
            // This stops being moused out and thinking you are near an edge in game view
            this.inputCommon.CursorScreenPoint = new float2(-1, -1);

            this.focus = new GameObject("Focus") { hideFlags = HideFlags.HideAndDontSave }.AddComponent<OnApplicationFocusBehaviour>();
            Object.DontDestroyOnLoad(this.focus.gameObject);
        }

        protected override void OnDestroy()
        {
            Object.Destroy(this.focus.gameObject);
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            var input = InputCommonSettings.I;
            if (input.CursorPosition != null)
            {
                input.CursorPosition.action.performed += this.OnCursorPositionPerformed;
            }
            else
            {
                SystemAPI.GetSingleton<BLLogger>().LogError("Input CursorPosition not setup");
            }

            this.anyButtonPress = InputSystem.onAnyButtonPress.Call(this.OnButtonPressed);
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            if (InputCommonSettings.I.CursorPosition != null)
            {
                InputCommonSettings.I.CursorPosition.action.performed -= this.OnCursorPositionPerformed;
            }

            this.anyButtonPress!.Dispose();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var camera = this.EntityManager.GetComponentObject<Camera>(SystemAPI.GetSingletonEntity<CameraMain>());

            this.inputCommon.ScreenSize = new int2(Screen.width, Screen.height);
            this.inputCommon.CursorViewPoint = this.inputCommon.CursorScreenPoint / this.inputCommon.ScreenSize;
            this.inputCommon.CursorInViewPort = InViewPort(this.inputCommon.CursorViewPoint);

            this.inputCommon.CursorCameraViewPoint = ((float3)camera.ScreenToViewportPoint((Vector2)this.inputCommon.CursorScreenPoint)).xy;
            this.inputCommon.CursorInCameraViewPort = InViewPort(this.inputCommon.CursorCameraViewPoint);

            this.inputCommon.InputOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            this.inputCommon.ApplicationFocus = this.focus.Value;

            if (math.all(this.inputCommon.CursorCameraViewPoint))
            {
                // ScreenPointToRay fails if out of bounds so we clamp it.
                // Won't be accurate, but it's up to the user to determine if they want to use it or not with InViewPort
                var screenPointForRay = this.inputCommon.CursorInCameraViewPort
                    ? this.inputCommon.CursorScreenPoint
                    : math.clamp(this.inputCommon.CursorScreenPoint, float2.zero, this.inputCommon.CursorScreenPoint);

                var cameraRay = camera.ScreenPointToRay((Vector2)screenPointForRay);
                this.inputCommon.CameraRay = cameraRay;
            }
            else
            {
                this.inputCommon.CameraRay = default;
            }

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
            this.inputCommon.CursorScreenPoint = context.ReadValue<Vector2>();
        }

        private void OnButtonPressed(InputControl button)
        {
            if (this.inputCommon.CursorInViewPort)
            {
                this.inputCommon.AnyButtonPress = true;
            }
        }
    }
}
