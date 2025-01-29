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
        private const int count = 100000;

        [Test]
        public void Test()
        {
            var archetype = this.Manager.CreateArchetype(typeof(Target), typeof(LocalTransform));
            var entities = this.Manager.CreateEntity(archetype, count, Allocator.TempJob);

            var target = this.Manager.CreateEntity(typeof(LocalTransform));
            for (var i = 0; i < entities.Length; i++)
            {
                this.Manager.SetComponentData(entities[i], new Target { Value = target });
            }

            this.World.CreateSystem<TestSystem>().Update(this.WorldUnmanaged);

            Assert.AreEqual(math.ceil(count / 10f), this.Manager.GetComponentData<LocalTransform>(target).Position.z);
        }

        private partial struct TestSystem : ISystem
        {
            public void OnUpdate(ref SystemState state)
            {
                var entityLock = new EntityLock(Allocator.TempJob);

                state.Dependency = new TestJob
                {
                    EntityLock = entityLock,
                    LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
                }.ScheduleParallel(state.Dependency);

                state.Dependency.Complete();

                entityLock.Dispose();
            }

            [BurstCompile]
            private partial struct TestJob : IJobEntity
            {
                public EntityLock EntityLock;

                [NativeDisableParallelForRestriction]
                public ComponentLookup<LocalTransform> LocalTransforms;

                private void Execute([EntityIndexInQuery] int index, in Target target)
                {
                    if (index % 100 != 0)
                    {
                        return;
                    }

                    using (this.EntityLock.Acquire(target.Value))
                    {
                        ref var lt = ref this.LocalTransforms.GetRefRW(target.Value).ValueRW;
                        for (var i = 0; i < 1000; i++)
                        {
                            lt.Position += new float3(0, 0, 0.01f);
                        }
                    }
                }
            }
        }

        public struct Target : IComponentData
        {
            public Entity Value;
        }
    }
}
