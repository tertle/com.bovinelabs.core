// <copyright file="AfterTransformSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Groups
{
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// A system group that updates after the <see cref="TransformSystemGroup"/> ensure up-to-date transform data.
    /// Any simulation system that does not write to <see cref="LocalTransform"/> should update in here.
    /// </summary>
    [WorldSystemFilter(Worlds.SimulationEditor, Worlds.Simulation)]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(CompanionGameObjectUpdateTransformSystem))]
    public partial class AfterTransformSystemGroup : BLSimulationSystemGroup
    {
    }
}
