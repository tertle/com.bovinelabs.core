// <copyright file="BeginSimulationSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Groups
{
    using Unity.Entities;

    [WorldSystemFilter(Worlds.SimulationThin, Worlds.SimulationThin)]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(VariableRateSimulationSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
#if UNITY_NETCODE
    [UpdateAfter(typeof(Unity.NetCode.GhostSimulationSystemGroup))]
    [UpdateBefore(typeof(Unity.NetCode.PredictedSimulationSystemGroup))]
#endif
    public partial class BeginSimulationSystemGroup : ComponentSystemGroup
    {
    }
}
