namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Internal;
    using Unity.Collections;

    public static unsafe class NativeQueueExtensions
    {
        public static bool IsCreated<T>(this NativeQueue<T>.ParallelWriter queue)
            where T : unmanaged
        {
            return queue.GetWriter().GetBuffer() != null;
        }
    }
}
