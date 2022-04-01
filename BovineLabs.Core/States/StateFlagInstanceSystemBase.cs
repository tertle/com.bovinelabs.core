// <copyright file="StateFlagInstanceSystemBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    public interface IStateFlagInstanceSystem
    {
        /// <summary> Gets the state key. </summary>
        uint StateKey { get; }

        /// <summary> Gets the state instance component this system reacts to. </summary>
        ComponentType StateInstanceComponent { get; }
    }

    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used by reflection to determine what group this system belongs to.")]
    public abstract partial class StateFlagInstanceSystemBase<T, TS> : SystemBase, IStateFlagInstanceSystem
        where T : unmanaged, IBitArray<T>
        where TS : IStateFlagComponent<T>
    {
        /// <inheritdoc/>
        public abstract uint StateKey { get; }

        /// <inheritdoc/>
        public abstract ComponentType StateInstanceComponent { get; }
    }
}
