// <copyright file="DynamicHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System.Collections.Generic;
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

        [Test]
        public void IndexerSetExisting()
        {
            var hashMap = this.CreateHashMap();

            // Add initial value
            hashMap.Add(42, 50);
            Assert.AreEqual(50, hashMap[42]);

            // Test indexer setter on existing key (tests optimized path)
            hashMap[42] = 75;
            Assert.AreEqual(75, hashMap[42]);
            Assert.AreEqual(1, hashMap.Count); // Should still be 1 element
        }

        [Test]
        public void IndexerSetNew()
        {
            var hashMap = this.CreateHashMap();

            // Test indexer setter on new key (tests optimized path)
            hashMap[42] = 100;
            Assert.AreEqual(100, hashMap[42]);
            Assert.AreEqual(1, hashMap.Count);
        }

        [Test]
        public void GetOrAddRef()
        {
            var hashMap = this.CreateHashMap();

            // Test with new key
            ref var value1 = ref hashMap.GetOrAddRef(42, 50);
            Assert.AreEqual(50, value1);
            Assert.AreEqual(1, hashMap.Count);

            // Modify through reference
            value1 = 75;
            Assert.AreEqual(75, hashMap[42]);

            // Test with existing key
            ref var value2 = ref hashMap.GetOrAddRef(42, 125);
            Assert.AreEqual(75, value2); // Should return existing value, not default
            Assert.AreEqual(1, hashMap.Count); // Still only one element
        }

        [Test]
        public void GetOrAddRefWithFlag()
        {
            var hashMap = this.CreateHashMap();

            // Test with new key
            ref var value1 = ref hashMap.GetOrAddRef(42, out var wasAdded1, 50);
            Assert.IsTrue(wasAdded1);
            Assert.AreEqual(50, value1);

            // Test with existing key
            ref var value2 = ref hashMap.GetOrAddRef(42, out var wasAdded2, 125);
            Assert.IsFalse(wasAdded2);
            Assert.AreEqual(50, value2); // Should return existing value
        }

        [Test]
        public void EnumerationConsistency()
        {
            const int count = 100;
            var hashMap = this.CreateHashMap();

            // Add elements
            for (var i = 0; i < count; i++)
            {
                hashMap.Add(i, (byte)(i % 255));
            }

            // Test enumeration gives all elements
            var found = new HashSet<int>();
            foreach (var kvp in hashMap)
            {
                Assert.IsFalse(found.Contains(kvp.Key), $"Duplicate key {kvp.Key} found during enumeration");
                found.Add(kvp.Key);
                Assert.AreEqual((byte)(kvp.Key % 255), kvp.Value);
            }

            Assert.AreEqual(count, found.Count);
        }

        [Test]
        public void ResizeStressTest()
        {
            var hashMap = this.CreateHashMap();

            // Force multiple resizes by adding many elements
            const int count = 1000;
            for (var i = 0; i < count; i++)
            {
                hashMap.Add(i, (byte)(i % 255));
            }

            // Verify all elements are still present and correct
            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(hashMap.TryGetValue(i, out var value), $"Key {i} not found after resize");
                Assert.AreEqual((byte)(i % 255), value, $"Incorrect value for key {i} after resize");
            }

            Assert.AreEqual(count, hashMap.Count);
        }

        private DynamicHashMap<int, byte> CreateHashMap()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashMap));
            return this.Manager.GetBuffer<TestHashMap>(entity).InitializeHashMap<TestHashMap, int, byte>(0, MinGrowth).AsHashMap<TestHashMap, int, byte>();
        }

        private struct TestHashMap : IDynamicHashMap<int, byte>
        {
            /// <inheritdoc />
            byte IDynamicHashMap<int, byte>.Value { get; }
        }
    }
}
