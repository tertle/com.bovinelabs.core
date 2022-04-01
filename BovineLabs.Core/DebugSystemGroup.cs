// <copyright file="DebugSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public class DebugSystemGroup : ComponentSystemGroup
    {
    }
#endif
}