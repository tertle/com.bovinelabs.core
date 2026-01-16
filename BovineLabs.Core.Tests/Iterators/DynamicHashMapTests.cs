// <copyright file="DynamicHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

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

        [Test]
        public unsafe void ValuesPointer_IsAlignedToValueType()
        {
            var hashMap = this.CreateHashMapLong();

            var align = UnsafeUtility.AlignOf<long>();
            var valuesPtr = (ulong)hashMap.Helper->Values;
            Assert.AreEqual(0u, valuesPtr % (ulong)align);
        }

        [Test]
        public unsafe void Resize_WithHoles_RebuildsAndClearsFreeList()
        {
            const int count = 256;
            var hashMap = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                hashMap.Add(i, (byte)i);
            }

            // Create holes.
            for (var i = 0; i < count; i += 2)
            {
                Assert.IsTrue(hashMap.Remove(i));
            }

            Assert.AreEqual(count / 2, hashMap.Count);

            // Force a resize while the map has holes.
            hashMap.Capacity *= 2;

            var helper = hashMap.Helper;
            Assert.AreEqual(-1, helper->FirstFreeIdx);
            Assert.AreEqual(helper->Count, helper->AllocatedIndex);

            for (var i = 1; i < count; i += 2)
            {
                Assert.IsTrue(hashMap.TryGetValue(i, out var value));
                Assert.AreEqual((byte)i, value);
            }

            for (var i = 0; i < count; i += 2)
            {
                Assert.IsFalse(hashMap.TryGetValue(i, out _));
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
        [Test]
        public void AddBatchUnsafe_WithHoles_Throws()
        {
            var hashMap = this.CreateHashMap();

            for (var i = 0; i < 32; i++)
            {
                hashMap.Add(i, (byte)i);
            }

            // Create holes.
            for (var i = 0; i < 32; i += 2)
            {
                Assert.IsTrue(hashMap.Remove(i));
            }

            var keys = new NativeArray<int>(2, Allocator.Temp);
            var values = new NativeArray<byte>(2, Allocator.Temp);
            keys[0] = 100;
            keys[1] = 101;
            values[0] = 1;
            values[1] = 2;

            Assert.Throws<InvalidOperationException>(() => hashMap.AddBatchUnsafe(keys, values));
        }
#endif

        private DynamicHashMap<int, byte> CreateHashMap()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashMap));
            return this.Manager.GetBuffer<TestHashMap>(entity).InitializeHashMap<TestHashMap, int, byte>(0, MinGrowth).AsHashMap<TestHashMap, int, byte>();
        }

        private DynamicHashMap<int, long> CreateHashMapLong()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashMapLong));
            return this.Manager.GetBuffer<TestHashMapLong>(entity).InitializeHashMap<TestHashMapLong, int, long>(0, MinGrowth).AsHashMap<TestHashMapLong, int, long>();
        }

        private struct TestHashMap : IDynamicHashMap<int, byte>
        {
            /// <inheritdoc />
            byte IDynamicHashMap<int, byte>.Value { get; }
        }

        private struct TestHashMapLong : IDynamicHashMap<int, long>
        {
            byte IDynamicHashMap<int, long>.Value { get; }
        }
    }
}
