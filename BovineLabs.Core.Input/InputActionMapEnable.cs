﻿// <copyright file="InputActionMapEnable.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input
{
    using Unity.Collections;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    public struct InputActionMapEnable : IBufferElementData
    {
        public FixedString32Bytes Input;
        public bool Enable;
    }
}
