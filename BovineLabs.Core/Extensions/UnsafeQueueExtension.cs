namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Internal;
    using Unity.Collections;

    public static class UnsafeQueueExtension
    {
        public static UnsafeQueue<T>.ParallelWriter AsParallelWriter<T>(this UnsafeQueue<T> queue, int threadIndex)
            where T : unmanaged
        {
            var parallelWriter = queue.AsParallelWriter();
            parallelWriter.GetThreadIndex() = threadIndex;
            return parallelWriter;
        }
    }
}
