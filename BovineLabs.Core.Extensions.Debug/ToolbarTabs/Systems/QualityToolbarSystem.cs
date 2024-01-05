// <copyright file="TimeToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.Toolbar;
    using Unity.Burst;
    using Unity.Entities;

    [WorldSystemFilter(Worlds.Service)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial struct QualityToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<QualityToolbarBindings, QualityToolbarBindings.Data> toolbar;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<QualityToolbarBindings, QualityToolbarBindings.Data>(state.World, "Quality", "quality");
        }

        /// <inheritdoc/>
        public void OnStartRunning(ref SystemState state)
        {
            this.toolbar.Load();
        }

        /// <inheritdoc/>
        public void OnStopRunning(ref SystemState state)
        {
            this.toolbar.Unload();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }
    }
}
#endif
