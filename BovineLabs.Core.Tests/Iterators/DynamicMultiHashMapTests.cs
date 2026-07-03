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
        public void Add_AllowsDuplicateExactPairs()
        {
            var hashMap = this.CreateHashMap();

            hashMap.Add(7, 1);
            hashMap.Add(7, 1);
            hashMap.Add(7, 2);

            Assert.AreEqual(3, hashMap.Count);
            AssertValues(hashMap, 7, 2, 1, 1);
        }

        [Test]
        public void TryAddUniquePair_IgnoresDuplicatePair()
        {
            var hashMap = this.CreateHashMap();

            Assert.IsTrue(hashMap.TryAddUniquePair(7, (byte)1));
            Assert.IsFalse(hashMap.TryAddUniquePair(7, (byte)1));
            Assert.IsTrue(hashMap.TryAddUniquePair(7, (byte)2));

            Assert.AreEqual(2, hashMap.Count);
            AssertValues(hashMap, 7, 2, 1);
        }

        [Test]
        public void RemoveExactPair_PreservesOtherValuesForKey()
        {
            var hashMap = this.CreateHashMap();
            hashMap.Add(7, 1);
            hashMap.Add(7, 2);
            hashMap.Add(7, 3);
            hashMap.Add(8, 2);

            Assert.IsTrue(hashMap.Remove(7, (byte)2));
            Assert.IsFalse(hashMap.Remove(7, (byte)4));

            Assert.AreEqual(3, hashMap.Count);
            AssertValues(hashMap, 7, 3, 1);
            AssertValues(hashMap, 8, 2);
        }

        [Test]
        public void RemoveIterator_FirstValue_RemovesOnlyCurrentValue()
        {
            var hashMap = this.CreateHashMap();
            hashMap.Add(7, 1);
            hashMap.Add(7, 2);
            hashMap.Add(7, 3);

            Assert.IsTrue(hashMap.TryGetFirstValue(7, out var value, out var it));
            Assert.AreEqual(3, value);

            hashMap.Remove(it);

            Assert.AreEqual(2, hashMap.Count);
            AssertValues(hashMap, 7, 2, 1);
        }

        [Test]
        public void RemoveIterator_MiddleValue_CanContinueIteration()
        {
            var hashMap = this.CreateHashMap();
            hashMap.Add(7, 1);
            hashMap.Add(7, 2);
            hashMap.Add(7, 3);

            Assert.IsTrue(hashMap.TryGetFirstValue(7, out var value, out var it));
            Assert.AreEqual(3, value);
            Assert.IsTrue(hashMap.TryGetNextValue(out value, ref it));
            Assert.AreEqual(2, value);

            hashMap.Remove(it);

            Assert.AreEqual(2, hashMap.Count);
            Assert.IsTrue(hashMap.TryGetNextValue(out value, ref it));
            Assert.AreEqual(1, value);
            Assert.IsFalse(hashMap.TryGetNextValue(out _, ref it));
            AssertValues(hashMap, 7, 3, 1);
        }

        [Test]
        public void RemoveIterator_LastValue_RemovesTail()
        {
            var hashMap = this.CreateHashMap();
            hashMap.Add(7, 1);
            hashMap.Add(7, 2);
            hashMap.Add(7, 3);

            Assert.IsTrue(hashMap.TryGetFirstValue(7, out var value, out var it));
            Assert.AreEqual(3, value);
            Assert.IsTrue(hashMap.TryGetNextValue(out value, ref it));
            Assert.AreEqual(2, value);
            Assert.IsTrue(hashMap.TryGetNextValue(out value, ref it));
            Assert.AreEqual(1, value);

            hashMap.Remove(it);

            Assert.AreEqual(2, hashMap.Count);
            Assert.IsFalse(hashMap.TryGetNextValue(out _, ref it));
            AssertValues(hashMap, 7, 3, 2);
        }

        [Test]
        public void RemoveIterator_FirstValue_CanContinueIteration()
        {
            var hashMap = this.CreateHashMap();
            hashMap.Add(7, 1);
            hashMap.Add(7, 2);
            hashMap.Add(7, 3);

            Assert.IsTrue(hashMap.TryGetFirstValue(7, out var value, out var it));
            Assert.AreEqual(3, value);

            hashMap.Remove(it);

            Assert.IsTrue(hashMap.TryGetNextValue(out value, ref it));
            Assert.AreEqual(2, value);
            Assert.IsTrue(hashMap.TryGetNextValue(out value, ref it));
            Assert.AreEqual(1, value);
            Assert.IsFalse(hashMap.TryGetNextValue(out _, ref it));
        }

        [Test]
        public unsafe void RemoveIterator_PreservesCollidingKey()
        {
            var hashMap = this.CreateHashMap();
            var key = 1;
            var collidingKey = key + hashMap.Helper->BucketCapacity;

            hashMap.Add(key, 10);
            hashMap.Add(collidingKey, 20);
            hashMap.Add(key, 30);

            Assert.IsTrue(hashMap.TryGetFirstValue(key, out var value, out var it));
            Assert.AreEqual(30, value);

            hashMap.Remove(it);

            AssertValues(hashMap, key, 10);
            AssertValues(hashMap, collidingKey, 20);
        }

        [Test]
        public void RemoveIterator_ReusesFreedSlot()
        {
            var hashMap = this.CreateHashMap();
            hashMap.Add(7, 1);
            hashMap.Add(7, 2);

            Assert.IsTrue(hashMap.TryGetFirstValue(7, out _, out var removed));
            var removedEntryIndex = removed.EntryIndex;

            hashMap.Remove(removed);
            hashMap.Add(8, 3);

            Assert.IsTrue(hashMap.TryGetFirstValue(8, out var value, out var added));
            Assert.AreEqual(3, value);
            Assert.AreEqual(removedEntryIndex, added.EntryIndex);
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
        [Test]
        public void RemoveIterator_WithRemovedIterator_Throws()
        {
            var hashMap = this.CreateHashMap();
            hashMap.Add(7, 1);

            Assert.IsTrue(hashMap.TryGetFirstValue(7, out _, out var it));
            hashMap.Remove(it);

            Assert.Throws<ArgumentException>(() => hashMap.Remove(it));
        }
#endif

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

        private static void AssertValues(DynamicMultiHashMap<int, byte> hashMap, int key, params byte[] expected)
        {
            var values = hashMap.GetValuesForKey(key);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(values.MoveNext());
                Assert.AreEqual(expected[i], values.Current);
            }

            Assert.IsFalse(values.MoveNext());
        }

        private DynamicMultiHashMap<int, byte> CreateHashMap()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicMultiHashMapTestsBuffer));
            return this
                .Manager
                .GetBuffer<DynamicMultiHashMapTestsBuffer>(entity)
                .InitializeMultiHashMap<DynamicMultiHashMapTestsBuffer, int, byte>(0, MinGrowth)
                .AsMultiHashMap<DynamicMultiHashMapTestsBuffer, int, byte>();
        }

        private DynamicMultiHashMap<int, long> CreateHashMapLong()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicMultiHashMapTestsLongBuffer));
            return this
                .Manager
                .GetBuffer<DynamicMultiHashMapTestsLongBuffer>(entity)
                .InitializeMultiHashMap<DynamicMultiHashMapTestsLongBuffer, int, long>(0, MinGrowth)
                .AsMultiHashMap<DynamicMultiHashMapTestsLongBuffer, int, long>();
        }
    }
}
