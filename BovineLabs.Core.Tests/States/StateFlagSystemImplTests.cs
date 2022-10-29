namespace BovineLabs.Core.Tests.States
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.States;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    public class StateFlagSystemImplTests : ECSTestsFixture
    {
        [Test]
        public void FlagTest()
        {
            this.World.CreateSystem<TestInstanceSystem1>();
            this.World.CreateSystem<TestInstanceSystem2>();
            var system = this.World.GetOrCreateSystem<TestSystem>();

            var entity = this.Manager.CreateEntity(typeof(TestState), typeof(TestStatePrevious));

            var states = new BitArray16 { [5] = true };
            this.Manager.SetComponentData(entity, new TestState { Value = states });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<TestInstanceSystem1.State>(entity));
            Assert.IsFalse(this.Manager.HasComponent<TestInstanceSystem2.State>(entity));

            states[13] = true;
            this.Manager.SetComponentData(entity, new TestState { Value = states });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<TestInstanceSystem1.State>(entity));
            Assert.IsTrue(this.Manager.HasComponent<TestInstanceSystem2.State>(entity));

            states[13] = false;
            this.Manager.SetComponentData(entity, new TestState { Value = states });
            system.Update(this.World.Unmanaged);
            Assert.IsTrue(this.Manager.HasComponent<TestInstanceSystem1.State>(entity));
            Assert.IsFalse(this.Manager.HasComponent<TestInstanceSystem2.State>(entity));
        }
    }

    [BurstCompile]
    internal partial struct TestSystem : ISystem, ISystemStartStop
    {
        private StateFlagSystemImpl impl;

        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            this.impl.Update(ref state, commandBuffer.AsParallelWriter());
            state.Dependency.Complete();
            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }

        public void OnStartRunning(ref SystemState state)
        {
            this.impl = new StateFlagSystemImpl(ref state, typeof(TestState), typeof(TestStatePrevious));
        }

        public void OnStopRunning(ref SystemState state)
        {
            this.impl.Dispose();
        }
    }

    internal partial struct TestInstanceSystem1 : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.SystemHandle, new StateSystemInstance
            {
                State = TypeManager.GetTypeIndex<TestState>(),
                StateKey = 5,
                StateInstanceComponent = TypeManager.GetTypeIndex<State>(),
            });
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) { }

        public struct State : IComponentData { }
    }

    internal partial class TestInstanceSystem2 : SystemBase
    {
        protected override void OnCreate()
        {
            EntityManager.AddComponentData(this.SystemHandle, new StateSystemInstance
            {
                State = TypeManager.GetTypeIndex<TestState>(),
                StateKey = 13,
                StateInstanceComponent = TypeManager.GetTypeIndex<State>(),
            });
        }

        protected override void OnUpdate() { }

        public struct State : IComponentData { }
    }

    internal struct TestState : IComponentData
    {
        public BitArray16 Value { get; set; }
    }

    internal struct TestStatePrevious : IComponentData
    {
        public BitArray16 Value { get; set; }
    }
}
