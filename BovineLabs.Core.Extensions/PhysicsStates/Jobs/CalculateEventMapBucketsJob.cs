// <copyright file="CalculateEventMapBucketsJob.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    [BurstCompile]
    internal struct CalculateEventMapBucketsJob<T, TC> : IJob
        where T : unmanaged, IBufferElementData
        where TC : unmanaged, IEventContainer<T, TC>
    {
        public NativeMultiHashMap<Entity, TC> CurrentEventMap;

        public void Execute()
        {
            this.CurrentEventMap.RecalculateBuckets();
        }
    }
}
#endif
