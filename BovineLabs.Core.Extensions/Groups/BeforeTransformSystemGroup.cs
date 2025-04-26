// <copyright file="BeforeTransformSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Groups
{
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// A system group that updates before the <see cref="TransformSystemGroup"/>.
    /// Any simulation system that writes to <see cref="LocalTransform"/> should update in here.
    /// </summary>
    [WorldSystemFilter(Worlds.SimulationEditor, Worlds.Simulation)]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class BeforeTransformSystemGroup : BLSimulationSystemGroup
    {
    }
}
