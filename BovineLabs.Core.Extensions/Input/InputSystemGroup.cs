// <copyright file="InputSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using Unity.Entities;
    using Unity.NetCode;

    [WorldSystemFilter(WorldSystemFilterFlags.Presentation, WorldSystemFilterFlags.Presentation)]
#if UNITY_NETCODE
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
#else
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
#endif
    public partial class InputSystemGroup : ComponentSystemGroup
    {
    }
}
#endif
