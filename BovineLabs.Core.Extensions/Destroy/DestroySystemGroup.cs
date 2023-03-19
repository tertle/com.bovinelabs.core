// <copyright file="DestroySystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Destroy
{
    using Unity.Entities;

    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class DestroySystemGroup : ComponentSystemGroup
    {
    }
}
