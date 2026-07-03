// <copyright file="EntityQueryExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
            this.CreateSingletonBuffer();

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<SingletonBuffer>().Build(this.Manager);

            var exception = Assert.Throws<InvalidOperationException>(() => query.GetSingletonBufferNoSync<SingletonBuffer>(false));

            Assert.That(exception.Message, Does.Contain("read-write"));
        }

        [Test]
        public void GetSingletonBufferNoSync_ReadOnlyAccessRequiresQueryToIncludeBuffer()
        {
            this.CreateSingletonBufferWithQueryTag();

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<QueryTag>().Build(this.Manager);

            var exception = Assert.Throws<InvalidOperationException>(() => query.GetSingletonBufferNoSync<SingletonBuffer>(true));

            Assert.That(exception.Message, Does.Contain("included in the EntityQuery"));
        }

        [Test]
        public void GetSingletonBufferNoSync_ReadOnlyAccessAllowsReadOnlyQuery()
        {
            this.CreateSingletonBuffer();

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<SingletonBuffer>().Build(this.Manager);

            var buffer = query.GetSingletonBufferNoSync<SingletonBuffer>(true);

            Assert.AreEqual(1, buffer.Length);
        }

        [Test]
        public void GetSingletonBufferNoSync_ReadWriteAccessAllowsReadWriteQuery()
        {
            var entity = this.CreateSingletonBuffer();

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<SingletonBuffer>().Build(this.Manager);

            var buffer = query.GetSingletonBufferNoSync<SingletonBuffer>(false);
            buffer.Add(new SingletonBuffer { Value = 2 });

            Assert.AreEqual(2, this.Manager.GetBuffer<SingletonBuffer>(entity).Length);
        }

        [Test]
        public void GetSingletonUntypedBuffer_ReadWriteAccessRequiresReadWriteQuery()
        {
            this.CreateSingletonBuffer();

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<SingletonBuffer>().Build(this.Manager);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                query.GetSingletonUntypedBuffer(ComponentType.ReadWrite<SingletonBuffer>(), false));

            Assert.That(exception.Message, Does.Contain("read-write"));
        }

        [Test]
        public void GetSingletonUntypedBuffer_ReadOnlyAccessRequiresQueryToIncludeBuffer()
        {
            this.CreateSingletonBufferWithQueryTag();

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<QueryTag>().Build(this.Manager);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                query.GetSingletonUntypedBuffer(ComponentType.ReadOnly<SingletonBuffer>(), true));

            Assert.That(exception.Message, Does.Contain("included in the EntityQuery"));
        }

        private Entity CreateSingletonBuffer()
        {
            var entity = this.Manager.CreateEntity(typeof(SingletonBuffer));
            var buffer = this.Manager.GetBuffer<SingletonBuffer>(entity);
            buffer.Add(new SingletonBuffer { Value = 1 });
            return entity;
        }

        private Entity CreateSingletonBufferWithQueryTag()
        {
            var entity = this.Manager.CreateEntity(typeof(SingletonBuffer), typeof(QueryTag));
            var buffer = this.Manager.GetBuffer<SingletonBuffer>(entity);
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
