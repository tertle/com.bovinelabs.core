// <copyright file="EntityDestroy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_DESTROY
namespace BovineLabs.Core.Destroy
{
    using Unity.Entities;

    /// <summary> Unified destroy component allowing entities to all pass through a singular cleanup group. </summary>
    [ChangeFilterTracking]
    public struct EntityDestroy : IComponentData, IEnableableComponent
    {
    }
}
#endif
