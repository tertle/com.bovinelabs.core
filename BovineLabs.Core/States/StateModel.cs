// <copyright file="StateModel.cs" company="BovineLabs">
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
    public struct StateModel
    {
        private StateImpl impl;

        public StateModel(ref SystemState state, ComponentType stateComponent, ComponentType previousStateComponent)
        {
            Assert.AreEqual(UnsafeUtility.SizeOf<byte>(), TypeManager.GetTypeInfo(stateComponent.TypeIndex).ElementSize);
            Assert.AreEqual(UnsafeUtility.SizeOf<byte>(), TypeManager.GetTypeInfo(previousStateComponent.TypeIndex).ElementSize);

            this.impl = new StateImpl(ref state, stateComponent, previousStateComponent);
        }

        public void Dispose()
        {
            this.impl.Dispose();
        }

        public void Update(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            this.impl.Update(ref state);

            state.Dependency = new StateJob
                {
                    RegisteredStates = this.impl.RegisteredStatesMap,
                    EntityType = this.impl.EntityType,
                    StateType = this.impl.StateType,
                    PreviousStateType = this.impl.PreviousStateType,
                    CommandBuffer = commandBuffer,
                }
                .ScheduleParallel(this.impl.Query, state.Dependency);
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

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(this.EntityType);
                var states = chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref this.StateType, UnsafeUtility.SizeOf<byte>());
                var previousStates = chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref this.PreviousStateType, UnsafeUtility.SizeOf<byte>());

                for (var i = 0; i < states.Length; i++)
                {
                    ref readonly var entity = ref entities.ElementAtRO(i);
                    ref readonly var state = ref states.ElementAtRO(i);
                    ref var previous = ref previousStates.ElementAt(i);

                    if (state == previous)
                    {
                        continue;
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
