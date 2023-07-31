// <copyright file="TimerTriggerResetJob.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Model
{
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [BurstCompile]
    public unsafe struct TimerTriggerResetJob<T> : IJobChunk
        where T : unmanaged, IComponentData
    {
        public ComponentTypeHandle<T> RemainingHandle;

        [ReadOnly]
        [NativeDisableUnsafePtrRestriction]
        public void* Zeros;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            // RO so we don't trigger filter unless required
            var remainings = chunk.GetRequiredComponentDataPtrRO(ref this.RemainingHandle);

            var length = UnsafeUtility.SizeOf<float>() * chunk.Count;
            if (UnsafeUtility.MemCmp(remainings, this.Zeros, length) != 0)
            {
                chunk.SetChangeFilter(ref this.RemainingHandle);
            }
        }
    }
}
