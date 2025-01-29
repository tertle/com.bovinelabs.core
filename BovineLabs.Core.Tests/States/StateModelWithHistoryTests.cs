// <copyright file="StateModelWithHistoryTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.States
{
    using BovineLabs.Core.States;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Entities;

    internal class StateModelWithHistoryTests : ECSTestsFixture
    {
        [Test]
        public void States()
        {
            this.World.CreateSystem<StateModelWithHistoryInstanceTestSystem1>();
            this.World.CreateSystem<StateModelWithHistoryInstanceTestSystem2>();
            var system = this.World.GetOrCreateSystem<StateModelWithHistoryTestSystem>();

            var entity = this.Manager.CreateEntity(typeof(TestState), typeof(TestStatePrevious), typeof(TestStateBack), typeof(TestStateForward));

            this.Manager.SetComponentData(entity, new TestState { Value = 1 });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<StateModelWithHistoryInstanceTestSystem1.State>(entity));
            Assert.IsFalse(this.Manager.HasComponent<StateModelWithHistoryInstanceTestSystem2.State>(entity));

            this.Manager.SetComponentData(entity, new TestState { Value = 2 });
            system.Update(this.World.Unmanaged);
            Assert.IsFalse(this.Manager.HasComponent<StateModelWithHistoryInstanceTestSystem1.State>(entity));
            Assert.IsTrue(this.Manager.HasComponent<StateModelWithHistoryInstanceTestSystem2.State>(entity));

            this.Manager.SetComponentData(entity, new TestState { Value = 0 });
            system.Update(this.World.Unmanaged);
            Assert.IsFalse(this.Manager.HasComponent<StateModelWithHistoryInstanceTestSystem1.State>(entity));
            Assert.IsFalse(this.Manager.HasComponent<StateModelWithHistoryInstanceTestSystem2.State>(entity));
        }

        [Test]
        public void History()
        {
            var system = this.World.GetOrCreateSystem<StateModelWithHistoryTestSystem>();
            var entity = this.Manager.CreateEntity(typeof(TestState), typeof(TestStatePrevious), typeof(TestStateBack), typeof(TestStateForward));

            this.Manager.SetComponentData(entity, new TestState { Value = 1 });
            system.Update(this.World.Unmanaged);

            this.Manager.SetComponentData(entity, new TestState { Value = 2 });
            system.Update(this.World.Unmanaged);

            this.Manager.SetComponentData(entity, new TestState { Value = 3 });
            system.Update(this.World.Unmanaged);

            this.Manager.SetComponentData(entity, new TestState { Value = 4 });
            system.Update(this.World.Unmanaged);

            // Test back history storage
            var backHistory = this.Manager.GetBuffer<TestStateBack>(entity);
            var forwardHistory = this.Manager.GetBuffer<TestStateForward>(entity);
            Assert.AreEqual(4, backHistory.Length);
            Assert.AreEqual(0, forwardHistory.Length);

            Assert.AreEqual(0, backHistory[0].Value);
            Assert.AreEqual(1, backHistory[1].Value);
            Assert.AreEqual(2, backHistory[2].Value);
            Assert.AreEqual(3, backHistory[3].Value);

            // Test stepping back
            this.Manager.SetComponentData(entity, new TestState { Value = 3 });
            system.Update(this.World.Unmanaged);

            this.Manager.SetComponentData(entity, new TestState { Value = 2 });
            system.Update(this.World.Unmanaged);

            this.Manager.SetComponentData(entity, new TestState { Value = 1 });
            system.Update(this.World.Unmanaged);

            backHistory = this.Manager.GetBuffer<TestStateBack>(entity);
            forwardHistory = this.Manager.GetBuffer<TestStateForward>(entity);
            Assert.AreEqual(1, backHistory.Length);
            Assert.AreEqual(3, forwardHistory.Length);

            Assert.AreEqual(0, backHistory[0].Value);
            Assert.AreEqual(4, forwardHistory[0].Value);
            Assert.AreEqual(3, forwardHistory[1].Value);
            Assert.AreEqual(2, forwardHistory[2].Value);

            // Test stepping forward
            this.Manager.SetComponentData(entity, new TestState { Value = 2 });
            system.Update(this.World.Unmanaged);

            backHistory = this.Manager.GetBuffer<TestStateBack>(entity);
            forwardHistory = this.Manager.GetBuffer<TestStateForward>(entity);
            Assert.AreEqual(2, backHistory.Length);
            Assert.AreEqual(2, forwardHistory.Length);

            Assert.AreEqual(0, backHistory[0].Value);
            Assert.AreEqual(1, backHistory[1].Value);
            Assert.AreEqual(4, forwardHistory[0].Value);
            Assert.AreEqual(3, forwardHistory[1].Value);

            // Test clearing history forward
            this.Manager.SetComponentData(entity, new TestState { Value = 4 });
            system.Update(this.World.Unmanaged);

            backHistory = this.Manager.GetBuffer<TestStateBack>(entity);
            forwardHistory = this.Manager.GetBuffer<TestStateForward>(entity);
            Assert.AreEqual(3, backHistory.Length);
            Assert.AreEqual(0, forwardHistory.Length);

            Assert.AreEqual(0, backHistory[0].Value);
            Assert.AreEqual(1, backHistory[1].Value);
            Assert.AreEqual(2, backHistory[2].Value);
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

    internal partial struct StateModelWithHistoryTestSystem : ISystem, ISystemStartStop
    {
        private StateModelWithHistory impl;

        /// <inheritdoc />
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            this.impl = new StateModelWithHistory(ref state, ComponentType.ReadWrite<StateModelWithHistoryTests.TestState>(),
                ComponentType.ReadWrite<StateModelWithHistoryTests.TestStatePrevious>(), ComponentType.ReadWrite<StateModelWithHistoryTests.TestStateBack>(),
                ComponentType.ReadWrite<StateModelWithHistoryTests.TestStateForward>(), 4);
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

    internal partial struct StateModelWithHistoryInstanceTestSystem1 : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.SystemHandle,
                new StateInstance
                {
                    State = TypeManager.GetTypeIndex<StateModelWithHistoryTests.TestState>(),
                    StateKey = 1,
                    StateInstanceComponent = TypeManager.GetTypeIndex<State>(),
                });
        }

        public struct State : IComponentData
        {
        }
    }

    internal partial class StateModelWithHistoryInstanceTestSystem2 : SystemBase
    {
        protected override void OnCreate()
        {
            this.EntityManager.AddComponentData(this.SystemHandle, new StateInstance
            {
                State = TypeManager.GetTypeIndex<StateModelWithHistoryTests.TestState>(),
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
