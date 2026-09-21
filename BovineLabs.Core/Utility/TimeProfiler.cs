namespace BovineLabs.Core.Utility
{
    using System;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Profiling.LowLevel.Unsafe;
    using UnityEngine;

    public readonly struct TimeProfiler : IDisposable
    {
#if UNITY_EDITOR
        private readonly long _startTime;
        private readonly FixedString64Bytes _text;
        private readonly LogLevel _level;
        private readonly int _min;

        private TimeProfiler(long startTime, FixedString64Bytes text, LogLevel level, int min)
        {
            _startTime = startTime;
            _text = text;
            _level = level;
            _min = min;
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
            if (_startTime == 0)
            {
                return;
            }

            var elapsed = ProfilerUnsafeUtility.Timestamp - _startTime;
            var elapsedMs = GetElapsedMilliseconds(elapsed);

            if (elapsedMs >= _min)
            {
                BLGlobalLogger.Log128($"{_text}: {elapsedMs}ms", _level);
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
