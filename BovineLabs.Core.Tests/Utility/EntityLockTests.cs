// <copyright file="EntityLockTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using BovineLabs.Core.Utility;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    public partial class EntityLockTests : ECSTestsFixture
    {
        private const int writeCount = 100;
        private const int count = 10000;
        private const int skipCount = 10;

        [Test]
        public void Test()
        {
            var archetype = this.Manager.CreateArchetype(typeof(Target));
            using var entities = this.Manager.CreateEntity(archetype, count, Allocator.TempJob);

            var target = this.Manager.CreateEntity(typeof(Count));
            for (var i = 0; i < entities.Length; i++)
            {
                this.Manager.SetComponentData(entities[i], new Target { Value = target });
            }

            this.World.CreateSystem<TestSystem>().Update(this.WorldUnmanaged);

            Assert.AreEqual((count / skipCount) * writeCount, this.Manager.GetComponentData<Count>(target).Value.z);
        }

        private partial struct TestSystem : ISystem
        {
            public void OnUpdate(ref SystemState state)
            {
                var entityLock = new EntityLock(Allocator.TempJob);

                state.Dependency = new TestJob
                {
                    EntityLock = entityLock,
                    Counts = SystemAPI.GetComponentLookup<Count>(),
                }.ScheduleParallel(state.Dependency);

                state.Dependency.Complete();

                entityLock.Dispose();
            }

            [BurstCompile]
            private partial struct TestJob : IJobEntity
            {
                public EntityLock EntityLock;

                [NativeDisableParallelForRestriction]
                public ComponentLookup<Count> Counts;

                private void Execute([EntityIndexInQuery] int index, in Target target)
                {
                    if (index % skipCount != 0)
                    {
                        return;
                    }

                    ref var lt = ref this.Counts.GetRefRW(target.Value).ValueRW;

                    using (this.EntityLock.Acquire(target.Value))
                    {
                        var z = lt.Value.z;

                        for (var i = 0; i < writeCount; i++)
                        {
                            z += 1;
                        }

                        lt.Value.z = z;
                    }
                }
            }
        }

        public struct Target : IComponentData
        {
            public Entity Value;
        }

        public struct Count : IComponentData
        {
            public int3 Value;
        }
    }
}
