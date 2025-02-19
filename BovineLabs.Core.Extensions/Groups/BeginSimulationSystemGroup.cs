// <copyright file="BeginSimulationSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Groups
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Entities;
#if UNITY_NETCODE
    using Unity.NetCode;
#endif

    /// <summary>
    /// A system group for updating at the start of <see cref="SimulationSystemGroup"/>.
    /// Use of this should be limited to systems that setup data that may be used in many other systems, such as Input.
    /// </summary>
    [WorldSystemFilter(Worlds.SimulationEditor, Worlds.SimulationThin)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(VariableRateSimulationSystemGroup))]
    public partial class BeginSimulationSystemGroup : BLSimulationSystemGroup
    {
    }

#if UNITY_NETCODE
    /// <summary> A hack system to stop warnings with RpcReceivedSystemGroup ordering when in editor world. </summary>
    [UpdateBefore(typeof(BeginSimulationSystemGroup))]
    [UpdateAfter(typeof(GhostSimulationSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Convenience")]
    internal partial class UpdateAfterGhostSimulationSystemGroup : SystemBase
    {
        protected override void OnCreate()
        {
            this.Enabled = false;
        }

        protected override void OnUpdate()
        {
        }
    }

    [UpdateAfter(typeof(BeginSimulationSystemGroup))]
    [UpdateBefore(typeof(PredictedSimulationSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Convenience")]
    internal partial class UpdateBeforePredictedSimulationSystemGroup : SystemBase
    {
        protected override void OnCreate()
        {
            this.Enabled = false;
        }

        protected override void OnUpdate()
        {
        }
    }
#endif
}
