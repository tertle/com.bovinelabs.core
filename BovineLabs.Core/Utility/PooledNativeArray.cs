namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Collections;

    /// <summary>
    /// A synchronous, same-thread lease over pooled list storage. Dispose the lease, not its array view, and do not copy the lease.
    /// </summary>
    public struct PooledNativeArray<T> : IDisposable
        where T : unmanaged
    {
        private PooledNativeList<T> _pool;

        public NativeArray<T> Array => _pool.List.AsArray();

        public static PooledNativeArray<T> Make(int length, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var result = new PooledNativeArray<T>
            {
                _pool = PooledNativeList<T>.Make(),
            };
            result._pool.List.Resize(length, options);
            return result;
        }

        public void Dispose()
        {
            _pool.Dispose();
        }
    }
}
