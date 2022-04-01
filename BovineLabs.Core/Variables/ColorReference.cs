// <copyright file="ColorReference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;
    using UnityEngine;

    /// <summary> The color reference. </summary>
    [Serializable]
    public class ColorReference : Reference<ColorVariable, Color>
    {
        /// <summary> Initializes a new instance of the <see cref="ColorReference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public ColorReference(Color value)
            : base(value)
        {
        }
    }
}