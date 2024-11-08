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
        private readonly IRateManager existingRateManager;
        private readonly bool isPresentation;
        private EntityQuery pauseQuery;
        private EntityQuery debugQuery;

        private bool wasPaused;
        private bool hasUpdatedThisFrame;
        private double pauseTime;

        public PauseRateManager(ComponentSystemGroup group, IRateManager existingRateManager, bool isPresentation)
        {
            this.pauseQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build(group);
            this.debugQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<BLDebug>().Build(group);
            this.existingRateManager = existingRateManager;
            this.isPresentation = isPresentation;
        }

        public float Timestep { get; set; }

        public bool ShouldGroupUpdate(ComponentSystemGroup group)
        {
            if (this.hasUpdatedThisFrame)
            {
                if (this.existingRateManager?.ShouldGroupUpdate(group) ?? false)
                {
                    return true;
                }

                this.hasUpdatedThisFrame = false;
                return false;
            }

            var pauses = this.pauseQuery.ToComponentDataArray<PauseGame>(group.WorldUpdateAllocator);
            var isPaused = false;

            if (this.isPresentation)
            {
                foreach (var p in pauses)
                {
                    if (p.PausePresentation)
                    {
                        isPaused = true;
                        break;
                    }
                }
            }
            else
            {
                isPaused = pauses.Length > 0;
            }

            // Became paused this frame
            if (isPaused && !this.wasPaused)
            {
                if (!this.isPresentation)
                {
                    this.debugQuery.GetSingleton<BLDebug>().Debug("World Paused: true");
                }

                this.pauseTime = group.World.Time.ElapsedTime;
                this.wasPaused = true;
            }
            else if (!isPaused && this.wasPaused)
            {
                if (!this.isPresentation)
                {
                    this.debugQuery.GetSingleton<BLDebug>().Debug("World Paused: false");
                }

                this.wasPaused = false;
            }

            if (isPaused)
            {
                // Game time progress needs to be paused to stop fixed step catchup after unpausing
                group.World.Time = new TimeData(this.pauseTime, group.World.Time.DeltaTime);
                PauseUtility.UpdateAlwaysSystems(group);

                this.Timestep = 0;
                return false;
            }

            if (!this.existingRateManager?.ShouldGroupUpdate(group) ?? false)
            {
                this.Timestep = 0;
                return false;
            }

            this.Timestep = group.World.Time.DeltaTime;
            this.hasUpdatedThisFrame = true;
            return true;
        }
    }
}
#endif
