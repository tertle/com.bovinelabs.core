// <copyright file="StatefulCollisionEventSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Physics;
    using Unity.Physics.Systems;
    using ContactPoint = Unity.Physics.ContactPoint;

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct StatefulCollisionEventSystem : ISystem
    {
        private StatefulEventImpl<StatefulCollisionEvent, StatefulCollisionEventContainer, CollectCollisionEvents, WriteEventsJob> impl;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.impl.OnCreate(ref state, 2);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            this.impl.OnDestroy();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            if (simulationSingleton.Type == SimulationType.NoPhysics)
            {
                return;
            }

            var simulation = simulationSingleton.AsSimulation();
            SafetyChecks.CheckSimulationStageAndThrow(simulation.m_SimulationScheduleStage, SimulationScheduleStage.Idle);
            if (!simulation.ReadyForEventScheduling)
            {
                return;
            }

            var collisionEvents = simulation.CollisionEvents;
            ref var access = ref UnsafeUtility.As<CollisionEvents, CollisionEventWrapper>(ref collisionEvents);
            var collector = new CollectCollisionEvents(access.TimeStep, access.InputVelocities);
            var job = new WriteEventsJob
            {
                StatefulCollisionEventDetailsHandle = SystemAPI.GetComponentTypeHandle<StatefulCollisionEventDetails>(),
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
            };

            this.impl.OnUpdate(ref state, access.EventDataStream, collector, job);
        }

        internal readonly unsafe struct CollectCollisionEvents : ICollectsEventsImpl<StatefulCollisionEvent, StatefulCollisionEventContainer>
        {
            private readonly float timeStep;

            [ReadOnly]
            private readonly NativeArray<Velocity> inputVelocities;

            public CollectCollisionEvents(float timeStep, NativeArray<Velocity> inputVelocities)
            {
                this.timeStep = timeStep;
                this.inputVelocities = inputVelocities;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public StatefulCollisionEventContainer Read(ref NativeStream.Reader reader)
            {
                var currentSize = reader.Read<int>();
                var collisionEventData = (CollisionEventData*)reader.ReadUnsafePtr(currentSize);
                var collisionEvent = collisionEventData->CreateCollisionEvent(this.timeStep, this.inputVelocities);
                return new StatefulCollisionEventContainer(collisionEvent);
            }
        }

        [BurstCompile]
        private unsafe struct WriteEventsJob : IWriteJob<StatefulCollisionEvent, StatefulCollisionEventContainer>
        {
            [ReadOnly]
            public ComponentTypeHandle<StatefulCollisionEventDetails> StatefulCollisionEventDetailsHandle;

            [ReadOnly]
            public PhysicsWorld PhysicsWorld;

            private BufferTypeHandle<StatefulCollisionEvent> statefulNewEventHandle;

            [ReadOnly]
            private EntityTypeHandle entityHandle;

            [ReadOnly]
            private NativeMultiHashMap<Entity, StatefulCollisionEventContainer> currentEventMap;

            [ReadOnly]
            private NativeMultiHashMap<Entity, StatefulCollisionEventContainer> previousEventMap;

            [ReadOnly]
            private NativeHashSet<StatefulCollisionEventContainer> currentEvents;

            [ReadOnly]
            private NativeHashSet<StatefulCollisionEventContainer> previousEvents;

            [NativeDisableContainerSafetyRestriction]
            private NativeList<ContactPoint> contactPoints;

            private interface IDetails
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                StatefulCollisionEvent.Details GetDetails(ref PhysicsWorld physicsWorld, in CollisionEvent collisionEvent);
            }

            public BufferTypeHandle<StatefulCollisionEvent> StatefulNewEventHandle
            {
                set => this.statefulNewEventHandle = value;
            }

            public EntityTypeHandle EntityHandle
            {
                set => this.entityHandle = value;
            }

            public NativeMultiHashMap<Entity, StatefulCollisionEventContainer> CurrentEventMap
            {
                set => this.currentEventMap = value;
            }

            public NativeMultiHashMap<Entity, StatefulCollisionEventContainer> PreviousEventMap
            {
                set => this.previousEventMap = value;
            }

            public NativeHashSet<StatefulCollisionEventContainer> CurrentEvents
            {
                set => this.currentEvents = value;
            }

            public NativeHashSet<StatefulCollisionEventContainer> PreviousEvents
            {
                set => this.previousEvents = value;
            }

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var writeDetails = chunk.Has(ref this.StatefulCollisionEventDetailsHandle);

                var statefulNewEventAccessor = chunk.GetBufferAccessorRO(ref this.statefulNewEventHandle);
                var entities = chunk.GetEntityDataPtrRO(this.entityHandle);

                bool changed;

                if (writeDetails)
                {
                    if (!this.contactPoints.IsCreated)
                    {
                        this.contactPoints = new NativeList<ContactPoint>(1, Allocator.Temp);
                    }

                    changed = this.IterateEvents(chunk, statefulNewEventAccessor, entities, new WithDetails(this.contactPoints));
                }
                else
                {
                    changed = this.IterateEvents(chunk, statefulNewEventAccessor, entities, default(NoDetails));
                }

                if (changed)
                {
                    chunk.SetChangeFilter(ref this.statefulNewEventHandle);
                }
            }

            private bool IterateEvents<T>(
                in ArchetypeChunk chunk, in BufferAccessor<StatefulCollisionEvent> statefulNewEventAccessor, Entity* entities, T details)
                where T : IDetails
            {
                var changed = false;

                for (var i = 0; i < chunk.Count; i++)
                {
                    var statefulNewEvents = statefulNewEventAccessor[i];
                    var entity = entities[i];

                    if (this.currentEventMap.TryGetFirstValue(entity, out var currentEvent, out var it))
                    {
                        changed = true;
                        do
                        {
                            var state = this.previousEvents.Contains(currentEvent) ? StatefulEventState.Stay : StatefulEventState.Enter;

                            var statefulCollisionEvent = currentEvent.Create(entity, state);
                            statefulCollisionEvent.CollisionDetails = details.GetDetails(ref this.PhysicsWorld, currentEvent.CollisionEvent);
                            statefulNewEvents.Add(statefulCollisionEvent);
                        }
                        while (this.currentEventMap.TryGetNextValue(out currentEvent, ref it));
                    }

                    if (!this.previousEventMap.TryGetFirstValue(entity, out var previousEvent, out it))
                    {
                        continue;
                    }

                    do
                    {
                        if (this.currentEvents.Contains(previousEvent))
                        {
                            // Already handled by looping the currentEventMap
                            continue;
                        }

                        changed = true;
                        statefulNewEvents.Add(previousEvent.Create(entity, StatefulEventState.Exit));
                    }
                    while (this.currentEventMap.TryGetNextValue(out previousEvent, ref it));
                }

                return changed;
            }

            private static CollisionEvent.Details CalculateDetails(
                ref PhysicsWorld physicsWorld, NativeList<ContactPoint> contactPoints, in CollisionEvent collisionEvent)
            {
                var eventData = collisionEvent.EventData;
                var numContactPoints = eventData.Value.NumNarrowPhaseContactPoints;

                contactPoints.ResizeUninitialized(numContactPoints);

                var contactPointsArray = contactPoints.AsArray();
                for (var i = 0; i < numContactPoints; i++)
                {
                    contactPointsArray[i] = eventData.Value.AccessContactPoint(i);
                }

                return eventData.Value.CalculateDetails(ref physicsWorld, collisionEvent.TimeStep, collisionEvent.InputVelocityA, collisionEvent.InputVelocityB,
                    contactPointsArray);
            }

            private readonly struct NoDetails : IDetails
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public StatefulCollisionEvent.Details GetDetails(ref PhysicsWorld physicsWorld, in CollisionEvent collisionEvent)
                {
                    return default;
                }
            }

            private readonly struct WithDetails : IDetails
            {
                private readonly NativeList<ContactPoint> contactPoints;

                public WithDetails(NativeList<ContactPoint> contactPoints)
                {
                    this.contactPoints = contactPoints;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public StatefulCollisionEvent.Details GetDetails(ref PhysicsWorld physicsWorld, in CollisionEvent collisionEvent)
                {
                    var details = CalculateDetails(ref physicsWorld, this.contactPoints, collisionEvent);

                    return new StatefulCollisionEvent.Details(details.EstimatedContactPointPositions.Length, details.EstimatedImpulse,
                        details.AverageContactPointPosition);
                }
            }
        }

        private struct CollisionEventWrapper
        {
            public readonly NativeStream EventDataStream;
            public readonly NativeArray<Velocity> InputVelocities;
            public readonly float TimeStep;
        }
    }
}
#endif
