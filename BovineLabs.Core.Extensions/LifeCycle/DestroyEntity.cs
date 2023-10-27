// <copyright file="DestroyEntity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using Unity.Entities;

    /// <summary> Unified destroy component allowing entities to all pass through a singular cleanup group. </summary>
    [ChangeFilterTracking]
    public struct DestroyEntity : IComponentData, IEnableableComponent
    {
    }
}
#endif
