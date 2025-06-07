// <copyright file="InputSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input
{
    using Unity.Entities;
#if UNITY_NETCODE
    using Unity.NetCode;
#else
    using BovineLabs.Core.Groups;
#endif

    [WorldSystemFilter(Worlds.ClientLocal, WorldSystemFilterFlags.Presentation)]
#if UNITY_NETCODE
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
#else
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
#endif
    public partial class InputSystemGroup : ComponentSystemGroup
    {
    }
}
