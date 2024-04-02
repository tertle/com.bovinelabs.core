// <copyright file="DestroySystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.App;
    using Unity.Entities;
    using Unity.Scenes;

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(InstantiateCommandBufferSystem))]
    [UpdateBefore(typeof(SceneSystemGroup))]
    public partial class DestroySystemGroup : InitializationRequireSubScenesSystemGroup
    {
    }
}
#endif
