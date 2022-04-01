// <copyright file="UIntReference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;

    /// <summary> The int reference. </summary>
    [Serializable]
    public class UIntReference : Reference<UIntVariable, uint>
    {
        /// <summary> Initializes a new instance of the <see cref="UIntReference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public UIntReference(uint value)
            : base(value)
        {
        }
    }
}
