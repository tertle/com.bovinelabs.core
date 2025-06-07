// <copyright file="InputCoreDebug.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input.Debug
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
