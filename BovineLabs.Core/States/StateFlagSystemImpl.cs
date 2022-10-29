// <copyright file="StateFlagSystemImpl.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using BovineLabs.Core.Extensions;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using UnityEngine;

    /// <summary> A generic general purpose state system that ensures only a single state component exists on an entity but driven from a byte field. </summary>
    public struct StateFlagSystemImpl
    {
        private readonly EntityQuery query;
        private readonly byte stateLength;

        private EntityTypeHandle entityHandle;
        private DynamicComponentTypeHandle stateHandle;
        private DynamicComponentTypeHandle previousStateHandle;

        private NativeParallelHashMap<byte, ComponentType> registeredStatesMap;

        public StateFlagSystemImpl(ref SystemState state, ComponentType stateComponent, ComponentType previousStateComponent)
        {
            this.query = state.GetEntityQuery(ComponentType.ReadOnly(stateComponent.TypeIndex), ComponentType.ReadWrite(previousStateComponent.TypeIndex));
            this.query.AddChangedVersionFilter(stateComponent);

            this.entityHandle = state.GetEntityTypeHandle();
            this.stateHandle = state.GetDynamicComponentTypeHandle(stateComponent);
            this.previousStateHandle = state.GetDynamicComponentTypeHandle(previousStateComponent);

            this.registeredStatesMap = new NativeParallelHashMap<byte, ComponentType>(256, Allocator.Persistent);

            this.stateLength = (byte)TypeManager.GetTypeInfo(stateComponent.TypeIndex).ElementSize;
            Assert.AreEqual(this.stateLength, TypeManager.GetTypeInfo(previousStateComponent.TypeIndex).ElementSize);

            var stateSystems = StateSystemInstanceCache.GetAllStateSystems(ref state);

            foreach (var component in stateSystems)
            {
                if (component.Value.State != stateComponent.TypeIndex)
                {
                    continue;
                }

                var stateInstanceComponent = ComponentType.FromTypeIndex(component.Value.StateInstanceComponent);

                if (!this.registeredStatesMap.TryAdd(component.Value.StateKey, stateInstanceComponent))
                {
                    Debug.LogError($"System {component.GetType()} key {component.Value.StateKey} has already been registered");
                }
            }
        }

        public void Dispose()
        {
            this.registeredStatesMap.Dispose();
        }

        public void Update(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            this.entityHandle.Update(ref state);
            this.stateHandle.Update(ref state);
            this.previousStateHandle.Update(ref state);

            state.Dependency = new StateJob
                {
                    RegisteredStates = this.registeredStatesMap,
                    EntityType = this.entityHandle,
                    StateType = this.stateHandle,
                    PreviousStateType = this.previousStateHandle,
                    CommandBuffer = commandBuffer,
                    StateLength = this.stateLength,
                }
                .ScheduleParallel(this.query, state.Dependency);
        }

        [BurstCompile]
        private unsafe struct StateJob : IJobChunk
        {
            [ReadOnly]
            public NativeParallelHashMap<byte, ComponentType> RegisteredStates;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            [ReadOnly]
            public DynamicComponentTypeHandle StateType;

            public DynamicComponentTypeHandle PreviousStateType;

            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public byte StateLength;

            /// <inheritdoc/>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(this.EntityType);
                var states = (byte*)chunk.GetDynamicComponentDataArrayReinterpret<byte>(this.StateType, this.StateLength).GetUnsafeReadOnlyPtr();
                var previousStates = (byte*)chunk.GetDynamicComponentDataArrayReinterpret<byte>(this.PreviousStateType, this.StateLength).GetUnsafePtr();

                for (var i = 0; i < entities.Length; i++)
                {
                    ref readonly var entity = ref entities.ElementAtRO(i);

                    var stateI = states + i * this.StateLength;
                    var previousI = previousStates + i * this.StateLength;

                    for (byte j = 0; j < this.StateLength; j++)
                    {
                        var state = stateI[j];
                        var previous = previousI[j];

                        // S P | R A
                        // ---------
                        // 0 0 | 0 0
                        // 0 1 | 1 0
                        // 1 0 | 0 1
                        // 1 1 | 0 0
                        // ---------
                        // R = !S & P
                        // A = S & !P

                        var toRemove = ~state & previous;
                        var toAdd = state & ~previous;

                        for (byte r = 0; r < 8; r++)
                        {
                            var mask = (byte)(1 << r);
                            byte bit = (byte)(j * 8 + r);

                            if ((mask & toRemove) != 0)
                            {
                                if (this.RegisteredStates.TryGetValue(bit, out var stateComponent))
                                {
                                    this.CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, stateComponent);
                                }
                            }
                            else if ((mask & toAdd) != 0)
                            {
                                if (this.RegisteredStates.TryGetValue(bit, out var stateComponent))
                                {
                                    this.CommandBuffer.AddComponent(unfilteredChunkIndex, entity, stateComponent);
                                }
                                else
                                {
                                    Debug.LogWarning($"State {bit} not setup");
                                }
                            }
                        }

                        *(previousI + j) = state;

                    }
                }
            }
        }
    }
}
