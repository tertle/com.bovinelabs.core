// <copyright file="EnsureCurrentEventsCapacityJob.cs" company="BovineLabs">
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
    internal struct EnsureCurrentEventsCapacityJob<T, TC> : IJob
        where T : unmanaged, IBufferElementData
        where TC : unmanaged, IEventContainer<T, TC>
    {
        [ReadOnly]
        public NativeStream.Reader Reader;

        public NativeHashSet<TC> CurrentEvents;
        public NativeReference<int> ForeachCount;

        public int EventsPerRead;

        public void Execute()
        {
            var capacity = this.Reader.Count() / this.EventsPerRead;
            this.ForeachCount.Value = this.Reader.ForEachCount;

            this.CurrentEvents.ClearLengthBuckets();

            if (this.CurrentEvents.Capacity < capacity)
            {
                this.CurrentEvents.Capacity = capacity;
            }

            this.CurrentEvents.SetCount(capacity);
        }
    }
}
#endif
