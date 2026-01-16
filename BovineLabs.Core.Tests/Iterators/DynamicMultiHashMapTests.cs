// <copyright file="DynamicMultiHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

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
            const int count = 128;
            var hashMap = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                hashMap.Add(i, (byte)i);
            }

            // Create holes by removing half the keys.
            for (var i = 0; i < count; i += 2)
            {
                Assert.AreEqual(1, hashMap.Remove(i));
            }

            Assert.AreEqual(count / 2, hashMap.Count);

            // Force a resize while the map has holes.
            hashMap.Capacity *= 2;

            var helper = hashMap.Helper;
            Assert.AreEqual(-1, helper->FirstFreeIdx);
            Assert.AreEqual(helper->Count, helper->AllocatedIndex);

            for (var i = 1; i < count; i += 2)
            {
                Assert.IsTrue(hashMap.TryGetFirstValue(i, out var value, out _));
                Assert.AreEqual((byte)i, value);
            }

            for (var i = 0; i < count; i += 2)
            {
                Assert.IsFalse(hashMap.TryGetFirstValue(i, out _, out _));
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
                Assert.AreEqual(1, hashMap.Remove(i));
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

        private DynamicMultiHashMap<int, byte> CreateHashMap()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashMap));
            return this
                .Manager
                .GetBuffer<TestHashMap>(entity)
                .InitializeMultiHashMap<TestHashMap, int, byte>(0, MinGrowth)
                .AsMultiHashMap<TestHashMap, int, byte>();
        }

        private DynamicMultiHashMap<int, long> CreateHashMapLong()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashMapLong));
            return this
                .Manager
                .GetBuffer<TestHashMapLong>(entity)
                .InitializeMultiHashMap<TestHashMapLong, int, long>(0, MinGrowth)
                .AsMultiHashMap<TestHashMapLong, int, long>();
        }

        private struct TestHashMap : IDynamicMultiHashMap<int, byte>
        {
            byte IDynamicMultiHashMap<int, byte>.Value { get; }
        }

        private struct TestHashMapLong : IDynamicMultiHashMap<int, long>
        {
            byte IDynamicMultiHashMap<int, long>.Value { get; }
        }
    }
}
