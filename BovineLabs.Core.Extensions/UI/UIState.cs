// <copyright file="UIState.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    public struct UIState : IComponentData
    {
        public BitArray256 Value;
    }

    internal struct UIStatePrevious : IComponentData
    {
        public BitArray256 Value { get; set; }
    }

    [InternalBufferCapacity(0)]
    public struct UIStateBack : IBufferElementData
    {
        public BitArray256 Value;
        public bool Popup;
    }

    [InternalBufferCapacity(0)]
    public struct UIStateForward : IBufferElementData
    {
        public BitArray256 Value;
        public bool Popup;
    }
}
