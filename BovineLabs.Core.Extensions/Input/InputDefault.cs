// <copyright file="InputDefault.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using System;
    using Unity.Entities;
    using UnityEngine.InputSystem;

    [Serializable]
    public class InputDefault : IComponentData
    {
        public InputActionAsset Asset = null!;
        public InputActionReference CursorPosition = null!;
    }
}
#endif
