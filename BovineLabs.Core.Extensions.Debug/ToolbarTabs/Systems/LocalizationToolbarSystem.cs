// <copyright file="LocalizationToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && UNITY_LOCALIZATION
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.Toolbar;
    using Unity.Burst;
    using Unity.Entities;

    [WorldSystemFilter(Worlds.Service)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    public partial struct LocalizationToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<LocalizationToolbarBindings, LocalizationToolbarBindings.Data> toolbar;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<LocalizationToolbarBindings, LocalizationToolbarBindings.Data>(state.World, "Localization", "localization");
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
            ref var data = ref this.toolbar.Binding;
        }
    }
}
#endif
