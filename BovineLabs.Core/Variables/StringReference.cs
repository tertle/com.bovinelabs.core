// <copyright file="StringReference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;

    /// <summary> The string reference. </summary>
    [Serializable]
    public class StringReference : Reference<StringVariable, string>
    {
        /// <summary> Initializes a new instance of the <see cref="StringReference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public StringReference(string value)
            : base(value)
        {
        }
    }
}