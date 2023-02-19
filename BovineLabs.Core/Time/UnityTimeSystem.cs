// <copyright file="UnityTimeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Time
{
    using Unity.Entities;
    using UnityEngine;

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct UnityTimeSystem : ISystem
    {
        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<UnityTime>(state.SystemHandle);
            Set(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            Set(ref state);
        }

        private static void Set(ref SystemState state)
        {
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
            });
        }
    }
}
