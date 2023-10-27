// <copyright file="FPSToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.Debug.ToolbarTabs
{
    using BovineLabs.Core.Debug.Toolbar;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> The toolbar for monitoring frames per second performance. </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    [BurstCompile]
    internal unsafe partial class FPSToolbarSystem : ToolbarSystemBase
    {
        private const int AvgFPSSamplesCapacity = 127;
        private const int TimeToResetMinMaxFPS = 10;

        private VisualTreeAsset? asset;
        private Label? avgLabel;

        private FPSStatistics fps;
        private Label? fpsLabel;
        private Label? frameTimeLabel;
        private Label? maxLabel;
        private Label? minLabel;
        private float timeToTriggerUpdatesPassed;

        /// <inheritdoc />
        protected override VisualTreeAsset Asset => this.asset!;

        /// <inheritdoc />
        protected override string Tab => "Monitor";

        /// <inheritdoc />
        protected override string Name => "FPS";

        /// <inheritdoc />
        protected override void OnCreateSystem()
        {
            var averageFPSSamples = default(FixedList512Bytes<float>);
            averageFPSSamples.Length = AvgFPSSamplesCapacity;
            this.fps = new FPSStatistics { AverageFPSSamples = averageFPSSamples };

            this.asset = Resources.Load<VisualTreeAsset>("FPSGroup");
        }

        /// <inheritdoc />
        protected override void OnLoad(VisualElement element)
        {
            this.fpsLabel = element.Q<Label>("fps");
            this.frameTimeLabel = element.Q<Label>("frametime");
            this.avgLabel = element.Q<Label>("avg");
            this.minLabel = element.Q<Label>("min");
            this.maxLabel = element.Q<Label>("max");
        }

        /// <inheritdoc />
        protected override void OnUpdateAlways()
        {
            this.timeToTriggerUpdatesPassed += UnityEngine.Time.unscaledDeltaTime;

            fixed (FPSStatistics* ptr = &this.fps)
            {
                CalculateStatistics(ptr, UnityEngine.Time.unscaledDeltaTime);
            }
        }

        /// <inheritdoc />
        protected override void OnUpdateVisible()
        {
            if (this.timeToTriggerUpdatesPassed < DefaultUpdateRate)
            {
                return;
            }

            this.timeToTriggerUpdatesPassed = 0;

            var ft = this.fps.CurrentFPS == 0 ? 0 : 1000 / this.fps.CurrentFPS;
            this.fpsLabel!.text = $"{(int)this.fps.CurrentFPS} fps";
            this.frameTimeLabel!.text = $"{ft:0.0} ms";
            this.avgLabel!.text = $"{(int)this.fps.AvgFPS} fps";
            this.minLabel!.text = $"{(int)this.fps.MinFPS} fps";
            this.maxLabel!.text = $"{(int)this.fps.MaxFPS} fps";
        }

        [BurstCompile]
        private static void CalculateStatistics(FPSStatistics* fps, float unscaledDeltaTime)
        {
            fps->TimeToResetMinFPSPassed += unscaledDeltaTime;
            fps->TimeToResetMaxFPSPassed += unscaledDeltaTime;

            // Build FPS and ms
            fps->CurrentFPS = 1 / unscaledDeltaTime;

            // Build avg FPS
            fps->AvgFPS = 0;
            fps->AverageFPSSamples[fps->IndexSample++] = fps->CurrentFPS;

            if (fps->IndexSample == AvgFPSSamplesCapacity)
            {
                fps->IndexSample = 0;
            }

            if (fps->AvgFPSSamplesCount < AvgFPSSamplesCapacity)
            {
                fps->AvgFPSSamplesCount++;
            }

            for (var i = 0; i < fps->AvgFPSSamplesCount; i++)
            {
                fps->AvgFPS += fps->AverageFPSSamples[i];
            }

            fps->AvgFPS /= fps->AvgFPSSamplesCount;

            // Checks to reset min and max FPS
            if (fps->TimeToResetMinFPSPassed > TimeToResetMinMaxFPS)
            {
                fps->MinFPS = 0;
                fps->TimeToResetMinFPSPassed = 0;
            }

            if (fps->TimeToResetMaxFPSPassed > TimeToResetMinMaxFPS)
            {
                fps->MaxFPS = 0;
                fps->TimeToResetMaxFPSPassed = 0;
            }

            // Build min FPS
            if ((fps->CurrentFPS < fps->MinFPS) || (fps->MinFPS <= 0))
            {
                fps->MinFPS = fps->CurrentFPS;

                fps->TimeToResetMinFPSPassed = 0;
            }

            // Build max FPS
            if ((fps->CurrentFPS > fps->MaxFPS) || (fps->MaxFPS <= 0))
            {
                fps->MaxFPS = fps->CurrentFPS;

                fps->TimeToResetMaxFPSPassed = 0;
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
