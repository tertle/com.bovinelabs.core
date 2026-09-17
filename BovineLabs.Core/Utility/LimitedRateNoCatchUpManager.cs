namespace BovineLabs.Core.Utility
{
    using Unity.Core;
    using Unity.Entities;

    public class LimitedRateNoCatchUpManager : IRateManager
    {
        private bool didPushTime;
        private double lastPushedTime;

        public LimitedRateNoCatchUpManager(float defaultFixedTimestep)
        {
            this.Timestep = defaultFixedTimestep;
        }

        public float Timestep { get; set; }

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
