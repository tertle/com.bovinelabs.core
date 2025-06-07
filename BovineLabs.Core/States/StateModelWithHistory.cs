// <copyright file="StateModelWithHistory.cs" company="BovineLabs">
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

    /// <summary> A generic general purpose state system that ensures only a single state component exists on an entity but driven from a byte field. </summary>
    public struct StateModelWithHistory
    {
        private readonly int maxHistorySize;
        private StateImpl impl;
        private DynamicComponentTypeHandle historyBackType;
        private DynamicComponentTypeHandle historyForwardType;

        public StateModelWithHistory(
            ref SystemState state, ComponentType stateComponent, ComponentType previousStateComponent, ComponentType historyBackComponent,
            ComponentType historyForwardComponent, int maxHistorySize)
        {
            Assert.AreEqual(UnsafeUtility.SizeOf<byte>(), TypeManager.GetTypeInfo(stateComponent.TypeIndex).ElementSize);
            Assert.AreEqual(UnsafeUtility.SizeOf<byte>(), TypeManager.GetTypeInfo(previousStateComponent.TypeIndex).ElementSize);
            Assert.AreEqual(UnsafeUtility.SizeOf<byte>(), TypeManager.GetTypeInfo(historyBackComponent.TypeIndex).ElementSize);
            Assert.AreEqual(UnsafeUtility.SizeOf<byte>(), TypeManager.GetTypeInfo(historyForwardComponent.TypeIndex).ElementSize);

            Assert.IsTrue(maxHistorySize > 0, "Can't have a history <= 0");

            this.historyBackType = state.GetDynamicComponentTypeHandle(historyBackComponent);
            this.historyForwardType = state.GetDynamicComponentTypeHandle(historyForwardComponent);

            this.maxHistorySize = maxHistorySize;

            this.impl = new StateImpl(ref state, stateComponent, previousStateComponent);
        }

        public void Dispose(ref SystemState state)
        {
            state.Dependency.Complete();
            this.impl.Dispose();
        }

        public void Run(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            state.Dependency.Complete();
            var job = this.UpdateInternal(ref state, commandBuffer.AsParallelWriter());
            job.RunByRef(this.impl.Query);
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

            this.historyBackType.Update(ref state);
            this.historyForwardType.Update(ref state);

            return new StateJob
            {
                RegisteredStates = this.impl.RegisteredStatesMap,
                EntityType = this.impl.EntityType,
                StateType = this.impl.StateType,
                PreviousStateType = this.impl.PreviousStateType,
                HistoryBackType = this.historyBackType,
                HistoryForwardType = this.historyForwardType,
                CommandBuffer = commandBuffer,
                Logger = this.impl.Logger,
                MaxHistory = this.maxHistorySize,
            };
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

            public DynamicComponentTypeHandle HistoryBackType;

            public DynamicComponentTypeHandle HistoryForwardType;

            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public BLLogger Logger;

            public int MaxHistory;

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(this.EntityType);
                var states = chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref this.StateType, UnsafeUtility.SizeOf<byte>());
                var previousStates = chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref this.PreviousStateType, UnsafeUtility.SizeOf<byte>());

                var historyBack = chunk.GetDynamicBufferAccessor(ref this.HistoryBackType);
                var historyForward = chunk.GetDynamicBufferAccessor(ref this.HistoryForwardType);

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
                            this.Logger.LogWarning($"State {state} not setup");
                        }
                    }

                    var back = historyBack.GetBuffer<byte>(i);
                    var forward = historyForward.GetBuffer<byte>(i);

                    // Was it a Pop operation to a previous state
                    if (back.Length > 0 && back[^1] == state)
                    {
                        // Limit capacity
                        if (forward.Length == this.MaxHistory)
                        {
                            forward.RemoveAt(0);
                        }

                        forward.Add(previous);
                        back.RemoveAt(back.Length - 1);
                    }
                    else
                    {
                        if (forward.Length > 0)
                        {
                            if (forward[^1] == state)
                            {
                                // If the last forward state is the new state we have stepped forward so remove it
                                forward.RemoveAt(forward.Length - 1);
                            }
                            else
                            {
                                // Otherwise we are entering a new state and the forward history is now garbage
                                forward.Clear();
                            }
                        }

                        // Limit capacity
                        if (back.Length == this.MaxHistory)
                        {
                            back.RemoveAt(0);
                        }

                        back.Add(previous);
                    }

                    previous = state;
                }
            }
        }
    }
}
