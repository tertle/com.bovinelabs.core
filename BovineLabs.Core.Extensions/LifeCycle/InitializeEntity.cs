// <copyright file="InitializeEntity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using Unity.Entities;

    /// <summary> Marks prefab entities for initialization. When enabled, triggers initialization systems during the InitializeSystemGroup phase. </summary>
    public struct InitializeEntity : IComponentData, IEnableableComponent
    {
    }
}
#endif
