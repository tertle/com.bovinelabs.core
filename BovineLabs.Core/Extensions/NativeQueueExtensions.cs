// <copyright file="NativeQueueExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Collections;

    public static unsafe class NativeQueueExtensions
    {
        public static bool IsCreated<T>(this NativeQueue<T>.ParallelWriter queue)
            where T : unmanaged
        {
            return queue.unsafeWriter.m_Buffer != null;
        }
    }
}
