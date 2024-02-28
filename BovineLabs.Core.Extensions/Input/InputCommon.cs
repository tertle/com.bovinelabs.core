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
        /// <summary> Point in screen space. Screenspace is defined in pixels. The bottom-left is (0,0); the right-top is (pixelWidth,pixelHeight). </summary>
        public float2 ScreenPoint;

        /// <summary> Point in viewport space. Viewport space is normalized. The bottom-left is (0,0); the top-right is (1,1). </summary>
        public float2 ViewPoint;

        /// <summary> Is the cursor currently inside the view port. </summary>
        public bool InViewPort;

        /// <summary> Gets a value indicating whether the cursor is currently over the UI. </summary>
        public bool InputOverUI;

        /// <summary> A ray going from camera through the current <see cref="ScreenPoint" /> using <see cref="Camera.ScreenPointToRay(Vector3)" />. </summary>
        /// <remarks> Displacement is set as a unit vector. </remarks>
        public Ray CameraRay;

        public bool AnyButtonPress;
    }
}
#endif
