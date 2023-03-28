// <copyright file="DestroySystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_DESTROY
namespace BovineLabs.Core.Destroy
{
    using Unity.Entities;

    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class DestroySystemGroup : ComponentSystemGroup
    {
    }
}
#endif
