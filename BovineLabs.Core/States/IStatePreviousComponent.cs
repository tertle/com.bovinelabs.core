// <copyright file="IPreviousStateComponent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using Unity.Entities;

    public interface IStatePreviousComponent : IComponentData
    {
        /// <summary> Gets or sets the previous state value to check if it has changed. </summary>
        byte Value { get; set; }
    }
}
