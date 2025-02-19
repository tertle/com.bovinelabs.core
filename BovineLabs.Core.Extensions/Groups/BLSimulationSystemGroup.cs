// <copyright file="BLSimulationSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Groups
{
    using BovineLabs.Core.Pause;
    using Unity.Entities;

    public abstract partial class BLSimulationSystemGroup : ComponentSystemGroup, IDisableWhilePaused
    {
    }
}
