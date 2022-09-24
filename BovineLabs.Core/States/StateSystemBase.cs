// <copyright file="StateSystemBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary> A generic general purpose state system that ensures only a single state component exists on an entity but driven from a byte field. </summary>
    /// <typeparam name="TComponent"> The state component. </typeparam>
    /// <typeparam name="TPrevious"> The previous state component. </typeparam>
    public abstract partial class StateSystemBase<TComponent, TPrevious> : SystemBase
        where TComponent : struct, IStateComponent
        where TPrevious : struct, IStatePreviousComponent
    {
        private NativeParallelHashMap<byte, ComponentType> registeredStatesMap;
        private EndSimulationEntityCommandBufferSystem bufferSystem;
        private EntityQuery query;
        private EntityQuery missingQuery;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.registeredStatesMap = new NativeParallelHashMap<byte, ComponentType>(256, Allocator.Persistent);
            this.bufferSystem = this.World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();

            this.missingQuery = this.GetEntityQuery(ComponentType.ReadOnly<TComponent>(), ComponentType.Exclude<TPrevious>());
            this.query = this.GetEntityQuery(ComponentType.ReadOnly<TComponent>(), ComponentType.ReadWrite<TPrevious>());
            this.query.AddChangedVersionFilter(ComponentType.ReadOnly<TComponent>());

            var systems = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => !t.IsInterface && !t.IsAbstract && t.IsClass)
                .Where(t =>
                {
                    var subClass = t.GetSubclassOfRawGeneric(typeof(StateInstanceSystemBase<>));
                    return subClass != null && subClass.GetGenericArguments()[0] == typeof(TComponent);
                });

            foreach (var systemType in systems)
            {
                var system = (IStateInstanceSystem)this.World.GetExistingSystem(systemType);

                if (!this.registeredStatesMap.TryAdd(system.StateKey, system.StateInstanceComponent))
                {
                    Debug.LogError($"System {system.GetType()} key {system.StateKey} has already been registered");
                }
            }
        }

        /// <summary> Create the job struct for burst support. </summary>
        /// <remarks> You only need to return default. The struct is populated for you. </remarks>
        /// <returns> The job struct. </returns>
        protected abstract StateJob CreateStateJob();

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            this.registeredStatesMap.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            this.EntityManager.AddComponent<TPrevious>(this.missingQuery);

            var stateJob = this.CreateStateJob();
            stateJob.RegisteredStates = this.registeredStatesMap;
            stateJob.EntityType = this.GetEntityTypeHandle();
            stateJob.StateType = this.GetComponentTypeHandle<TComponent>(true);
            stateJob.PreviousStateType = this.GetComponentTypeHandle<TPrevious>();
            stateJob.CommandBuffer = this.bufferSystem.CreateCommandBuffer().AsParallelWriter();
            stateJob.LastSystemVersion = this.LastSystemVersion;
            this.Dependency = stateJob.ScheduleParallel(this.query, this.Dependency);

            this.bufferSystem.AddJobHandleForProducer(this.Dependency);
        }

        [BurstCompile]
        protected struct StateJob : IJobEntityBatch
        {
            [ReadOnly]
            internal NativeParallelHashMap<byte, ComponentType> RegisteredStates;

            [ReadOnly]
            internal EntityTypeHandle EntityType;

            [ReadOnly]
            internal ComponentTypeHandle<TComponent> StateType;

            internal ComponentTypeHandle<TPrevious> PreviousStateType;

            internal EntityCommandBuffer.ParallelWriter CommandBuffer;

            internal uint LastSystemVersion;

            /// <inheritdoc/>
            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                if (!batchInChunk.DidChange(this.StateType, this.LastSystemVersion))
                {
                    return;
                }

                var entities = batchInChunk.GetNativeArray(this.EntityType);
                var states = batchInChunk.GetNativeArray(this.StateType);
                var previousStates = batchInChunk.GetNativeArray(this.PreviousStateType);

                for (var i = 0; i < states.Length; i++)
                {
                    var entity = entities[i];
                    var state = states[i];
                    var previous = previousStates[i];

                    if (state.Value == previous.Value)
                    {
                        return;
                    }

                    // TODO potentially change this to new component filtering when Unity implements this instead of changing architecture
                    if (previous.Value != 0)
                    {
                        if (this.RegisteredStates.TryGetValue(previous.Value, out var stateComponent))
                        {
                            this.CommandBuffer.RemoveComponent(batchIndex, entity, stateComponent);
                        }
                    }

                    if (state.Value != 0)
                    {
                        if (this.RegisteredStates.TryGetValue(state.Value, out var stateComponent))
                        {
                            this.CommandBuffer.AddComponent(batchIndex, entity, stateComponent);
                        }
                        else
                        {
                            Debug.LogWarning($"State {state.Value} not setup");
                        }
                    }

                    previous.Value = state.Value;
                    previousStates[i] = previous;
                }
            }
        }
    }
}
