// <copyright file="LimitedRateNoCatchUpManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Core;
    using Unity.Entities;

    /// <summary> Limits a component group to update on a timestep but not enforcing a fixed timestep and no catchup. </summary>
    public class LimitedRateNoCatchUpManager : IRateManager
    {
        private bool didPushTime;
        private double lastPushedTime;

        /// <summary> Initializes a new instance of the <see cref="LimitedRateNoCatchUpManager" /> class. </summary>
        /// <param name="defaultFixedTimestep"> The default timestep to try update to. </param>
        public LimitedRateNoCatchUpManager(float defaultFixedTimestep)
        {
            this.Timestep = defaultFixedTimestep;
        }

        /// <inheritdoc />
        public float Timestep { get; set; }

        /// <inheritdoc />
        public bool ShouldGroupUpdate(ComponentSystemGroup group)
        {
            // Already pushed this frame
            if (this.didPushTime)
            {
                group.World.PopTime();
                this.didPushTime = false;
                return false;
            }

            var deltaTime = (float)(group.World.Unmanaged.Time.ElapsedTime - this.lastPushedTime);

            // Not enough time elapsed
            if (group.World.Unmanaged.Time.ElapsedTime - this.lastPushedTime < this.Timestep)
            {
                return false;
            }

            this.lastPushedTime = group.World.Unmanaged.Time.ElapsedTime;

            group.World.PushTime(new TimeData(group.World.Unmanaged.Time.ElapsedTime, deltaTime));

            this.didPushTime = true;
            return true;
        }
    }
}
