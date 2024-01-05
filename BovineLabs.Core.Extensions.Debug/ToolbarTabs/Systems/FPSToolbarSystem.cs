// <copyright file="FPSToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.Time;
    using BovineLabs.Core.Toolbar;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary> The toolbar for monitoring frames per second performance. </summary>
    [WorldSystemFilter(Worlds.Service)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial struct FPSToolbarSystem : ISystem, ISystemStartStop
    {
        private const int AvgFPSSamplesCapacity = 127;
        private const int TimeToResetMinMaxFPS = 10;

        private FPSStatistics fps;
        private float timeToTriggerUpdatesPassed;
        private ToolbarHelper<FPSToolbarBindings, FPSToolbarBindings.Data> toolbar;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<FPSToolbarBindings, FPSToolbarBindings.Data>(state.World, "FPS", "fps");

            var averageFPSSamples = default(FixedList512Bytes<float>);
            averageFPSSamples.Length = AvgFPSSamplesCapacity;
            this.fps = new FPSStatistics { AverageFPSSamples = averageFPSSamples };
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
            var time = SystemAPI.GetSingleton<UnityTime>();
            this.timeToTriggerUpdatesPassed += time.UnscaledDeltaTime;

            this.CalculateStatistics(time.UnscaledDeltaTime);

            if (!this.toolbar.IsVisible())
            {
                return;
            }

            this.UpdateVisible();
        }

        private void UpdateVisible()
        {
            ref var data = ref this.toolbar.Binding;

            if (this.timeToTriggerUpdatesPassed < ToolbarManager.DefaultUpdateRate)
            {
                return;
            }

            this.timeToTriggerUpdatesPassed = 0;

            data.CurrentFPS = this.fps.CurrentFPS;
            data.AverageFPS = this.fps.AvgFPS;
            data.MinFPS = this.fps.MinFPS;
            data.MaxFPS = this.fps.MaxFPS;
            data.Version++;
        }

        private void CalculateStatistics(float unscaledDeltaTime)
        {
            this.fps.TimeToResetMinFPSPassed += unscaledDeltaTime;
            this.fps.TimeToResetMaxFPSPassed += unscaledDeltaTime;

            // Build FPS and ms
            this.fps.CurrentFPS = 1 / unscaledDeltaTime;

            // Build avg FPS
            this.fps.AvgFPS = 0;
            this.fps.AverageFPSSamples[this.fps.IndexSample++] = this.fps.CurrentFPS;

            if (this.fps.IndexSample == AvgFPSSamplesCapacity)
            {
                this.fps.IndexSample = 0;
            }

            if (this.fps.AvgFPSSamplesCount < AvgFPSSamplesCapacity)
            {
                this.fps.AvgFPSSamplesCount++;
            }

            for (var i = 0; i < this.fps.AvgFPSSamplesCount; i++)
            {
                this.fps.AvgFPS += this.fps.AverageFPSSamples[i];
            }

            this.fps.AvgFPS /= this.fps.AvgFPSSamplesCount;

            // Checks to reset min and max FPS
            if (this.fps.TimeToResetMinFPSPassed > TimeToResetMinMaxFPS)
            {
                this.fps.MinFPS = 0;
                this.fps.TimeToResetMinFPSPassed = 0;
            }

            if (this.fps.TimeToResetMaxFPSPassed > TimeToResetMinMaxFPS)
            {
                this.fps.MaxFPS = 0;
                this.fps.TimeToResetMaxFPSPassed = 0;
            }

            // Build min FPS
            if ((this.fps.CurrentFPS < this.fps.MinFPS) || (this.fps.MinFPS <= 0))
            {
                this.fps.MinFPS = this.fps.CurrentFPS;

                this.fps.TimeToResetMinFPSPassed = 0;
            }

            // Build max FPS
            if ((this.fps.CurrentFPS > this.fps.MaxFPS) || (this.fps.MaxFPS <= 0))
            {
                this.fps.MaxFPS = this.fps.CurrentFPS;

                this.fps.TimeToResetMaxFPSPassed = 0;
            }
        }

        private struct FPSStatistics
        {
            public float CurrentFPS;
            public float AvgFPS;
            public float MinFPS;
            public float MaxFPS;

            public FixedList512Bytes<float> AverageFPSSamples; // used as an array
            public int AvgFPSSamplesCount;
            public int IndexSample;

            public float TimeToResetMinFPSPassed;
            public float TimeToResetMaxFPSPassed;
        }
    }
}
#endif
