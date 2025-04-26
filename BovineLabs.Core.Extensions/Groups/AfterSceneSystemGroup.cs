// <copyright file="AfterSceneSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Groups
{
    using Unity.Entities;
    using Unity.Scenes;

    /// <summary>
    /// Group that updates after <see cref="SceneSystemGroup"/>.
    /// Put initialization systems in here that need to affect entities just loaded from SubScenes.
    /// </summary>
    [WorldSystemFilter(Worlds.All, Worlds.Simulation)]
    [UpdateAfter(typeof(SceneSystemGroup))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class AfterSceneSystemGroup : ComponentSystemGroup
    {
    }
}
