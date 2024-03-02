// <copyright file="InitializeSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.LifeCycle
{
    using Unity.Entities;

    [UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class InitializeSystemGroup : ComponentSystemGroup
    {
    }
}
