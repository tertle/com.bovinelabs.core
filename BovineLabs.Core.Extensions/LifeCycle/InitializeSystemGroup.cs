// <copyright file="InitializeSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.App;
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    [UpdateInGroup(typeof(AfterSceneSystemGroup), OrderFirst = true)]
    public partial class InitializeSystemGroup : InitializationRequireSubScenesSystemGroup
    {
    }
}
#endif
