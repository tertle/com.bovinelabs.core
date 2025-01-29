// <copyright file="DynamicUntypedHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Utility;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Mathematics;

    public class DynamicUntypedHashMapTests : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();

            TypeManagerEx.Initialize();
        }

        [Test]
        public void InitializeAndLarge()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>().AsUntypedHashMap<TestBuffer, int>();

            hashMap.AddOrSet(0, 1);
            hashMap.AddOrSet(1, (short)2);
            hashMap.AddOrSet(2, new float3(1, 2, 3));
            hashMap.AddOrSet(3, new Large());
            hashMap.AddOrSet(2, new float3(3, 2, 1));
            hashMap.AddOrSet(3, new Large());

            Assert.AreEqual(1, hashMap.GetOrAddRef(0, 0));
            Assert.AreEqual((short)2, hashMap.GetOrAddRef(1, (short)0));
            Assert.AreEqual(new float3(3, 2, 1), hashMap.GetOrAddRef(2, float3.zero));
        }

        [Test]
        public void StressAdd()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>(0, 0).AsUntypedHashMap<TestBuffer, int>();

            for (var i = 0; i < 50; i++)
            {
                hashMap.AddOrSet((i * 8) + 0, 1);
                hashMap.AddOrSet((i * 8) + 1, (short)2);
                hashMap.AddOrSet((i * 8) + 2, new float3(1, 2, 3));
                hashMap.AddOrSet((i * 8) + 3, new Large());
                hashMap.AddOrSet((i * 8) + 4, new float3(3, 2, 1));
                hashMap.AddOrSet((i * 8) + 5, new Large());
                hashMap.AddOrSet((i * 8) + 6, 1);
                hashMap.AddOrSet((i * 8) + 7, (short)2);
            }

            for (var i = 0; i < 50; i++)
            {
                hashMap.AddOrSet((i * 8) + 0, 1);
                hashMap.AddOrSet((i * 8) + 1, (short)2);
                hashMap.AddOrSet((i * 8) + 2, new float3(1, 2, 3));
                hashMap.AddOrSet((i * 8) + 3, new Large());
                hashMap.AddOrSet((i * 8) + 4, new float3(3, 2, 1));
                hashMap.AddOrSet((i * 8) + 5, new Large());
                hashMap.AddOrSet((i * 8) + 6, 1);
                hashMap.AddOrSet((i * 8) + 7, (short)2);
            }
        }

        [Test]
        public void StressGet()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>(0, 0).AsUntypedHashMap<TestBuffer, int>();

            for (var i = 0; i < 50; i++)
            {
                hashMap.GetOrAddRef((i * 8) + 0, 1);
                hashMap.GetOrAddRef((i * 8) + 1, (short)2);
                hashMap.GetOrAddRef((i * 8) + 2, new float3(1, 2, 3));
                hashMap.GetOrAddRef((i * 8) + 3, new Large());
                hashMap.GetOrAddRef((i * 8) + 4, new float3(3, 2, 1));
                hashMap.GetOrAddRef((i * 8) + 5, new Large());
                hashMap.GetOrAddRef((i * 8) + 6, 1);
                hashMap.GetOrAddRef((i * 8) + 7, (short)2);
            }

            for (var i = 0; i < 50; i++)
            {
                hashMap.GetOrAddRef((i * 8) + 0, 1);
                hashMap.GetOrAddRef((i * 8) + 1, (short)2);
                hashMap.GetOrAddRef((i * 8) + 2, new float3(1, 2, 3));
                hashMap.GetOrAddRef((i * 8) + 3, new Large());
                hashMap.GetOrAddRef((i * 8) + 4, new float3(3, 2, 1));
                hashMap.GetOrAddRef((i * 8) + 5, new Large());
                hashMap.GetOrAddRef((i * 8) + 6, 1);
                hashMap.GetOrAddRef((i * 8) + 7, (short)2);
            }
        }

        [InternalBufferCapacity(0)]
        private struct TestBuffer : IDynamicUntypedHashMap<int>
        {
            byte IDynamicUntypedHashMap<int>.Value { get; }
        }

        public struct Large
        {
            public ulong TestValue0;
            public ulong TestValue1;
        }
    }
}
