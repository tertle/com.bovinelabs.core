// <copyright file="DynamicDictionaryTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using EntityValueEntry = BovineLabs.Core.Tests.Collections.DynamicDictionaryEntityValueEntry;
    using TestEntry = BovineLabs.Core.Tests.Collections.DynamicDictionaryTestEntry;

    public class DynamicDictionaryTests : ECSTestsFixture
    {
        [Test]
        public void IndexerSetAddsAndUpdates()
        {
            var entity = this.Manager.CreateEntity(typeof(TestEntry));
            var buffer = this.Manager.GetBuffer<TestEntry>(entity);

            var map = buffer.AsDynamicDictionary<int, int, TestEntry>();

            map[1] = 10;
            map[9] = 90;
            map[1] = 11;

            Assert.AreEqual(2, map.Count);
            Assert.AreEqual(11, map[1]);
            Assert.AreEqual(90, map[9]);
            Assert.IsFalse(map.TryAdd(1, 12));
            Assert.AreEqual(11, map[1]);
        }

        [Test]
        public void RemoveMaintainsCluster()
        {
            var entity = this.Manager.CreateEntity(typeof(TestEntry));
            var buffer = this.Manager.GetBuffer<TestEntry>(entity);

            var map = buffer.AsDynamicDictionary<int, int, TestEntry>();

            Assert.IsTrue(map.TryAdd(1, 10));
            Assert.IsTrue(map.TryAdd(9, 90));
            Assert.IsTrue(map.TryAdd(17, 170));

            Assert.IsTrue(map.TryRemove(9));
            Assert.IsFalse(map.TryGetValue(9, out _));

            Assert.IsTrue(map.TryGetValue(1, out var value1));
            Assert.AreEqual(10, value1);

            Assert.IsTrue(map.TryGetValue(17, out var value2));
            Assert.AreEqual(170, value2);
        }

        [Test]
        public void ReconstructAfterRemapUpdatesTags()
        {
            var entity = this.Manager.CreateEntity(typeof(TestEntry));
            var buffer = this.Manager.GetBuffer<TestEntry>(entity);

            var map = buffer.AsDynamicDictionary<int, int, TestEntry>();

            Assert.IsTrue(map.TryAdd(1, 10));
            Assert.IsTrue(map.TryAdd(9, 90));

            var entriesLength = buffer.Length;
            for (var i = 0; i < entriesLength; i++)
            {
                var entry = buffer[i];
                if (entry.TagField == 0)
                {
                    continue;
                }

                entry.KeyField += 100;
                buffer[i] = entry;
            }

            Assert.IsFalse(map.TryGetValue(101, out _));

            map.ReconstructAfterRemap();

            Assert.IsTrue(map.TryGetValue(101, out var value1));
            Assert.AreEqual(10, value1);

            Assert.IsTrue(map.TryGetValue(109, out var value2));
            Assert.AreEqual(90, value2);
        }

        [Test]
        public void TryAddResizesMultipleTimes()
        {
            var entity = this.Manager.CreateEntity(typeof(TestEntry));
            var buffer = this.Manager.GetBuffer<TestEntry>(entity);

            var map = buffer.AsDynamicDictionary<int, int, TestEntry>();

            Assert.IsTrue(map.TryAdd(1, 10));
            Assert.IsTrue(map.TryAdd(2, 20));
            Assert.IsTrue(map.TryAdd(3, 30));
            Assert.IsTrue(map.TryAdd(4, 40));
            Assert.IsTrue(map.TryAdd(5, 50));

            Assert.AreEqual(map.Capacity, buffer.Length);
            Assert.GreaterOrEqual(buffer.Capacity, map.Capacity + 1);
            Assert.GreaterOrEqual(map.Capacity, 5);

            for (var i = 1; i <= 5; i++)
            {
                Assert.IsTrue(map.TryGetValue(i, out var value));
                Assert.AreEqual(i * 10, value);
            }
        }

        [Test]
        public void EnsureCapacityRoundsToPowerOfTwo()
        {
            var entity = this.Manager.CreateEntity(typeof(TestEntry));
            var buffer = this.Manager.GetBuffer<TestEntry>(entity);

            var map = buffer.AsDynamicDictionary<int, int, TestEntry>();

            map.EnsureCapacity(3);

            Assert.AreEqual(4, map.Capacity);
            Assert.AreEqual(map.Capacity, buffer.Length);
            Assert.GreaterOrEqual(buffer.Capacity, map.Capacity + 1);

            Assert.IsTrue(map.TryAdd(1, 10));

            map.EnsureCapacity(9);

            Assert.AreEqual(16, map.Capacity);
            Assert.AreEqual(map.Capacity, buffer.Length);
            Assert.GreaterOrEqual(buffer.Capacity, map.Capacity + 1);
            Assert.IsTrue(map.TryGetValue(1, out var value));
            Assert.AreEqual(10, value);
        }

        [Test]
        public void RemoveMarksTombstone()
        {
            var entity = this.Manager.CreateEntity(typeof(TestEntry));
            var buffer = this.Manager.GetBuffer<TestEntry>(entity);

            var map = buffer.AsDynamicDictionary<int, int, TestEntry>();

            Assert.IsTrue(map.TryAdd(42, 420));
            Assert.IsTrue(map.TryRemove(42));

            var entriesLength = buffer.Length;
            var tombstoneCount = 0;
            for (var entryIndex = 0; entryIndex < entriesLength; entryIndex++)
            {
                var entryTag = buffer[entryIndex].TagField;
                if (entryTag != 0 && (entryTag & 1u) == 0)
                {
                    tombstoneCount++;
                }
            }

            Assert.AreEqual(1, tombstoneCount);
        }

        [Test]
        public void ClearRemovesEntries()
        {
            var entity = this.Manager.CreateEntity(typeof(TestEntry));
            var buffer = this.Manager.GetBuffer<TestEntry>(entity);

            var map = buffer.AsDynamicDictionary<int, int, TestEntry>();

            for (var keyIndex = 0; keyIndex < 6; keyIndex++)
            {
                Assert.IsTrue(map.TryAdd(200 + keyIndex, 2000 + keyIndex));
            }

            map.Clear();

            for (var keyIndex = 0; keyIndex < 6; keyIndex++)
            {
                Assert.IsFalse(map.TryGetValue(200 + keyIndex, out _));
            }

            for (var keyIndex = 0; keyIndex < 3; keyIndex++)
            {
                Assert.IsTrue(map.TryAdd(300 + keyIndex, 3000 + keyIndex));
            }

            for (var keyIndex = 0; keyIndex < 3; keyIndex++)
            {
                Assert.IsTrue(map.TryGetValue(300 + keyIndex, out var value));
                Assert.AreEqual(3000 + keyIndex, value);
            }
        }

        [Test]
        public void WriteRebuildsHeaderAfterSpareCapacityRemoved()
        {
            var entity = this.Manager.CreateEntity(typeof(TestEntry));
            var buffer = this.Manager.GetBuffer<TestEntry>(entity);

            var map = buffer.AsDynamicDictionary<int, int, TestEntry>();

            const int keyCount = 6;
            for (var keyIndex = 0; keyIndex < keyCount; keyIndex++)
            {
                Assert.IsTrue(map.TryAdd(keyIndex, keyIndex * 10));
            }

            var tableCapacity = map.Capacity;
            buffer.Capacity = buffer.Length;

            Assert.AreEqual(tableCapacity, buffer.Length);
            Assert.AreEqual(buffer.Length, buffer.Capacity);

            Assert.IsTrue(map.TryRemove(1));
            Assert.IsFalse(map.TryGetValue(1, out _));

            Assert.IsTrue(map.TryAdd(100, 1000));
            Assert.IsTrue(map.TryGetValue(100, out var addedValue));
            Assert.AreEqual(1000, addedValue);

            for (var keyIndex = 0; keyIndex < keyCount; keyIndex++)
            {
                if (keyIndex == 1)
                {
                    continue;
                }

                Assert.IsTrue(map.TryGetValue(keyIndex, out var value));
                Assert.AreEqual(keyIndex * 10, value);
            }

            Assert.AreEqual(map.Capacity, buffer.Length);
            Assert.GreaterOrEqual(buffer.Capacity, map.Capacity + 1);
        }

        [Test]
        public void LengthExcludesHeaderForRemappableValueEntries()
        {
            var target = this.Manager.CreateEntity();
            var entity = this.Manager.CreateEntity(typeof(EntityValueEntry));
            var buffer = this.Manager.GetBuffer<EntityValueEntry>(entity);

            var map = buffer.AsDynamicDictionary<int, Entity, EntityValueEntry>();

            map.EnsureCapacity(4);
            Assert.IsTrue(map.TryAdd(1, target));

            Assert.AreEqual(map.Capacity, buffer.Length);
            Assert.GreaterOrEqual(buffer.Capacity, map.Capacity + 1);
            Assert.IsTrue(map.TryGetValue(1, out var value));
            Assert.AreEqual(target, value);

            var occupiedEntries = 0;
            for (var entryIndex = 0; entryIndex < buffer.Length; entryIndex++)
            {
                var entry = buffer[entryIndex];
                if (entry.TagField == 0)
                {
                    Assert.AreEqual(Entity.Null, entry.ValueField);
                    continue;
                }

                occupiedEntries++;
                Assert.AreEqual(target, entry.ValueField);
            }

            Assert.AreEqual(1, occupiedEntries);
        }
    }
}
