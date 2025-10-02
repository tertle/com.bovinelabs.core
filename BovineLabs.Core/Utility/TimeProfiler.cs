// <copyright file="TimeProfiler.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Profiling.LowLevel.Unsafe;
    using UnityEngine;

    /// <summary> A simple scoped timer restricted by <see cref="LogLevel"/> that is easily stripped outside the editor. </summary>
    public readonly struct TimeProfiler : IDisposable
    {
#if UNITY_EDITOR
        private readonly long startTime;
        private readonly FixedString64Bytes text;
        private readonly LogLevel level;
        private readonly int min;

        private TimeProfiler(long startTime, FixedString64Bytes text, LogLevel level, int min)
        {
            this.startTime = startTime;
            this.text = text;
            this.level = level;
            this.min = min;
        }
#endif

        public static TimeProfiler Start(FixedString64Bytes text, LogLevel logLevel = LogLevel.Verbose)
        {
#if UNITY_EDITOR
            return logLevel < BLLogger.Level ? default : new TimeProfiler(ProfilerUnsafeUtility.Timestamp, text, logLevel, 0);
#else
            return default;
#endif
        }

        public static TimeProfiler StartWithMin(FixedString64Bytes text, int min, LogLevel logLevel = LogLevel.Verbose)
        {
#if UNITY_EDITOR
            return logLevel < BLLogger.Level ? default : new TimeProfiler(ProfilerUnsafeUtility.Timestamp, text, logLevel, min);
#else
            return default;
#endif
        }

        public static TimeProfiler StartString(string text, LogLevel logLevel = LogLevel.Verbose)
        {
#if UNITY_EDITOR
            return logLevel < BLLogger.Level ? default : new TimeProfiler(ProfilerUnsafeUtility.Timestamp, text.ToFixedString64NoError(), logLevel, 0);
#else
            return default;
#endif
        }

        public static TimeProfiler StartStringWithMin(string text, int min, LogLevel logLevel = LogLevel.Verbose)
        {
#if UNITY_EDITOR
            return logLevel < BLLogger.Level ? default : new TimeProfiler(ProfilerUnsafeUtility.Timestamp, text.ToFixedString64NoError(), logLevel, min);
#else
            return default;
#endif
        }

        [HideInCallstack]
        public void Dispose()
        {
#if UNITY_EDITOR
            if (this.startTime == 0)
            {
                return;
            }

            var elapsed = ProfilerUnsafeUtility.Timestamp - this.startTime;
            var elapsedMs = GetElapsedMilliseconds(elapsed);

            if (elapsedMs >= this.min)
            {
                BLGlobalLogger.Log128($"{this.text}: {elapsedMs}ms", this.level);
            }
#endif
        }

#if UNITY_EDITOR
        private static long GetElapsedMilliseconds(long elapsed)
        {
            const int nanoSecondsPerMilliSecond = 1_000_000;
            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            return elapsed * conversionRatio.Numerator / conversionRatio.Denominator / nanoSecondsPerMilliSecond;
        }
#endif
    }
}
