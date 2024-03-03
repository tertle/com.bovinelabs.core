// <copyright file="InputDefault.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.InputSystem;

    internal struct InputDefault : IComponentData
    {
        public UnityObjectRef<InputActionAsset> Asset;
        public UnityObjectRef<InputActionReference> CursorPosition;
    }

    [InternalBufferCapacity(0)]
    internal struct InputDefaultEnabled : IBufferElementData
    {
        public FixedString32Bytes ActionMap;
    }
}
#endif
