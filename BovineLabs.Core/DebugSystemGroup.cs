// <copyright file="DebugSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;
    using WorldFlag = Unity.Entities.WorldSystemFilterFlags;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(
        WorldFlag.LocalSimulation | WorldFlag.ClientSimulation | WorldFlag.ServerSimulation | WorldFlag.ThinClientSimulation | WorldFlag.Editor,
        WorldFlag.LocalSimulation | WorldFlag.ClientSimulation | WorldFlag.ServerSimulation | WorldFlag.ThinClientSimulation)]
    public partial class DebugSystemGroup : ComponentSystemGroup
    {
    }
#endif
}
