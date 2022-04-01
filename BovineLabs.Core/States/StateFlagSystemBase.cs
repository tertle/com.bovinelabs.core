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
    /// <typeparam name="T"> The bit array type. </typeparam>
    /// <typeparam name="TS"> The state component. </typeparam>
    /// <typeparam name="TP"> The previous state component. </typeparam>
    public abstract partial class StateFlagSystemBase<T, TS, TP> : SystemBase
        where T : unmanaged, IBitArray<T>
        where TS : struct, IStateFlagComponent<T>
        where TP : struct, IStateFlagPreviousComponent<T>
    {
        private NativeHashMap<uint, ComponentType> registeredStatesMap;
        private EndSimulationEntityCommandBufferSystem bufferSystem;
        private EntityQuery query;
        private EntityQuery missingQuery;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.registeredStatesMap = new NativeHashMap<uint, ComponentType>(256, Allocator.Persistent);
            this.bufferSystem = this.World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            this.missingQuery = this.GetEntityQuery(ComponentType.ReadOnly<TS>(), ComponentType.Exclude<TP>());
            this.query = this.GetEntityQuery(ComponentType.ReadOnly<TS>(), ComponentType.ReadWrite<TP>());
            this.query.AddChangedVersionFilter(typeof(TS));

            var systems = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => !t.IsInterface && !t.IsAbstract && t.IsClass)
                .Where(t =>
                {
                    var subClass = t.GetSubclassOfRawGeneric(typeof(StateFlagInstanceSystemBase<,>));
                    return subClass != null && subClass.GetGenericArguments()[1] == typeof(TS);
                });

            foreach (var systemType in systems)
            {
                var system = (IStateFlagInstanceSystem)this.World.GetOrCreateSystem(systemType);
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
            this.EntityManager.AddComponent<TP>(this.missingQuery);

            if (this.query.IsEmpty)
            {
                return;
            }

            var stateJob = this.CreateStateJob();
            stateJob.RegisteredStates = this.registeredStatesMap;
            stateJob.EntityType = this.GetEntityTypeHandle();
            stateJob.StateType = this.GetComponentTypeHandle<TS>(true);
            stateJob.PreviousStateType = this.GetComponentTypeHandle<TP>();
            stateJob.CommandBuffer = this.bufferSystem.CreateCommandBuffer().AsParallelWriter();
            this.Dependency = stateJob.ScheduleParallel(this.query, this.Dependency);

            this.bufferSystem.AddJobHandleForProducer(this.Dependency);
        }

        [BurstCompile]
        protected struct StateJob : IJobEntityBatch
        {
            [ReadOnly]
            internal NativeHashMap<uint, ComponentType> RegisteredStates;

            [ReadOnly]
            internal EntityTypeHandle EntityType;

            [ReadOnly]
            internal ComponentTypeHandle<TS> StateType;

            internal ComponentTypeHandle<TP> PreviousStateType;

            internal EntityCommandBuffer.ParallelWriter CommandBuffer;

            /// <inheritdoc/>
            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var entities = batchInChunk.GetNativeArray(this.EntityType);
                var states = batchInChunk.GetNativeArray(this.StateType);
                var previousStates = batchInChunk.GetNativeArray(this.PreviousStateType);

                for (var i = 0; i < states.Length; i++)
                {
                    var entity = entities[i];
                    var state = states[i];
                    var previous = previousStates[i];

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
                    previousStates[i] = previous;
                }
            }
        }
    }
}