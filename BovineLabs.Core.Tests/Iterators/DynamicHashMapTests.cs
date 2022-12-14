// <copyright file="DynamicHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;

    public class DynamicHashMapTests : ECSTestsFixture
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
                Assert.IsTrue(hashMap.TryAdd(i + i, (byte)i));
            }

            Assert.AreEqual(count, hashMap.Count());

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

        private DynamicHashMap<int, byte> CreateHashMap()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashMap));
            return this.Manager.GetBuffer<TestHashMap>(entity).AsHashMap<TestHashMap, int, byte>();
        }

        private struct TestHashMap : IDynamicHashMap<int, byte>
        {
            byte IDynamicHashMapBase<int, byte>.Value { get; }
        }
    }
}
