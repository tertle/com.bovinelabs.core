// <copyright file="TimeToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.App;
    using BovineLabs.Core.Toolbar;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial struct TimeToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<TimeToolbarBindings, TimeToolbarBindings.Data> toolbar;
        private float lastTimeScale;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<TimeToolbarBindings, TimeToolbarBindings.Data>(ref state, "Time", "time");
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
                    PauseGame.Pause(ref state);
                }
            }
            else
            {
                if (state.EntityManager.HasComponent<PauseGame>(state.SystemHandle))
                {
                    PauseGame.Unpause(ref state);
                }
            }

            if (!mathex.Approximately(UnityEngine.Time.timeScale, this.lastTimeScale))
            {
                data.Timescale = UnityEngine.Time.timeScale;
                this.lastTimeScale = data.Timescale;
            }
            else if (!mathex.Approximately(data.Timescale, this.lastTimeScale))
            {
                UnityEngine.Time.timeScale = data.Timescale;
                this.lastTimeScale = data.Timescale;
            }
        }
    }
}
#endif
