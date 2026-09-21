namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Profiling;

    public sealed class ProfilerRecorderGroup : IDisposable
    {
        private readonly ProfilerRecorder[] _recorders;
        private bool _disposed;

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

            _recorders = new ProfilerRecorder[counterNames.Length];

            try
            {
                for (var i = 0; i < _recorders.Length; i++)
                {
                    _recorders[i] = ProfilerRecorder.StartNew(category, counterNames[i], 1);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public bool Valid
        {
            get
            {
                if (_disposed)
                {
                    return false;
                }

                foreach (var recorder in _recorders)
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
                foreach (var recorder in _recorders)
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
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (var i = 0; i < _recorders.Length; i++)
            {
                if (_recorders[i].Valid)
                {
                    _recorders[i].Dispose();
                }
            }
        }
    }
}
