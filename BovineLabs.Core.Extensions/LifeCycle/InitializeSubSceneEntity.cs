// <copyright file="InitializeSubSceneEntity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using Unity.Entities;

    /// <summary>
    /// By default, most entities in a sub scene should not run initialization, however this gives the option to run on them by
    /// doing an any query e.g. [WithAny(typeof(InitializeEntity), typeof(InitializeSubSceneEntity))].
    /// </summary>
    public struct InitializeSubSceneEntity : IComponentData, IEnableableComponent
    {
    }
}
#endif
