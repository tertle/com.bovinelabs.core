// <copyright file="DynamicMultiHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;

    public class DynamicMultiHashMapTests : ECSTestsFixture
    {
        private const int MinGrowth = 64;

        [Test]
        public void Capacity()
        {
            const int newCapacity = 128;

            var hashMap = this.CreateHashMap();
            Assert.AreEqual(MinGrowth, hashMap.Capacity);

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

            Assert.AreEqual(count * 2, hashMap.Count);

            for (var i = 0; i < count; i++)
            {
                Assert.AreEqual(2, hashMap.Remove(i + i));
            }

            Assert.AreEqual(0, hashMap.Count);
        }

        [Test]
        public void AddBatchUnsafe()
        {
            const int count = 1027;
            const int keyLimit = 89;

            var hashMap = this.CreateHashMap();

            var keys = new NativeArray<int>(count, Allocator.Temp);
            var values = new NativeArray<byte>(count, Allocator.Temp);

            for (var i = 0; i < count; i++)
            {
                keys[i] = i % keyLimit;
                values[i] = (byte)(i % byte.MaxValue);
            }

            hashMap.AddBatchUnsafe(keys, values);

            Assert.AreEqual(count, hashMap.Count);

            for (var i = 0; i < keyLimit; i++)
            {
                Assert.IsTrue(hashMap.ContainsKey(i));
            }
        }

        [Test]
        public void TryGetValue()
        {
            var hashMap = this.CreateHashMap();
            Assert.IsFalse(hashMap.TryGetFirstValue(47, out _, out _));

            hashMap.Add(47, 123);
            Assert.IsTrue(hashMap.TryGetFirstValue(47, out var result, out _));
            Assert.AreEqual(123, result);

            hashMap.Remove(47);
            Assert.IsFalse(hashMap.TryGetFirstValue(47, out _, out _));
        }

        private DynamicMultiHashMap<int, byte> CreateHashMap()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashMap));
            return this
                .Manager
                .GetBuffer<TestHashMap>(entity)
                .InitializeMultiHashMap<TestHashMap, int, byte>(0, MinGrowth)
                .AsMultiHashMap<TestHashMap, int, byte>();
        }

        private struct TestHashMap : IDynamicMultiHashMap<int, byte>
        {
            byte IDynamicMultiHashMap<int, byte>.Value { get; }
        }
    }
}
