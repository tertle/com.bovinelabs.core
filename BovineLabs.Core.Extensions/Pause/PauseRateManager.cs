// <copyright file="PauseRateManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PAUSE
namespace BovineLabs.Core.Pause
{
    using Unity.Collections;
    using Unity.Core;
    using Unity.Entities;

    public class PauseRateManager : IRateManager
    {
        private EntityQuery pauseQuery;

        private bool wasPaused;
        private bool hasUpdatedThisFrame;
        private double pauseTime;

        public PauseRateManager(ComponentSystemGroup group)
        {
            this.pauseQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build(group);
            this.ExistingRateManager = group.RateManager;
        }

        /// <inheritdoc/>
        float IRateManager.Timestep
        {
            get => this.ExistingRateManager?.Timestep ?? 0;
            set
            {
                if (this.ExistingRateManager != null)
                {
                    this.ExistingRateManager.Timestep = value;
                }
            }
        }

        public IRateManager ExistingRateManager { get; }

        /// <inheritdoc/>
        bool IRateManager.ShouldGroupUpdate(ComponentSystemGroup group)
        {
            if (this.hasUpdatedThisFrame)
            {
                if (this.ExistingRateManager?.ShouldGroupUpdate(group) ?? false)
                {
                    return true;
                }

                this.hasUpdatedThisFrame = false;
                return false;
            }

            var pauses = this.pauseQuery.ToComponentDataArray<PauseGame>(group.WorldUpdateAllocator);

            var isPauseAll = false;

            foreach (var p in pauses)
            {
                if (p.PauseAll)
                {
                    isPauseAll = true;
                    break;
                }
            }

            var isPaused = pauses.Length > 0;

            // Became paused this frame
            if (isPaused && !this.wasPaused)
            {
                this.pauseTime = group.World.Time.ElapsedTime;
                this.wasPaused = true;
            }
            else if (!isPaused && this.wasPaused)
            {
                this.wasPaused = false;
            }

            if (isPaused)
            {
                // Game time progress needs to be paused to stop fixed step catchup after unpausing
                group.World.Time = new TimeData(this.pauseTime, group.World.Time.DeltaTime);

                if (!isPauseAll)
                {
                    if (this.ExistingRateManager != null)
                    {
                        while (this.ExistingRateManager.ShouldGroupUpdate(group))
                        {
                            PauseUtility.UpdateAlwaysSystems(group);
                        }
                    }
                    else
                    {
                        PauseUtility.UpdateAlwaysSystems(group);
                    }
                }

                return false;
            }

            if (!this.ExistingRateManager?.ShouldGroupUpdate(group) ?? false)
            {
                return false;
            }

            this.hasUpdatedThisFrame = true;
            return true;
        }
    }
}
#endif
