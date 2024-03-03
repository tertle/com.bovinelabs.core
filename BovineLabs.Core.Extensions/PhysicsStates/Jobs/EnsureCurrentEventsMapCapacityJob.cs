// <copyright file="EnsureCurrentEventsMapCapacityJob.cs" company="BovineLabs">
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
    internal struct EnsureCurrentEventsMapCapacityJob<T, TC> : IJob
        where T : unmanaged, IBufferElementData
        where TC : unmanaged, IEventContainer<T, TC>
    {
        [ReadOnly]
        public NativeStream.Reader Reader;

        public NativeMultiHashMap<Entity, TC> CurrentEventMap;

        public int EventsPerRead;

        public void Execute()
        {
            var capacity = this.Reader.Count() / this.EventsPerRead;

            this.CurrentEventMap.ClearLengthBuckets();

            // we write each event twice
            if (this.CurrentEventMap.Capacity < capacity * 2)
            {
                this.CurrentEventMap.Capacity = capacity * 2;
            }

            this.CurrentEventMap.SetCount(capacity * 2);
        }
    }
}
#endif
