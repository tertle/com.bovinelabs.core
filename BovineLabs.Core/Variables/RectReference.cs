// <copyright file="RectReference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;
    using UnityEngine;

    /// <summary> The rect reference. </summary>
    [Serializable]
    public class RectReference : Reference<RectVariable, Rect>
    {
        /// <summary> Initializes a new instance of the <see cref="RectReference"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public RectReference(Rect value)
            : base(value)
        {
        }
    }
}