// <copyright file="TimeToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.App;
    using BovineLabs.Core.Toolbar;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial struct TimeToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<TimeToolbarBindings, TimeToolbarBindings.Data> toolbar;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<TimeToolbarBindings, TimeToolbarBindings.Data>(state.World, "Time", "time");
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
            if (!this.toolbar.IsVisible())
            {
                return;
            }

            ref var data = ref this.toolbar.Binding;

            if (data.Paused)
            {
                if (!state.EntityManager.HasComponent<PauseGame>(state.SystemHandle))
                {
                    state.EntityManager.AddComponent<PauseGame>(state.SystemHandle);
                }
            }
            else
            {
                if (state.EntityManager.HasComponent<PauseGame>(state.SystemHandle))
                {
                    state.EntityManager.RemoveComponent<PauseGame>(state.SystemHandle);
                }
            }
        }
    }
}
#endif
