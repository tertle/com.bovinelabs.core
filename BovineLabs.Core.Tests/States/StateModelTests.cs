// <copyright file="StateModelTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.States
{
    using BovineLabs.Core.States;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Entities;

    internal class StateModelTests : ECSTestsFixture
    {
        [Test]
        public void States()
        {
            this.World.CreateSystem<StateModelInstanceTestSystem1>();
            this.World.CreateSystem<StateModelInstanceTestSystem2>();
            var system = this.World.GetOrCreateSystem<StateModelTestSystem>();

            var entity = this.Manager.CreateEntity(typeof(TestState), typeof(TestStatePrevious), typeof(TestStateBack), typeof(TestStateForward));

            this.Manager.SetComponentData(entity, new TestState { Value = 1 });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<StateModelInstanceTestSystem1.State>(entity));
            Assert.IsFalse(this.Manager.HasComponent<StateModelInstanceTestSystem2.State>(entity));

            this.Manager.SetComponentData(entity, new TestState { Value = 2 });
            system.Update(this.World.Unmanaged);
            Assert.IsFalse(this.Manager.HasComponent<StateModelInstanceTestSystem1.State>(entity));
            Assert.IsTrue(this.Manager.HasComponent<StateModelInstanceTestSystem2.State>(entity));

            this.Manager.SetComponentData(entity, new TestState { Value = 0 });
            system.Update(this.World.Unmanaged);
            Assert.IsFalse(this.Manager.HasComponent<StateModelInstanceTestSystem1.State>(entity));
            Assert.IsFalse(this.Manager.HasComponent<StateModelInstanceTestSystem2.State>(entity));
        }

        internal struct TestState : IComponentData
        {
            public byte Value;
        }

        internal struct TestStatePrevious : IComponentData
        {
            public byte Value;
        }

        internal struct TestStateBack : IBufferElementData
        {
            public byte Value;
        }

        internal struct TestStateForward : IBufferElementData
        {
            public byte Value;
        }
    }

    [BurstCompile]
    internal partial struct StateModelTestSystem : ISystem, ISystemStartStop
    {
        private StateModel impl;

        /// <inheritdoc />
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            this.impl = new StateModel(ref state, ComponentType.ReadWrite<StateModelTests.TestState>(),
                ComponentType.ReadWrite<StateModelTests.TestStatePrevious>());
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

    internal partial struct StateModelInstanceTestSystem1 : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.SystemHandle, new StateInstance
            {
                State = TypeManager.GetTypeIndex<StateModelTests.TestState>(),
                StateKey = 1,
                StateInstanceComponent = TypeManager.GetTypeIndex<State>(),
            });
        }

        public struct State : IComponentData
        {
        }
    }

    internal partial class StateModelInstanceTestSystem2 : SystemBase
    {
        protected override void OnCreate()
        {
            this.EntityManager.AddComponentData(this.SystemHandle, new StateInstance
            {
                State = TypeManager.GetTypeIndex<StateModelTests.TestState>(),
                StateKey = 2,
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
