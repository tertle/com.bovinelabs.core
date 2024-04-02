// <copyright file="InputCoreDebug.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.Input;
    using Unity.Entities;

    public partial struct InputCoreDebug : IComponentData
    {
        [InputAction]
        public ButtonState TimeScaleDouble;

        [InputAction]
        public ButtonState TimeScaleHalve;
    }
}
#endif
