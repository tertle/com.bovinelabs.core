// <copyright file="Vector4Reference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;
    using UnityEngine;

    /// <summary> The vector4 reference. </summary>
    [Serializable]
    public class Vector4Reference : Reference<Vector4Variable, Vector4>
    {
        /// <summary> Initializes a new instance of the <see cref="Vector4Reference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public Vector4Reference(Vector4 value)
            : base(value)
        {
        }
    }
}