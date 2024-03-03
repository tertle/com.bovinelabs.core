// <copyright file="CollectEventsJob.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Jobs;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    internal interface ICollectsEventsImpl<T, out TC>
        where T : unmanaged, IBufferElementData
        where TC : unmanaged, IEventContainer<T, TC>
    {
        TC Read(ref NativeStream.Reader reader);
    }

    [BurstCompile]
    internal unsafe struct CollectEventsJob<T, TC, TI> : IJobParallelForDeferBatch
        where T : unmanaged, IBufferElementData
        where TC : unmanaged, IEventContainer<T, TC>
        where TI : unmanaged, ICollectsEventsImpl<T, TC>
    {
        [ReadOnly]
        public NativeStream.Reader Reader;

        [NativeDisableParallelForRestriction]
        public NativeMultiHashMap<Entity, TC> CurrentEventMap;

        [NativeDisableParallelForRestriction]
        public NativeHashSet<TC> CurrentEvents;

        public TI EventCollector;

        public int EventsPerRead;

        public void Execute(int startIndex, int count)
        {
            var end = startIndex + count;

            var events = 0;

            for (var index = startIndex; index < end; index++)
            {
                events += this.Reader.BeginForEachIndex(index) / this.EventsPerRead;
            }

            if (events == 0)
            {
                return;
            }

            var statefulIndex = this.CurrentEvents.ReserveAtomicNoResize(events);
            var sKey = this.CurrentEvents.GetKeys();

            // * 2 as we write them both directions
            var listIndex = this.CurrentEventMap.ReserveAtomicNoResize(events * 2);

            var keys = this.CurrentEventMap.GetKeys();
            var values = this.CurrentEventMap.GetValues();

            for (var index = startIndex; index < end; index++)
            {
                this.Reader.BeginForEachIndex(index);

                while (this.Reader.RemainingItemCount > 0)
                {
                    var e = this.EventCollector.Read(ref this.Reader);

                    keys[listIndex] = e.EntityA;
                    values[listIndex] = e;
                    listIndex++;

                    keys[listIndex] = e.EntityB;
                    values[listIndex] = e;
                    listIndex++;

                    sKey[statefulIndex++] = e;
                }
            }
        }
    }
}
#endif
