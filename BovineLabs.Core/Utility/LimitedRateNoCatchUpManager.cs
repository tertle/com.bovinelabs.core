namespace BovineLabs.Core.Utility
{
    using Unity.Core;
    using Unity.Entities;

    public class LimitedRateNoCatchUpManager : IRateManager
    {
        private bool _didPushTime;
        private double _lastPushedTime;

        public LimitedRateNoCatchUpManager(float defaultFixedTimestep)
        {
            Timestep = defaultFixedTimestep;
        }

        public float Timestep { get; set; }

        public bool ShouldGroupUpdate(ComponentSystemGroup group)
        {
            // Already pushed this frame
            if (_didPushTime)
            {
                group.World.PopTime();
                _didPushTime = false;
                return false;
            }

            var deltaTime = (float)(group.World.Unmanaged.Time.ElapsedTime - _lastPushedTime);

            // Not enough time elapsed
            if (group.World.Unmanaged.Time.ElapsedTime - _lastPushedTime < Timestep)
            {
                return false;
            }

            _lastPushedTime = group.World.Unmanaged.Time.ElapsedTime;

            group.World.PushTime(new TimeData(group.World.Unmanaged.Time.ElapsedTime, deltaTime));

            _didPushTime = true;
            return true;
        }
    }
}
