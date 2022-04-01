// <copyright file="ByteReference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;

    /// <summary> The bool reference. </summary>
    [Serializable]
    public class ByteReference : Reference<ByteVariable, byte>
    {
        /// <summary> Initializes a new instance of the <see cref="ByteReference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public ByteReference(byte value)
            : base(value)
        {
        }
    }
}
