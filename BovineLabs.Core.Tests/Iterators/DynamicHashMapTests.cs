// <copyright file="DynamicHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;

    public class DynamicHashMapTests : ECSTestsFixture
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
            const int count = 1024;

            var hashMap = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(hashMap.TryAdd(i + i, (byte)i));
            }

            Assert.AreEqual(count, hashMap.Count);

            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(hashMap.Remove(i + i));
            }

            Assert.AreEqual(0, hashMap.Count);
        }

        [Test]
        public void AddBatchUnsafe()
        {
            const int count = 1027;

            var hashMap = this.CreateHashMap();

            var keys = new NativeArray<int>(count, Allocator.Temp);
            var values = new NativeArray<byte>(count, Allocator.Temp);

            for (var i = 0; i < count; i++)
            {
                keys[i] = i;
                values[i] = (byte)(i % byte.MaxValue);
            }

            hashMap.AddBatchUnsafe(keys, values);

            Assert.AreEqual(count, hashMap.Count);

            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(hashMap.ContainsKey(i));
            }
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
            return this.Manager.GetBuffer<TestHashMap>(entity).InitializeHashMap<TestHashMap, int, byte>(0, MinGrowth).AsHashMap<TestHashMap, int, byte>();
        }

        private struct TestHashMap : IDynamicHashMap<int, byte>
        {
            byte IDynamicHashMap<int, byte>.Value { get; }
        }
    }
}
