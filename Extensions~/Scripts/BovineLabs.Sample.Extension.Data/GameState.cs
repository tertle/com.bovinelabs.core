// <copyright file="GameState.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.Data
{
    using global::BovineLabs.Core.Collections;
    using global::BovineLabs.Core.States;
    using Unity.Entities;
    using Unity.Properties;

    public struct GameState : IState<BitArray256>
    {
        [CreateProperty]
        public BitArray256 Value { get; set; }
    }

    public struct GameStatePrevious : IComponentData
    {
        public BitArray256 Value;
    }
}
