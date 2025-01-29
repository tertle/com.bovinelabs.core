// <copyright file="IJobParallelForDeferExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Burst;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;

    public static class IJobParallelForDeferExtensions
    {
        public static unsafe JobHandle Schedule<T, U>(this T jobData, DynamicBuffer<U> list, int innerloopBatchCount, JobHandle dependsOn = default)
            where T : struct, IJobParallelForDefer
            where U : unmanaged
        {
            ref var intern = ref UnsafeUtility.As<DynamicBuffer<U>, DynamicBufferInternal>(ref list);
            var header = intern.Buffer;
            return jobData.Schedule(&header->Length, innerloopBatchCount, dependsOn);
        }

        private unsafe struct DynamicBufferInternal
        {
            [NativeDisableUnsafePtrRestriction]
            [NoAlias]
            public readonly BufferHeader* Buffer;
        }
    }
}
