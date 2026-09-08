// <copyright file="SaveFeature.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Savanna
{
    using System;

    [Flags]
    public enum SaveFeature : byte
    {
        None = 0,

        /// <summary>
        /// Can this component be added at runtime?
        /// Using this will cause deserialization to be a lot slower as it will need to be processed by an entity command buffer.
        /// </summary>
        AddComponent = 1,
    }
}
