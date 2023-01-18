// <copyright file="UnityTime.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Time
{
    using Unity.Entities;

    public struct UnityTime : IComponentData
    {
        public int FrameCount;

        public float TimeScale;

        public float DeltaTime;
        public float SmoothDeltaTime;
        public float UnscaledDeltaTime;

        public double Time;
        public double UnscaledTime;
        public double RealTimeSinceStartup;
        public double TimeSinceLevelLoad;
    }
}
