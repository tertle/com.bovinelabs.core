namespace BovineLabs.Core.Tests.Extensions
{
    using System;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    public class EntityQueryExtensionsTests : ECSTestsFixture
    {
        [Test]
        public void GetSingletonBufferNoSync_ReadWriteAccessRequiresReadWriteQuery()
        {
            CreateSingletonBuffer();

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<SingletonBuffer>().Build(Manager);

            var exception = Assert.Throws<InvalidOperationException>(() => query.GetSingletonBufferNoSync<SingletonBuffer>(false));

            Assert.That(exception.Message, Does.Contain("read-write"));
        }

        [Test]
        public void GetSingletonBufferNoSync_ReadOnlyAccessRequiresQueryToIncludeBuffer()
        {
            CreateSingletonBufferWithQueryTag();

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<QueryTag>().Build(Manager);

            var exception = Assert.Throws<InvalidOperationException>(() => query.GetSingletonBufferNoSync<SingletonBuffer>(true));

            Assert.That(exception.Message, Does.Contain("included in the EntityQuery"));
        }

        [Test]
        public void GetSingletonBufferNoSync_ReadOnlyAccessAllowsReadOnlyQuery()
        {
            CreateSingletonBuffer();

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<SingletonBuffer>().Build(Manager);

            var buffer = query.GetSingletonBufferNoSync<SingletonBuffer>(true);

            Assert.AreEqual(1, buffer.Length);
        }

        [Test]
        public void GetSingletonBufferNoSync_ReadWriteAccessAllowsReadWriteQuery()
        {
            var entity = CreateSingletonBuffer();

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<SingletonBuffer>().Build(Manager);

            var buffer = query.GetSingletonBufferNoSync<SingletonBuffer>(false);
            buffer.Add(new SingletonBuffer { Value = 2 });

            Assert.AreEqual(2, Manager.GetBuffer<SingletonBuffer>(entity).Length);
        }

        private Entity CreateSingletonBuffer()
        {
            var entity = Manager.CreateEntity(typeof(SingletonBuffer));
            var buffer = Manager.GetBuffer<SingletonBuffer>(entity);
            buffer.Add(new SingletonBuffer { Value = 1 });
            return entity;
        }

        private Entity CreateSingletonBufferWithQueryTag()
        {
            var entity = Manager.CreateEntity(typeof(SingletonBuffer), typeof(QueryTag));
            var buffer = Manager.GetBuffer<SingletonBuffer>(entity);
            buffer.Add(new SingletonBuffer { Value = 1 });
            return entity;
        }

        private struct SingletonBuffer : IBufferElementData
        {
            public int Value;
        }

        private struct QueryTag : IComponentData
        {
        }
    }
}
