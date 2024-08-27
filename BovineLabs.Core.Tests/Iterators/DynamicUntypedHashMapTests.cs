// <copyright file="DynamicUntypedHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Mathematics;

    public class DynamicUntypedHashMapTests : ECSTestsFixture
    {
        [Test]
        public void InitializeAndLarge()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, ushort>(0, 256).AsUntypedHashMap<TestBuffer, ushort>();

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
        public void Stress()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, ushort>(0, 0).AsUntypedHashMap<TestBuffer, ushort>();

            for (var i = 0; i < 100; i++)
            {
                hashMap.AddOrSet(0, 1);
                hashMap.AddOrSet(1, (short)2);
                hashMap.AddOrSet(2, new float3(1, 2, 3));
                hashMap.AddOrSet(3, new Large());
                hashMap.AddOrSet(4, new float3(3, 2, 1));
                hashMap.AddOrSet(5, new Large());
                hashMap.AddOrSet(6, 1);
                hashMap.AddOrSet(7, (short)2);
            }

            //
            // for (ushort i = 0; i < 100; i++)
            // {
            //     hashMap.AddOrSet(i, new float3(1, 2, 3));
            // }
        }

        [InternalBufferCapacity(0)]
        private struct TestBuffer : IDynamicUntypedHashMap<ushort>
        {
            byte IDynamicUntypedHashMap<ushort>.Value { get; }
        }

        public struct Large
        {
            public ulong TestValue0;
            public ulong TestValue1;
        }
    }
}
