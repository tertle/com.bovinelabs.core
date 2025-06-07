// <copyright file="InputCommon.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input
{
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
#if UNITY_PHYSICS
    using Ray = Unity.Physics.Ray;
#endif

    public struct InputCommon : IComponentData
    {
        /// <summary> The size of the screen. <see cref="Screen.width" /> and <see cref="Screen.height" />. </summary>
        public int2 ScreenSize;

        /// <summary> Point in screen space. Screenspace is defined in pixels. The bottom-left is (0,0); the right-top is (pixelWidth,pixelHeight). </summary>
        public float2 CursorScreenPoint;

        /// <summary> Cursor point in viewport space. Viewport space is normalized. The bottom-left is (0,0); the top-right is (1,1). </summary>
        public float2 CursorViewPoint;

        /// <summary> Cursor point in camera viewport space. Viewport space is normalized. The bottom-left is (0,0); the top-right is (1,1). </summary>
        public float2 CursorCameraViewPoint;

        /// <summary> Is the cursor currently inside the view port. </summary>
        public bool CursorInViewPort;

        /// <summary> Is the cursor currently inside the cameras view port. </summary>
        public bool CursorInCameraViewPort;

        /// <summary> Gets a value indicating whether the cursor is currently over the UI. </summary>
        public bool InputOverUI;

        /// <summary> Gets a value indicating whether the application has focus. </summary>
        public bool ApplicationFocus;

#if UNITY_PHYSICS
        /// <summary> A ray going from camera through the current <see cref="CursorScreenPoint" /> using <see cref="Camera.ScreenPointToRay(Vector3)" />. </summary>
        /// <remarks> Displacement is set as a unit vector. </remarks>
        public Ray CameraRay;
#endif
        /// <summary> Gets a value indicating whether any button was pressed. </summary>
        public bool AnyButtonPress;

        /// <summary>
        /// Gets a value indicating whether the cursor is in the view and the application has focus.
        /// combination of <see cref="CursorInViewPort" /> && <see cref="ApplicationFocus" />.
        /// </summary>
        public bool InViewWithFocus => this.CursorInViewPort && this.ApplicationFocus;
    }
}
