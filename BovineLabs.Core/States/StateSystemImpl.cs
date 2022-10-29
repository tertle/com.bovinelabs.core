// <copyright file="StateSystemImpl.cs" company="BovineLabs">
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
    public struct StateSystemImpl
    {
        private readonly EntityQuery query;

        private EntityTypeHandle entityHandle;
        private DynamicComponentTypeHandle stateHandle;
        private DynamicComponentTypeHandle previousStateHandle;

        private NativeParallelHashMap<byte, ComponentType> registeredStatesMap;

        public StateSystemImpl(ref SystemState state, ComponentType stateComponent, ComponentType previousStateComponent)
        {
            this.query = state.GetEntityQuery(ComponentType.ReadOnly(stateComponent.TypeIndex), ComponentType.ReadWrite(previousStateComponent.TypeIndex));
            this.query.AddChangedVersionFilter(stateComponent);

            this.entityHandle = state.GetEntityTypeHandle();
            this.stateHandle = state.GetDynamicComponentTypeHandle(stateComponent);
            this.previousStateHandle = state.GetDynamicComponentTypeHandle(previousStateComponent);

            Assert.AreEqual(UnsafeUtility.SizeOf<byte>(), TypeManager.GetTypeInfo(stateComponent.TypeIndex).ElementSize);

            this.registeredStatesMap = new NativeParallelHashMap<byte, ComponentType>(256, Allocator.Persistent);

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
                }
                .ScheduleParallel(this.query, state.Dependency);
        }

        [BurstCompile]
        private struct StateJob : IJobChunk
        {
            [ReadOnly]
            public NativeParallelHashMap<byte, ComponentType> RegisteredStates;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            [ReadOnly]
            public DynamicComponentTypeHandle StateType;

            public DynamicComponentTypeHandle PreviousStateType;

            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            /// <inheritdoc/>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(this.EntityType);
                var states = chunk.GetDynamicComponentDataArrayReinterpret<byte>(this.StateType, UnsafeUtility.SizeOf<byte>());
                var previousStates = chunk.GetDynamicComponentDataArrayReinterpret<byte>(this.PreviousStateType, UnsafeUtility.SizeOf<byte>());

                for (var i = 0; i < states.Length; i++)
                {
                    ref readonly var entity = ref entities.ElementAtRO(i);
                    ref readonly var state = ref states.ElementAtRO(i);
                    ref var previous = ref previousStates.ElementAt(i);

                    if (state == previous)
                    {
                        return;
                    }

                    if (previous != 0)
                    {
                        if (this.RegisteredStates.TryGetValue(previous, out var stateComponent))
                        {
                            this.CommandBuffer.RemoveComponent(unfilteredChunkIndex, entity, stateComponent);
                        }
                    }

                    if (state != 0)
                    {
                        if (this.RegisteredStates.TryGetValue(state, out var stateComponent))
                        {
                            this.CommandBuffer.AddComponent(unfilteredChunkIndex, entity, stateComponent);
                        }
                        else
                        {
                            Debug.LogWarning($"State {state} not setup");
                        }
                    }

                    previous = state;
                    previousStates[i] = previous;
                }
            }
        }
    }
}
