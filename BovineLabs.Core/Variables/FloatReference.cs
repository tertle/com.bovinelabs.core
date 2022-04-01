// <copyright file="FloatReference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;

    /// <summary> The float reference. </summary>
    [Serializable]
    public class FloatReference : Reference<FloatVariable, float>
    {
        /// <summary> Initializes a new instance of the <see cref="FloatReference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public FloatReference(float value)
            : base(value)
        {
        }
    }
}