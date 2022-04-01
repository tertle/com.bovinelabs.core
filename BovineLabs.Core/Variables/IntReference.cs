// <copyright file="IntReference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;

    /// <summary> The int reference. </summary>
    [Serializable]
    public class IntReference : Reference<IntVariable, int>
    {
        /// <summary> Initializes a new instance of the <see cref="IntReference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public IntReference(int value)
            : base(value)
        {
        }
    }
}
