// <copyright file="CalculateCurrentEventsBucketsJob.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    [BurstCompile]
    internal struct CalculateCurrentEventsBucketsJob<T, TC> : IJob
        where T : unmanaged, IBufferElementData
        where TC : unmanaged, IEventContainer<T, TC>
    {
        public NativeHashSet<TC> CurrentEvents;

        public void Execute()
        {
            this.CurrentEvents.RecalculateBuckets();
        }
    }
}
#endif
