// <copyright file="StatefulEventImpl.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using BovineLabs.Core.Jobs;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    internal interface IWriteJob<T, TC> : IJobChunk
        where T : unmanaged, IBufferElementData
        where TC : unmanaged, IEventContainer<T, TC>
    {
        BufferTypeHandle<T> StatefulNewEventHandle { set; }

        EntityTypeHandle EntityHandle { set; }

        NativeMultiHashMap<Entity, TC> CurrentEventMap { set; }

        NativeMultiHashMap<Entity, TC> PreviousEventMap { set; }

        NativeHashSet<TC> CurrentEvents { set; }

        public NativeHashSet<TC> PreviousEvents { set; }
    }

    internal struct StatefulEventImpl<T, TC, TI, TW>
        where T : unmanaged, IBufferElementData
        where TC : unmanaged, IEventContainer<T, TC>
        where TI : unmanaged, ICollectsEventsImpl<T, TC>
        where TW : unmanaged, IWriteJob<T, TC>
    {
        private const int Capacity = 512;

        private NativeReference<int> foreachCount;

        private NativeMultiHashMap<Entity, TC> currentEventMap;
        private NativeMultiHashMap<Entity, TC> previousEventMap;

        private NativeHashSet<TC> currentEvents;
        private NativeHashSet<TC> previousEvents;

        private EntityQuery query;
        private BufferTypeHandle<T> statefulNewEventHandle;
        private EntityTypeHandle entityHandle;
        private int eventsPerRead;

        public void OnCreate(ref SystemState state, int numberOfEventsPerRead)
        {
            this.eventsPerRead = numberOfEventsPerRead;

            this.foreachCount = new NativeReference<int>(Allocator.Persistent);

            this.currentEventMap = new NativeMultiHashMap<Entity, TC>(Capacity * 2, Allocator.Persistent);
            this.previousEventMap = new NativeMultiHashMap<Entity, TC>(Capacity * 2, Allocator.Persistent);

            this.currentEvents = new NativeHashSet<TC>(Capacity, Allocator.Persistent);
            this.previousEvents = new NativeHashSet<TC>(Capacity, Allocator.Persistent);

            var builder = new EntityQueryBuilder(Allocator.Temp);
            this.query = builder.WithAll<T>().Build(ref state);

            this.statefulNewEventHandle = state.GetBufferTypeHandle<T>();
            this.entityHandle = state.GetEntityTypeHandle();
        }

        public void OnDestroy()
        {
            this.foreachCount.Dispose();
            this.currentEventMap.Dispose();
            this.previousEventMap.Dispose();
            this.currentEvents.Dispose();
            this.previousEvents.Dispose();
        }

        public void OnUpdate(ref SystemState state, NativeStream eventReader, TI eventCollector, TW job)
        {
            if (!eventReader.IsCreated)
            {
                return;
            }

            var capacity1 = new EnsureCurrentEventsCapacityJob<T, TC>
            {
                Reader = eventReader.AsReader(),
                CurrentEvents = this.currentEvents,
                ForeachCount = this.foreachCount,
                EventsPerRead = this.eventsPerRead,
            }.Schedule(state.Dependency);

            var capacity2 = new EnsureCurrentEventsMapCapacityJob<T, TC>
            {
                Reader = eventReader.AsReader(),
                CurrentEventMap = this.currentEventMap,
                EventsPerRead = this.eventsPerRead,
            }.Schedule(state.Dependency);

            state.Dependency = JobHandle.CombineDependencies(capacity1, capacity2);

            state.Dependency = new CollectEventsJob<T, TC, TI>
            {
                Reader = eventReader.AsReader(),
                CurrentEventMap = this.currentEventMap,
                CurrentEvents = this.currentEvents,
                EventCollector = eventCollector,
                EventsPerRead = this.eventsPerRead,
            }.ScheduleParallel(this.foreachCount, 64, state.Dependency);

            var buckets1 = new CalculateEventMapBucketsJob<T, TC> { CurrentEventMap = this.currentEventMap }.Schedule(state.Dependency);
            var buckets2 = new CalculateCurrentEventsBucketsJob<T, TC> { CurrentEvents = this.currentEvents }.Schedule(state.Dependency);

            state.Dependency = JobHandle.CombineDependencies(buckets1, buckets2);

            this.statefulNewEventHandle.Update(ref state);
            this.entityHandle.Update(ref state);

            job.StatefulNewEventHandle = this.statefulNewEventHandle;
            job.EntityHandle = this.entityHandle;
            job.CurrentEventMap = this.currentEventMap;
            job.PreviousEventMap = this.previousEventMap;
            job.CurrentEvents = this.currentEvents;
            job.PreviousEvents = this.previousEvents;
            state.Dependency = job.ScheduleParallel(this.query, state.Dependency);

            // Swap our current and previous maps for next frame
            (this.currentEventMap, this.previousEventMap) = (this.previousEventMap, this.currentEventMap);
            (this.currentEvents, this.previousEvents) = (this.previousEvents, this.currentEvents);
        }
    }
}
#endif
