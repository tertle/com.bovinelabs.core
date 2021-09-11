// <copyright file="IStateComponent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using Unity.Entities;

    public interface IStateComponent : IComponentData
    {
        byte Value { get; }
    }
}
