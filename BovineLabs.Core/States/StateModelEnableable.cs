// <copyright file="StateModelEnableable.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary> A generic general purpose state system that ensures only a single state component exists on an entity but driven from a byte field. </summary>
    public struct StateModelEnableable
    {
        private StateImpl impl;

        public StateModelEnableable(ref SystemState state, ComponentType stateComponent, ComponentType previousStateComponent)
        {
            Assert.AreEqual(UnsafeUtility.SizeOf<byte>(), TypeManager.GetTypeInfo(stateComponent.TypeIndex).ElementSize);
            Assert.AreEqual(UnsafeUtility.SizeOf<byte>(), TypeManager.GetTypeInfo(previousStateComponent.TypeIndex).ElementSize);

            this.impl = new StateImpl(ref state, stateComponent, previousStateComponent);

            using var e = this.impl.RegisteredStatesMap.GetEnumerator();
            while (e.MoveNext())
            {
                Assert.IsTrue(TypeManager.GetTypeInfo(e.Current.Value.TypeIndex).EnableableType,
                    $"Non EnableableType {e.Current.Value} trying to be assigned to {stateComponent}");

                // Add all component dependencies to this system
                state.AddDependency(e.Current.Value.TypeIndex);
            }
        }

        public void Dispose(ref SystemState state)
        {
            state.Dependency.Complete();
            this.impl.Dispose();
        }

        public void Run(ref SystemState state)
        {
            state.Dependency.Complete();
            var job = this.UpdateInternal(ref state);
            job.RunByRef(this.impl.Query);
        }

        public void Update(ref SystemState state)
        {
            var job = this.UpdateInternal(ref state);
            state.Dependency = job.ScheduleByRef(this.impl.Query, state.Dependency);
        }

        public void UpdateParallel(ref SystemState state)
        {
            var job = this.UpdateInternal(ref state);
            state.Dependency = job.ScheduleParallel(this.impl.Query, state.Dependency);
        }

        private StateJob UpdateInternal(ref SystemState state)
        {
            this.impl.Update(ref state);

            return new StateJob
            {
                RegisteredStates = this.impl.RegisteredStatesMap,
                EntityType = this.impl.EntityType,
                StateType = this.impl.StateType,
                PreviousStateType = this.impl.PreviousStateType,
                UnsafeEnableableLookup = state.GetUnsafeEnableableLookup(),
                Logger = this.impl.Logger,
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

            public UnsafeEnableableLookup UnsafeEnableableLookup;

            public BLLogger Logger;

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
                            this.UnsafeEnableableLookup.SetComponentEnabled(entity, stateComponent, false);
                        }
                    }

                    if (state != 0)
                    {
                        if (this.RegisteredStates.TryGetValue(state, out var stateComponent))
                        {
                            this.UnsafeEnableableLookup.SetComponentEnabled(entity, stateComponent, true);
                        }
                        else
                        {
                            this.Logger.LogWarning($"State {state} not setup");
                        }
                    }

                    previous = state;
                    previousStates[i] = previous;
                }
            }
        }
    }
}
