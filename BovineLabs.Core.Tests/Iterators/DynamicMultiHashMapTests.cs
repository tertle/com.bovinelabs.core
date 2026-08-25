// <copyright file="DynamicMultiHashMapTests.cs" company="BovineLabs">
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

    public class DynamicMultiHashMapTests : ECSTestsFixture
    {
        private const int MinGrowth = 64;

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
        public unsafe void Enumeration_WithDuplicateAndCollidingKeys_ReturnsEveryPair()
        {
            var hashMap = this.CreateHashMap();
            var key = 7;
            var collidingKey = key + hashMap.Helper->BucketCapacity;

            hashMap.Add(key, 1);
            hashMap.Add(key, 1);
            hashMap.Add(key, 2);
            hashMap.Add(collidingKey, 3);
            Assert.IsTrue(hashMap.Helper->IsDense);

            var actual = new List<(int Key, byte Value)>();
            foreach (var pair in hashMap)
            {
                actual.Add((pair.Key, pair.Value));
            }

            CollectionAssert.AreEquivalent(
                new[] { (key, (byte)1), (key, (byte)1), (key, (byte)2), (collidingKey, (byte)3) },
                actual);
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

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
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
