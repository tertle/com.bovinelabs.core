// <copyright file="StateFlagModelTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.States
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.States;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Entities;

    internal class StateFlagModelTests : ECSTestsFixture
    {
        [Test]
        public void Flags()
        {
            this.World.CreateSystem<StateFlagModelInstanceTestSystem1>();
            this.World.CreateSystem<StateFlagModelInstanceTestSystem2>();
            var system = this.World.GetOrCreateSystem<StateFlagModelTestSystem>();

            var entity = this.Manager.CreateEntity(typeof(TestState), typeof(TestStatePrevious));

            var states = new BitArray16 { [5] = true };
            this.Manager.SetComponentData(entity, new TestState { Value = states });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<StateFlagModelInstanceTestSystem1.State>(entity));
            Assert.IsFalse(this.Manager.HasComponent<StateFlagModelInstanceTestSystem2.State>(entity));

            states[13] = true;
            this.Manager.SetComponentData(entity, new TestState { Value = states });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<StateFlagModelInstanceTestSystem1.State>(entity));
            Assert.IsTrue(this.Manager.HasComponent<StateFlagModelInstanceTestSystem2.State>(entity));

            states[13] = false;
            this.Manager.SetComponentData(entity, new TestState { Value = states });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<StateFlagModelInstanceTestSystem1.State>(entity));
            Assert.IsFalse(this.Manager.HasComponent<StateFlagModelInstanceTestSystem2.State>(entity));
        }

        internal struct TestState : IComponentData
        {
            public BitArray16 Value;
        }

        internal struct TestStatePrevious : IComponentData
        {
            public BitArray16 Value;
        }
    }

    internal partial struct StateFlagModelTestSystem : ISystem, ISystemStartStop
    {
        private StateFlagModel impl;

        /// <inheritdoc />
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            this.impl = new StateFlagModel(ref state, ComponentType.ReadWrite<StateFlagModelTests.TestState>(),
                ComponentType.ReadWrite<StateFlagModelTests.TestStatePrevious>());
        }

        /// <inheritdoc />
        public void OnStopRunning(ref SystemState state)
        {
            this.impl.Dispose(ref state);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);
            this.impl.UpdateParallel(ref state, commandBuffer.AsParallelWriter());
            state.Dependency.Complete();
            commandBuffer.Playback(state.EntityManager);
        }
    }

    internal partial struct StateFlagModelInstanceTestSystem1 : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.SystemHandle, new StateInstance
            {
                State = TypeManager.GetTypeIndex<StateFlagModelTests.TestState>(),
                StateKey = 5,
                StateInstanceComponent = TypeManager.GetTypeIndex<State>(),
            });
        }

        public struct State : IComponentData
        {
        }
    }

    internal partial class StateFlagModelInstanceTestSystem2 : SystemBase
    {
        protected override void OnCreate()
        {
            this.EntityManager.AddComponentData(this.SystemHandle, new StateInstance
            {
                State = TypeManager.GetTypeIndex<StateFlagModelTests.TestState>(),
                StateKey = 13,
                StateInstanceComponent = TypeManager.GetTypeIndex<State>(),
            });
        }

        protected override void OnUpdate()
        {
        }

        public struct State : IComponentData
        {
        }
    }
}
