// <copyright file="StatefulTriggerEventSystem.cs" company="BovineLabs">
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

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct StatefulTriggerEventSystem : ISystem
    {
        private StatefulEventImpl<StatefulTriggerEvent, StatefulTriggerEventContainer, CollectTriggerEvents, WriteEventsJob> impl;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.impl.OnCreate(ref state, 1);
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

            var triggerEvents = simulation.TriggerEvents;
            ref var eventReader = ref UnsafeUtility.As<TriggerEvents, NativeStream>(ref triggerEvents);

            this.impl.OnUpdate(ref state, eventReader, default, default);
        }

        internal struct CollectTriggerEvents : ICollectsEventsImpl<StatefulTriggerEvent, StatefulTriggerEventContainer>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public StatefulTriggerEventContainer Read(ref NativeStream.Reader reader)
            {
                var triggerEventData = reader.Read<TriggerEventData>();
                var triggerEvent = triggerEventData.CreateTriggerEvent();
                return new StatefulTriggerEventContainer(triggerEvent);
            }
        }

        [BurstCompile]
        private unsafe struct WriteEventsJob : IWriteJob<StatefulTriggerEvent, StatefulTriggerEventContainer>
        {
            private BufferTypeHandle<StatefulTriggerEvent> statefulNewEventHandle;

            [ReadOnly]
            private EntityTypeHandle entityHandle;

            [ReadOnly]
            private NativeMultiHashMap<Entity, StatefulTriggerEventContainer> currentEventMap;

            [ReadOnly]
            private NativeMultiHashMap<Entity, StatefulTriggerEventContainer> previousEventMap;

            [ReadOnly]
            private NativeHashSet<StatefulTriggerEventContainer> currentEvents;

            [ReadOnly]
            private NativeHashSet<StatefulTriggerEventContainer> previousEvents;

            public BufferTypeHandle<StatefulTriggerEvent> StatefulNewEventHandle
            {
                set => this.statefulNewEventHandle = value;
            }

            public EntityTypeHandle EntityHandle
            {
                set => this.entityHandle = value;
            }

            public NativeMultiHashMap<Entity, StatefulTriggerEventContainer> CurrentEventMap
            {
                set => this.currentEventMap = value;
            }

            public NativeMultiHashMap<Entity, StatefulTriggerEventContainer> PreviousEventMap
            {
                set => this.previousEventMap = value;
            }

            public NativeHashSet<StatefulTriggerEventContainer> CurrentEvents
            {
                set => this.currentEvents = value;
            }

            public NativeHashSet<StatefulTriggerEventContainer> PreviousEvents
            {
                set => this.previousEvents = value;
            }

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var statefulNewEventAccessor = chunk.GetBufferAccessorRO(ref this.statefulNewEventHandle);
                var entities = chunk.GetEntityDataPtrRO(this.entityHandle);

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
                            statefulNewEvents.Add(currentEvent.Create(entity, state));
                        }
                        while (this.currentEventMap.TryGetNextValue(out currentEvent, ref it));
                    }

                    if (this.previousEventMap.TryGetFirstValue(entity, out var previousEvent, out it))
                    {
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
                }

                if (changed)
                {
                    chunk.SetChangeFilter(ref this.statefulNewEventHandle);
                }
            }
        }
    }
}
#endif
