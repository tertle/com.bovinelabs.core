// <copyright file="InputCommon.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using Ray = Unity.Physics.Ray;

    public struct InputCommon : IComponentData
    {
        /// <summary> The size of the screen. <see cref="Screen.width"/> and <see cref="Screen.height"/>. </summary>
        public int2 ScreenSize;

        /// <summary> Point in screen space. Screenspace is defined in pixels. The bottom-left is (0,0); the right-top is (pixelWidth,pixelHeight). </summary>
        public float2 CameraScreenPoint;

        /// <summary> Point in viewport space. Viewport space is normalized. The bottom-left is (0,0); the top-right is (1,1). </summary>
        public float2 CameraViewPoint;

        /// <summary> Point in viewport space. Viewport space is normalized. The bottom-left is (0,0); the top-right is (1,1). </summary>
        public float2 ViewPoint;

        /// <summary> Is the cursor currently inside the view port. </summary>
        public bool InViewPort;

        /// <summary> Is the cursor currently inside the view port. </summary>
        public bool InCameraViewPort;

        /// <summary> Gets a value indicating whether the cursor is currently over the UI. </summary>
        public bool InputOverUI;

        /// <summary> Gets a value indicating whether the application has focus. </summary>
        public bool ApplicationFocus;

        /// <summary> A ray going from camera through the current <see cref="CameraScreenPoint" /> using <see cref="Camera.ScreenPointToRay(Vector3)" />. </summary>
        /// <remarks> Displacement is set as a unit vector. </remarks>
        public Ray CameraRay;

        /// <summary> Gets a value indicating whether any button was pressed. </summary>
        public bool AnyButtonPress;

        /// <summary> Combination of <see cref="InViewPort"/> && <see cref="ApplicationFocus"/>. </summary>
        public bool InViewWithFocus => this.InViewPort && this.ApplicationFocus;
    }
}
#endif
