// <copyright file="DestroySystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(BeginSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | Worlds.Service)]
    public partial class DestroySystemGroup : ComponentSystemGroup
    {
    }
}
#endif
