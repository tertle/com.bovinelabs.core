// <copyright file="UISystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using Unity.Entities;

    /// <summary> System group for UI systems. </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation, WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class UISystemGroup : ComponentSystemGroup
    {
    }
}
#endif
