// <copyright file="StateFlagSystemBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary> A generic general purpose state system that ensures only a single state component exists on an entity but driven from a byte field. </summary>
    /// <typeparam name="TSize"> The bit array type. </typeparam>
    /// <typeparam name="TComponent"> The state component. </typeparam>
    /// <typeparam name="TPrevious"> The previous state component. </typeparam>
    public abstract partial class StateFlagSystemBase<TSize, TComponent, TPrevious> : SystemBase
        where TSize : unmanaged, IBitArray<TSize>
        where TComponent : struct, IStateFlagComponent<TSize>
        where TPrevious : struct, IStateFlagPreviousComponent<TSize>
    {
        private NativeParallelHashMap<uint, ComponentType> registeredStatesMap;
        private EndSimulationEntityCommandBufferSystem bufferSystem;
        private EntityQuery query;
        private EntityQuery missingQuery;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.registeredStatesMap = new NativeParallelHashMap<uint, ComponentType>(256, Allocator.Persistent);
            this.bufferSystem = this.World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();

            this.missingQuery = this.GetEntityQuery(ComponentType.ReadOnly<TComponent>(), ComponentType.Exclude<TPrevious>());
            this.query = this.GetEntityQuery(ComponentType.ReadOnly<TComponent>(), ComponentType.ReadWrite<TPrevious>());
            this.query.AddChangedVersionFilter(typeof(TComponent));

            var systems = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => !t.IsInterface && !t.IsAbstract && t.IsClass)
                .Where(t =>
                {
                    var subClass = t.GetSubclassOfRawGeneric(typeof(StateFlagInstanceSystemBase<,>));
                    return subClass != null && subClass.GetGenericArguments()[1] == typeof(TComponent);
                });

            foreach (var systemType in systems)
            {
                var system = (IStateFlagInstanceSystem)this.World.GetExistingSystem(systemType);
                this.registeredStatesMap.Add(system.StateKey, system.StateInstanceComponent);
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
            internal NativeParallelHashMap<uint, ComponentType> RegisteredStates;

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
                    ref var previous = ref previousStates.ElementAt(i);

                    // S P | R A
                    // ---------
                    // 0 0 | 0 0
                    // 0 1 | 1 0
                    // 1 0 | 0 1
                    // 1 1 | 0 0
                    // ---------
                    // R = !S & P
                    // A = S & !P
                    var toRemove = state.Value.BitNot().BitAnd(previous.Value);
                    var toAdd = state.Value.BitAnd(previous.Value.BitNot());

                    for (uint r = 0; r < toRemove.Capacity; r++)
                    {
                        if (toRemove[r])
                        {
                            Debug.Assert(this.RegisteredStates.ContainsKey(r), $"Trying to remove state {r} that was not registered");
                            var stateComponent = this.RegisteredStates[r];
                            this.CommandBuffer.RemoveComponent(batchIndex, entity, stateComponent);
                        }
                    }

                    for (uint r = 0; r < toAdd.Capacity; r++)
                    {
                        if (toAdd[r])
                        {
                            Debug.Assert(this.RegisteredStates.ContainsKey(r), $"Trying to add state {r} that was not registered");
                            var stateComponent = this.RegisteredStates[r];
                            this.CommandBuffer.AddComponent(batchIndex, entity, stateComponent);
                        }
                    }

                    previous.Value = state.Value;
                }
            }
        }
    }
}
