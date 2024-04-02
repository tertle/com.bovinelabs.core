// <copyright file="InitializeEntity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using Unity.Entities;

    public struct InitializeEntity : IComponentData, IEnableableComponent
    {
    }
}
#endif
