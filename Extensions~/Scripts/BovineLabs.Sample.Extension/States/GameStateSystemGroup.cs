// <copyright file="GameStateSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.States
{
    using BovineLabs.Core;
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    [WorldSystemFilter(
        WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Presentation | Worlds.Service,
        WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Presentation)]
    public partial class GameStateSystemGroup : ComponentSystemGroup
    {
    }
}
