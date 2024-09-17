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
        private readonly bool isPresentation;
        private EntityQuery pauseQuery;
        private EntityQuery debugQuery;

        private bool wasPaused;
        private bool hasUpdatedThisFrame;
        private double pauseTime;

        public PauseRateManager(ComponentSystemGroup group, bool isPresentation)
        {
            this.pauseQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build(group);
            this.debugQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<BLDebug>().Build(group);
            this.isPresentation = isPresentation;
        }

        public float Timestep { get; set; }

        public bool ShouldGroupUpdate(ComponentSystemGroup group)
        {
            if (this.hasUpdatedThisFrame)
            {
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
                this.debugQuery.GetSingleton<BLDebug>().Info("Game Paused: true");
                this.pauseTime = group.World.Time.ElapsedTime;
                this.wasPaused = true;
            }
            else if (!isPaused && this.wasPaused)
            {
                this.debugQuery.GetSingleton<BLDebug>().Info("Game Paused: false");
                this.wasPaused = false;
            }

            if (isPaused)
            {
                // Game time progress needs to be paused to stop fixed step catchup after unpausing
                group.World.Time = new TimeData(this.pauseTime, group.World.Time.DeltaTime);
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
