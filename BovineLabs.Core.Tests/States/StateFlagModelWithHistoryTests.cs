// <copyright file="StateFlagModelWithHistoryTests.cs" company="BovineLabs">
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

    internal class StateFlagModelWithHistoryTests : ECSTestsFixture
    {
        [Test]
        public void Flags()
        {
            this.World.CreateSystem<StateFlagModelWithHistoryInstanceTestSystem1>();
            this.World.CreateSystem<StateFlagModelWithHistoryInstanceTestSystem2>();
            var system = this.World.GetOrCreateSystem<StateFlagModelWithHistoryTestSystem>();

            var entity = this.Manager.CreateEntity(typeof(TestState), typeof(TestStatePrevious), typeof(TestStateBack), typeof(TestStateForward));

            var states = new BitArray16 { [5] = true };
            this.Manager.SetComponentData(entity, new TestState { Value = states });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<StateFlagModelWithHistoryInstanceTestSystem1.State>(entity));
            Assert.IsFalse(this.Manager.HasComponent<StateFlagModelWithHistoryInstanceTestSystem2.State>(entity));

            states[13] = true;
            this.Manager.SetComponentData(entity, new TestState { Value = states });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<StateFlagModelWithHistoryInstanceTestSystem1.State>(entity));
            Assert.IsTrue(this.Manager.HasComponent<StateFlagModelWithHistoryInstanceTestSystem2.State>(entity));

            states[13] = false;
            this.Manager.SetComponentData(entity, new TestState { Value = states });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<StateFlagModelWithHistoryInstanceTestSystem1.State>(entity));
            Assert.IsFalse(this.Manager.HasComponent<StateFlagModelWithHistoryInstanceTestSystem2.State>(entity));
        }

        [Test]
        public void History()
        {
            this.World.CreateSystem<StateFlagModelWithHistoryInstanceTestSystem1>();
            this.World.CreateSystem<StateFlagModelWithHistoryInstanceTestSystem2>();
            var system = this.World.GetOrCreateSystem<StateFlagModelWithHistoryTestSystem>();

            var entity = this.Manager.CreateEntity(typeof(TestState), typeof(TestStatePrevious), typeof(TestStateBack), typeof(TestStateForward));

            this.Manager.SetComponentData(entity, new TestState { Value = new BitArray16 { [1] = true } });
            system.Update(this.World.Unmanaged);

            this.Manager.SetComponentData(entity, new TestState { Value = new BitArray16 { [2] = true } });
            system.Update(this.World.Unmanaged);

            this.Manager.SetComponentData(entity, new TestState { Value = new BitArray16 { [3] = true } });
            system.Update(this.World.Unmanaged);

            this.Manager.SetComponentData(entity, new TestState { Value = new BitArray16 { [4] = true } });
            system.Update(this.World.Unmanaged);

            // Test back history storage
            var backHistory = this.Manager.GetBuffer<TestStateBack>(entity);
            var forwardHistory = this.Manager.GetBuffer<TestStateForward>(entity);
            Assert.AreEqual(4, backHistory.Length);
            Assert.AreEqual(0, forwardHistory.Length);

            Assert.AreEqual(0, backHistory[0].Value.Data);
            Assert.IsTrue(backHistory[1].Value[1]);
            Assert.IsTrue(backHistory[2].Value[2]);
            Assert.IsTrue(backHistory[3].Value[3]);

            this.Manager.SetComponentData(entity, new TestState { Value = new BitArray16 { [3] = true } });
            system.Update(this.World.Unmanaged);

            this.Manager.SetComponentData(entity, new TestState { Value = new BitArray16 { [2] = true } });
            system.Update(this.World.Unmanaged);

            this.Manager.SetComponentData(entity, new TestState { Value = new BitArray16 { [1] = true } });
            system.Update(this.World.Unmanaged);

            backHistory = this.Manager.GetBuffer<TestStateBack>(entity);
            forwardHistory = this.Manager.GetBuffer<TestStateForward>(entity);
            Assert.AreEqual(1, backHistory.Length);
            Assert.AreEqual(3, forwardHistory.Length);

            Assert.AreEqual(0, backHistory[0].Value.Data);
            Assert.IsTrue(forwardHistory[0].Value[4]);
            Assert.IsTrue(forwardHistory[1].Value[3]);
            Assert.IsTrue(forwardHistory[2].Value[2]);

            // Test stepping forward
            this.Manager.SetComponentData(entity, new TestState { Value = new BitArray16 { [2] = true } });
            system.Update(this.World.Unmanaged);

            backHistory = this.Manager.GetBuffer<TestStateBack>(entity);
            forwardHistory = this.Manager.GetBuffer<TestStateForward>(entity);
            Assert.AreEqual(2, backHistory.Length);
            Assert.AreEqual(2, forwardHistory.Length);

            Assert.AreEqual(0, backHistory[0].Value.Data);
            Assert.IsTrue(backHistory[1].Value[1]);
            Assert.IsTrue(forwardHistory[0].Value[4]);
            Assert.IsTrue(forwardHistory[1].Value[3]);

            // Test clearing history forward
            this.Manager.SetComponentData(entity, new TestState { Value = new BitArray16 { [4] = true } });
            system.Update(this.World.Unmanaged);

            backHistory = this.Manager.GetBuffer<TestStateBack>(entity);
            forwardHistory = this.Manager.GetBuffer<TestStateForward>(entity);
            Assert.AreEqual(3, backHistory.Length);
            Assert.AreEqual(0, forwardHistory.Length);

            Assert.AreEqual(0, backHistory[0].Value.Data);
            Assert.IsTrue(backHistory[1].Value[1]);
            Assert.IsTrue(backHistory[2].Value[2]);
        }
    }

    internal struct TestState : IComponentData
    {
        public BitArray16 Value;
    }

    internal struct TestStatePrevious : IComponentData
    {
        public BitArray16 Value;
    }

    internal struct TestStateBack : IBufferElementData
    {
        public BitArray16 Value;

        public bool WasPopup;
    }

    internal struct TestStateForward : IBufferElementData
    {
        public BitArray16 Value;

        public bool WasPopup;
    }

    internal partial struct StateFlagModelWithHistoryTestSystem : ISystem, ISystemStartStop
    {
        private StateFlagModelWithHistory impl;

        /// <inheritdoc />
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            this.impl = new StateFlagModelWithHistory(ref state, ComponentType.ReadWrite<TestState>(), ComponentType.ReadWrite<TestStatePrevious>(),
                ComponentType.ReadWrite<TestStateBack>(), ComponentType.ReadWrite<TestStateForward>(), 4);
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
            commandBuffer.Dispose();
        }
    }

    internal partial struct StateFlagModelWithHistoryInstanceTestSystem1 : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.SystemHandle, new StateInstance
            {
                State = TypeManager.GetTypeIndex<TestState>(),
                StateKey = 5,
                StateInstanceComponent = TypeManager.GetTypeIndex<State>(),
            });
        }

        public struct State : IComponentData
        {
        }
    }

    internal partial class StateFlagModelWithHistoryInstanceTestSystem2 : SystemBase
    {
        protected override void OnCreate()
        {
            this.EntityManager.AddComponentData(this.SystemHandle, new StateInstance
            {
                State = TypeManager.GetTypeIndex<TestState>(),
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
