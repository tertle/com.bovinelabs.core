// <copyright file="DynamicMultiHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;

    public class DynamicMultiHashMapTests : ECSTestsFixture
    {
        [Test]
        public void Capacity()
        {
            const int newCapacity = 128;

            var hashMap = this.CreateHashMap();
            Assert.AreEqual(0, hashMap.Capacity);

            hashMap.Capacity = newCapacity;

            Assert.AreEqual(newCapacity, hashMap.Capacity);
        }

        [Test]
        public void AddRemove()
        {
            const int count = 1027;

            var hashMap = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                hashMap.Add(i + i, (byte)i);
                hashMap.Add(i + i, (byte)i);
            }

            Assert.AreEqual(count * 2, hashMap.Count());

            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(hashMap.Remove(i + i));
            }

            Assert.AreEqual(0, hashMap.Count());
        }

        [Test]
        public void TryGetValue()
        {
            var hashMap = this.CreateHashMap();
            Assert.IsFalse(hashMap.TryGetValue(47, out _));

            hashMap.Add(47, 123);
            Assert.IsTrue(hashMap.TryGetValue(47, out var result));
            Assert.AreEqual(123, result);

            hashMap.Remove(47);
            Assert.IsFalse(hashMap.TryGetValue(47, out _));
        }

        private DynamicMultiHashMap<int, byte> CreateHashMap()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashMap));
            return this.Manager.GetBuffer<TestHashMap>(entity).AsMultiHashMap<TestHashMap, int, byte>();
        }

        private struct TestHashMap : IDynamicMultiHashMap<int, byte>
        {
            byte IDynamicHashMapBase<int, byte>.Value { get; }
        }
    }
}
