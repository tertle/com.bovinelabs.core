// <copyright file="ToolbarSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.Toolbar
{
    using BovineLabs.Core;
    using Unity.Entities;

    [WorldSystemFilter(WorldSystemFilterFlags.Default | Worlds.Service)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial class ToolbarSystemGroup : ComponentSystemGroup
    {
    }
}
#endif
