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
    using Unity.Mathematics;
    using UnityEngine;

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
#if !BL_DISABLE_INPUT
            const float multi = 8f;

            var inputDebug = SystemAPI.GetSingleton<InputCoreDebug>();
            if (inputDebug.TimeScaleDouble.Down)
            {
                Time.timeScale = math.clamp(math.ceilpow2((int)(Time.timeScale * multi * 2)) / multi, 1 / multi, 100);
            }
            else if (inputDebug.TimeScaleHalve.Up)
            {
                Time.timeScale = math.max(math.ceilpow2((int)(Time.timeScale * multi / 2)) / multi, 0);
            }
#endif

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

            var newTimescale = math.clamp(data.TimeScale, 0, 100);

            if (!mathex.Approximately(Time.timeScale, this.lastTimeScale, 0.01f))
            {
                data.TimeScale = Time.timeScale;
                this.lastTimeScale = data.TimeScale;
            }
            else if (!mathex.Approximately(newTimescale, this.lastTimeScale, 0.01f))
            {
                Time.timeScale = newTimescale;
                this.lastTimeScale = newTimescale;
                data.TimeScale = newTimescale; // in case it went outside clamp
            }
        }
    }
}
#endif
