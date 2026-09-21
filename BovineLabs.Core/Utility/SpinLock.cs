namespace BovineLabs.Core.Utility
{
    using System.Runtime.CompilerServices;
    using System.Threading;

    // Taken from com.unity.collections\Unity.Collections\AllocatorManager.cs
    public struct SpinLock
    {
        private int _lock;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Acquire()
        {
            for (;;)
            {
                // Optimistically assume the lock is free on the first try.
                if (Interlocked.CompareExchange(ref _lock, 1, 0) == 0)
                {
                    return;
                }

                // Wait for lock to be released without generating cache misses.
                while (Volatile.Read(ref _lock) == 1)
                {
                }

                // Future improvement: the 'continue' instruction above could be swapped for a 'pause' intrinsic
                // instruction when the CPU supports it, to further reduce contention by reducing load-store unit
                // utilization. However, this would need to be optional because if you don't use hyper-threading
                // and you don't care about power efficiency, using the 'pause' instruction will slow down lock
                // acquisition in the contended scenario.
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAcquire()
        {
            // First do a memory load (read) to check if lock is free in order to prevent unnecessary cache misses.
            return Volatile.Read(ref _lock) == 0 && Interlocked.CompareExchange(ref _lock, 1, 0) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAcquire(bool spin)
        {
            if (spin)
            {
                Acquire();
                return true;
            }

            return TryAcquire();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            Volatile.Write(ref _lock, 0);
        }
    }
}
