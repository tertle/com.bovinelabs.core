// <copyright file="SceneLoadedSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Groups
{
    using Unity.Entities;
    using Unity.Scenes;

    [UpdateAfter(typeof(SceneSystemGroup))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | Worlds.Service)]
    public partial class AfterSceneSystemGroup : ComponentSystemGroup
    {
    }
}
