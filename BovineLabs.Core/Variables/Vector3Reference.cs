// <copyright file="Vector3Reference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;
    using UnityEngine;

    /// <summary> The vector3 reference. </summary>
    [Serializable]
    public class Vector3Reference : Reference<Vector3Variable, Vector3>
    {
        /// <summary> Initializes a new instance of the <see cref="Vector3Reference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public Vector3Reference(Vector3 value)
            : base(value)
        {
        }
    }
}