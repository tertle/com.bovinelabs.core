// <copyright file="BoolReference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;

    /// <summary> The bool reference. </summary>
    [Serializable]
    public class BoolReference : Reference<BoolVariable, bool>
    {
        /// <summary> Initializes a new instance of the <see cref="BoolReference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public BoolReference(bool value)
            : base(value)
        {
        }
    }
}