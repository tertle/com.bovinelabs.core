// <copyright file="StateInstanceSystemBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Entities;

    public interface IStateInstanceSystem
    {
        /// <summary> Gets the state key. </summary>
        byte StateKey { get; }

        /// <summary> Gets the state instance component this system reacts to. </summary>
        ComponentType StateInstanceComponent { get; }
    }

    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used by reflection to determine what group this system belongs to.")]
    public abstract partial class StateInstanceSystemBase<T> : SystemBase, IStateInstanceSystem
        where T : IStateComponent
    {
        /// <inheritdoc/>
        public abstract byte StateKey { get; }

        /// <inheritdoc/>
        public abstract ComponentType StateInstanceComponent { get; }
    }
}
