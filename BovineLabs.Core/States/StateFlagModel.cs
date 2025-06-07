// <copyright file="StateFlagModel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Utility;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> A generic general purpose state system that ensures only a single state component exists on an entity but driven from a byte field. </summary>
    public struct StateFlagModel
    {
        private readonly byte stateSize;
        private StateImpl impl;

        public StateFlagModel(ref SystemState state, ComponentType stateComponent, ComponentType previousStateComponent)
        {
            this.stateSize = (byte)TypeManager.GetTypeInfo(stateComponent.TypeIndex).ElementSize;
            Assert.AreEqual(this.stateSize, TypeManager.GetTypeInfo(previousStateComponent.TypeIndex).ElementSize);

            this.impl = new StateImpl(ref state, stateComponent, previousStateComponent);
        }

        public NativeParallelHashMap<byte, ComponentType>.ReadOnly States => this.impl.RegisteredStatesMap.AsReadOnly();

        public void Dispose(ref SystemState state)
        {
            state.Dependency.Complete();
            this.impl.Dispose();
        }

        public void Run(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            state.Dependency.Complete();
            var job = this.UpdateInternal(ref state, commandBuffer.AsParallelWriter());
            job.Run(this.impl.Query);
        }

        public void Update(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            var job = this.UpdateInternal(ref state, commandBuffer.AsParallelWriter());
            state.Dependency = job.ScheduleByRef(this.impl.Query, state.Dependency);
        }

        public void UpdateParallel(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            var job = this.UpdateInternal(ref state, commandBuffer);
            state.Dependency = job.ScheduleParallelByRef(this.impl.Query, state.Dependency);
        }

        private StateJob UpdateInternal(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            this.impl.Update(ref state);

            return new StateJob
            {
                RegisteredStates = this.impl.RegisteredStatesMap,
                EntityType = this.impl.EntityType,
                StateType = this.impl.StateType,
                PreviousStateType = this.impl.PreviousStateType,
                CommandBuffer = commandBuffer,
                Logger = this.impl.Logger,
                StateSize = this.stateSize,
            };
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

            public BLLogger Logger;

            public byte StateSize;

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(this.EntityType);
                var states = (byte*)chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref this.StateType, this.StateSize).GetUnsafeReadOnlyPtr();
                var previousStates = (byte*)chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref this.PreviousStateType, this.StateSize).GetUnsafePtr();

                for (var i = 0; i < entities.Length; i++)
                {
                    ref readonly var entity = ref entities.ElementAtRO(i);

                    var stateI = states + (i * this.StateSize);
                    var previousI = previousStates + (i * this.StateSize);

                    for (byte j = 0; j < this.StateSize; j++)
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
                            var bit = (byte)((j * 8) + r);

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
                                    this.Logger.LogWarning($"State {bit} not setup for type {TypeManagerEx.GetTypeName(this.StateType.m_TypeIndex)}");
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
