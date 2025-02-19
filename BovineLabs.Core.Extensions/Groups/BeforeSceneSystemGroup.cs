// <copyright file="BeforeSceneSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Groups
{
    using Unity.Entities;
    using Unity.Scenes;

    /// <summary>
    /// Group that updates before <see cref="SceneSystemGroup"/>.
    /// Put systems in here that need to affect entities before they might be unloaded by a SubScene.
    /// General initialization code that isn't dependent on subscene entities should also go in here.
    /// </summary>
    [WorldSystemFilter(Worlds.All, Worlds.SimulationThin)]
    [UpdateBefore(typeof(SceneSystemGroup))]
#if BL_DISABLE_TIME
    [UpdateAfter(typeof(UpdateWorldTimeSystem))]
#endif
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class BeforeSceneSystemGroup : ComponentSystemGroup
    {
    }
}
