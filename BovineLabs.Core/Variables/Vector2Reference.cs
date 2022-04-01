// <copyright file="Vector2Reference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;
    using UnityEngine;

    /// <summary> The vector2 reference. </summary>
    [Serializable]
    public class Vector2Reference : Reference<Vector2Variable, Vector2>
    {
        /// <summary> Initializes a new instance of the <see cref="Vector2Reference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public Vector2Reference(Vector2 value)
            : base(value)
        {
        }
    }
}