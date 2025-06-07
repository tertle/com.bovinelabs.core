// <copyright file="ProfileTimer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Diagnostics;

    /// <summary> A simple editor only timer that is easily stripped/ignored. </summary>
    public readonly struct TimeProfiler : IDisposable
    {
#if UNITY_EDITOR
        private readonly Stopwatch stopwatch;
        private readonly string text;
        private readonly LogLevel level;

        public TimeProfiler(Stopwatch stopwatch, string text, LogLevel level)
        {
            this.stopwatch = stopwatch;
            this.text = text;
            this.level = level;
        }
#endif

        public static TimeProfiler Start(string text, LogLevel logLevel = LogLevel.Verbose)
        {
#if UNITY_EDITOR
            return logLevel < BLLogger.Level ? default : new TimeProfiler(Stopwatch.StartNew(), text, logLevel);
#else
            return default;
#endif
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            if (this.stopwatch == null)
            {
                return;
            }

            this.stopwatch.Stop();
            BLGlobalLogger.LogString($"{this.text}: {this.stopwatch.ElapsedMilliseconds}ms", this.level);
#endif
        }
    }
}
