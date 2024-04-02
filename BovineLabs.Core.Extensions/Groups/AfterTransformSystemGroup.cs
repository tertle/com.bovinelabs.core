// <copyright file="AfterTransformSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Groups
{
    using Unity.Entities;
    using Unity.Transforms;

    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(CompanionGameObjectUpdateTransformSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class AfterTransformSystemGroup : ComponentSystemGroup
    {
    }
}
