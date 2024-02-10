// <copyright file="UnityTimeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TIME
namespace BovineLabs.Core.Time
{
    using Unity.Entities;
    using Unity.IntegerTime;
    using UnityEngine;

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation | Worlds.Service)]
    public partial struct UnityTimeSystem : ISystem
    {
        private long systemTicks;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<UnityTime>(state.SystemHandle);
            this.systemTicks = System.DateTime.Now.Ticks;
            this.Set(ref state);
        }

        /// <inheritdoc/>
        public void OnUpdate(ref SystemState state)
        {
            this.Set(ref state);
        }

        private void Set(ref SystemState state)
        {
            var ticks = System.DateTime.Now.Ticks;
            var deltaTicks = ticks - this.systemTicks;
            this.systemTicks = ticks;

            state.EntityManager.SetComponentData(state.SystemHandle, new UnityTime
            {
                FrameCount = Time.frameCount,
                TimeScale = Time.timeScale,
                DeltaTime = Time.deltaTime,
                SmoothDeltaTime = Time.smoothDeltaTime,
                UnscaledDeltaTime = Time.unscaledDeltaTime,
                Time = Time.time,
                UnscaledTime = Time.unscaledTime,
                RealTimeSinceStartup = Time.realtimeSinceStartup,
                TimeSinceLevelLoad = Time.timeSinceLevelLoad,
                Ticks = deltaTicks * DiscreteTime.TicksPerSecond / System.TimeSpan.TicksPerSecond,
            });
        }
    }
}
#endif
