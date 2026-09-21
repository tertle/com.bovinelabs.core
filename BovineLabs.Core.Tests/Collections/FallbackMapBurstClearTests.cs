namespace BovineLabs.Core.Tests.Collections
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    public partial class FallbackMapBurstClearTests : ECSTestsFixture
    {
        [Test]
        public void HashMapClear_FromBurstCompiledSystem_ClearsHashMapAndFallback()
        {
            var systemHandle = World.CreateSystem<HashMapClearSystem>();

            systemHandle.Update(WorldUnmanaged);
            WorldUnmanaged.ResolveSystemStateRef(systemHandle).CompleteDependency();

            ref var system = ref WorldUnmanaged.GetUnsafeSystemRef<HashMapClearSystem>(systemHandle);
            Assert.AreEqual(0, system.Map.HashMap.Count());
            Assert.AreEqual(0, system.Map.Fallback.Count);
        }

        [Test]
        public void MultiHashMapClear_FromBurstCompiledSystem_ClearsHashMapAndFallback()
        {
            var systemHandle = World.CreateSystem<MultiHashMapClearSystem>();

            systemHandle.Update(WorldUnmanaged);
            WorldUnmanaged.ResolveSystemStateRef(systemHandle).CompleteDependency();

            ref var system = ref WorldUnmanaged.GetUnsafeSystemRef<MultiHashMapClearSystem>(systemHandle);
            Assert.AreEqual(0, system.Map.HashMap.Count());
            Assert.AreEqual(0, system.Map.Fallback.Count);
        }

        private partial struct HashMapClearSystem : ISystem
        {
            public NativeParallelHashMapFallback<Entity, HashMapValue> Map;

            public void OnCreate(ref SystemState state)
            {
                Map = new NativeParallelHashMapFallback<Entity, HashMapValue>(1, Allocator.Persistent);

                var writer = Map.AsWriter();
                writer.Add(new Entity { Index = 1, Version = 1 }, new HashMapValue { Value = 10 });
                writer.Add(new Entity { Index = 2, Version = 1 }, new HashMapValue { Value = 20 });
            }

            public void OnDestroy(ref SystemState state)
            {
                state.Dependency.Complete();
                Map.Dispose();
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                state.Dependency = Map.Clear(state.Dependency);
            }
        }

        private partial struct MultiHashMapClearSystem : ISystem
        {
            public NativeParallelMultiHashMapFallback<Entity, MultiHashMapValue> Map;

            public void OnCreate(ref SystemState state)
            {
                Map = new NativeParallelMultiHashMapFallback<Entity, MultiHashMapValue>(1, Allocator.Persistent);

                var writer = Map.AsWriter();
                writer.Add(new Entity { Index = 1, Version = 1 }, new MultiHashMapValue { Value = 10 });
                writer.Add(new Entity { Index = 2, Version = 1 }, new MultiHashMapValue { Value = 20 });
            }

            public void OnDestroy(ref SystemState state)
            {
                state.Dependency.Complete();
                Map.Dispose();
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                state.Dependency = Map.Clear(state.Dependency);
            }
        }

        private struct HashMapValue
        {
            public int Value;
        }

        private struct MultiHashMapValue
        {
            public int Value;
        }
    }
}
