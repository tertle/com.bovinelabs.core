// <copyright file="StateFlagModelWithHistory.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> A generic general purpose state system that ensures only a single state component exists on an entity but driven from a byte field. </summary>
    public struct StateFlagModelWithHistory
    {
        private readonly int stateSize;
        private readonly int historySize;
        private readonly int maxHistorySize;
        private StateImpl impl;
        private DynamicComponentTypeHandle historyBackType;
        private DynamicComponentTypeHandle historyForwardType;

        public StateFlagModelWithHistory(
            ref SystemState state, ComponentType stateComponent, ComponentType previousStateComponent, ComponentType historyBackComponent,
            ComponentType historyForwardComponent, int maxHistorySize)
        {
            this.stateSize = TypeManager.GetTypeInfo(stateComponent.TypeIndex).ElementSize;

            this.historySize = TypeManager.GetTypeInfo(historyBackComponent.TypeIndex).ElementSize;

            Assert.AreEqual(this.stateSize, TypeManager.GetTypeInfo(previousStateComponent.TypeIndex).ElementSize);
            Assert.AreEqual(this.historySize, TypeManager.GetTypeInfo(historyBackComponent.TypeIndex).ElementSize, "Forward buffer doesn't match back buffer");
            Assert.IsTrue(this.historySize > this.stateSize);

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
                StateSize = this.stateSize,
                HistorySize = this.historySize,
                MaxHistory = this.maxHistorySize,
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

            public DynamicComponentTypeHandle HistoryBackType;

            public DynamicComponentTypeHandle HistoryForwardType;

            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public BLLogger Logger;

            public int StateSize;
            public int HistorySize;

            public int MaxHistory;

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(this.EntityType);
                var states = (byte*)chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref this.StateType, this.StateSize).GetUnsafeReadOnlyPtr();
                var previousStates = (byte*)chunk.GetDynamicComponentDataArrayReinterpret<byte>(ref this.PreviousStateType, this.StateSize).GetUnsafePtr();

                var historyBack = chunk.GetDynamicBufferAccessor(ref this.HistoryBackType);
                var historyForward = chunk.GetDynamicBufferAccessor(ref this.HistoryForwardType);

                var previousWithState = stackalloc byte[this.HistorySize];

                for (var i = 0; i < entities.Length; i++)
                {
                    ref readonly var entity = ref entities.ElementAtRO(i);

                    var stateI = states + (i * this.StateSize);
                    var previousI = previousStates + (i * this.StateSize);

                    var changed = UnsafeUtility.MemCmp(stateI, previousI, this.StateSize);
                    if (changed == 0)
                    {
                        continue;
                    }

                    var isAdditionOnly = true;

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

                        isAdditionOnly &= (state & previous) == previous;

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
                                    this.Logger.LogWarning($"State {bit} not setup");
                                }
                            }
                        }
                    }

                    var back = historyBack.GetUntypedBuffer(i);
                    var forward = historyForward.GetUntypedBuffer(i);

                    // First element, never mark it addition
                    if (back.Length == 0)
                    {
                        isAdditionOnly = false;
                    }

                    // Write the state bit
                    UnsafeUtility.MemCpy(previousWithState, previousI, this.StateSize);
                    UnsafeUtility.MemCpy(previousWithState + this.StateSize, &isAdditionOnly, 1);

                    // Was it a Pop operation to a previous state
                    if (back.Length > 0 && this.Equal(back[^1], stateI))
                    {
                        // Limit capacity
                        if (forward.Length == this.MaxHistory)
                        {
                            // forward.RemoveAt(0);
                        }

                        forward.Add(previousWithState);
                        back.RemoveAt(back.Length - 1);
                    }
                    else
                    {
                        if (forward.Length > 0)
                        {
                            if (this.Equal(forward[^1], stateI))
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

                        back.Add(previousWithState);
                    }

                    UnsafeUtility.MemCpy(previousI, stateI, this.StateSize);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool Equal(void* a1, void* a2)
            {
                // We are comparing without the extra state byte
                return UnsafeUtility.MemCmp(a1, a2, this.StateSize) == 0;
            }
        }
    }
}
