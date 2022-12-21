// <copyright file="StateInstance.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using Unity.Entities;

    public struct StateInstance : IComponentData
    {
        public TypeIndex State;

        /// <summary> Gets the state key. </summary>
        public byte StateKey;

        /// <summary> Gets the state instance component this system reacts to. </summary>
        public TypeIndex StateInstanceComponent;
    }
}
