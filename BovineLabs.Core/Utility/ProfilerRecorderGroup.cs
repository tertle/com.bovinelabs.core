// <copyright file="ProfilerRecorderGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Profiling;

    /// <summary>Owns one or more profiler recorders and exposes their latest values as a sum.</summary>
    public sealed class ProfilerRecorderGroup : IDisposable
    {
        private readonly ProfilerRecorder[] recorders;
        private bool disposed;

        /// <summary>Initializes a new instance of the <see cref="ProfilerRecorderGroup"/> class.</summary>
        /// <param name="category">Profiler category shared by the counters.</param>
        /// <param name="counterNames">Names of the counters to aggregate.</param>
        public ProfilerRecorderGroup(ProfilerCategory category, params string[] counterNames)
        {
            if (counterNames == null)
            {
                throw new ArgumentNullException(nameof(counterNames));
            }

            if (counterNames.Length == 0)
            {
                throw new ArgumentException("At least one profiler counter name is required.", nameof(counterNames));
            }

            this.recorders = new ProfilerRecorder[counterNames.Length];

            try
            {
                for (var i = 0; i < this.recorders.Length; i++)
                {
                    this.recorders[i] = ProfilerRecorder.StartNew(category, counterNames[i], 1);
                }
            }
            catch
            {
                this.Dispose();
                throw;
            }
        }

        /// <summary>Gets whether every configured profiler counter is available.</summary>
        public bool Valid
        {
            get
            {
                if (this.disposed)
                {
                    return false;
                }

                foreach (var recorder in this.recorders)
                {
                    if (!recorder.Valid)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Gets the sum of the latest available counter samples.</summary>
        public long LastValue
        {
            get
            {
                var value = 0L;
                foreach (var recorder in this.recorders)
                {
                    if (recorder.Valid && recorder.Count > 0)
                    {
                        value += recorder.LastValue;
                    }
                }

                return value;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            for (var i = 0; i < this.recorders.Length; i++)
            {
                if (this.recorders[i].Valid)
                {
                    this.recorders[i].Dispose();
                }
            }
        }
    }
}
