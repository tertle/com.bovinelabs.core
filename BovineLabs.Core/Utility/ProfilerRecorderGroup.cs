namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Profiling;

    public sealed class ProfilerRecorderGroup : IDisposable
    {
        private readonly ProfilerRecorder[] recorders;
        private bool disposed;

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
