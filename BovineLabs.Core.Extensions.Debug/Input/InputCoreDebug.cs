﻿// <copyright file="InputCoreDebug.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using Unity.Entities;

    public partial struct InputCoreDebug : IComponentData
    {
        [InputActionDown]
        public bool TimeScaleDouble;

        [InputActionDown]
        public bool TimeScaleHalve;
    }
}
#endif