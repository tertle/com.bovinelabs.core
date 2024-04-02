// <copyright file="UnsafeQueueExtension.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Collections;

    public static class UnsafeQueueExtension
    {
        public static UnsafeQueue<T>.ParallelWriter AsParallelWriter<T>(this UnsafeQueue<T> queue, int threadIndex)
            where T : unmanaged
        {
            var parallelWriter = queue.AsParallelWriter();
            parallelWriter.m_ThreadIndex = threadIndex;
            return parallelWriter;
        }
    }
}
