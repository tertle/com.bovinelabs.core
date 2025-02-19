// <copyright file="DebugSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using BovineLabs.Core.Pause;
    using Unity.Entities;
    using WorldFlag = Unity.Entities.WorldSystemFilterFlags;

#if UNITY_EDITOR || BL_DEBUG
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldFlag.LocalSimulation | WorldFlag.ClientSimulation | WorldFlag.ServerSimulation | WorldFlag.ThinClientSimulation | WorldFlag.Editor,
        WorldFlag.LocalSimulation | WorldFlag.ClientSimulation | WorldFlag.ServerSimulation)]
    public partial class DebugSystemGroup : ComponentSystemGroup, IUpdateWhilePaused
    {
    }
#endif
}
