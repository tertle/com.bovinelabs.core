// <copyright file="DynamicVariableMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Iterators.Columns;
    using BovineLabs.Testing;
    using JetBrains.Annotations;
    using NUnit.Framework;

    public class DynamicVariableMapTests : ECSTestsFixture
    {
        [Test]
        public void WhenAddingItems_ShouldBeRetrievableByKey()
        {
            var map = this.CreateMap();

            map.Add(1, 0.5f, 5);
            map.Add(2, -1.5f, 7);
            map.Add(3, 35.5f, 9);

            // Test key-based retrieval
            Assert.IsTrue(map.TryGetValue(1, out var data, out var column));
            Assert.AreEqual(0.5f, data);
            Assert.AreEqual(5, column);

            Assert.IsTrue(map.TryGetValue(2, out data, out column));
            Assert.AreEqual(-1.5f, data);
            Assert.AreEqual(7, column);

            Assert.IsTrue(map.TryGetValue(3, out data, out column));
            Assert.AreEqual(35.5f, data);
            Assert.AreEqual(9, column);
        }

        [Test]
        public void WhenRemovingItems_ShouldUpdateCollectionCorrectly()
        {
            var map = this.CreateMap();
            map.Add(1, 0.5f, 5);
            map.Add(2, -1.5f, 7);
            map.Add(3, 35.5f, 9);

            Assert.AreEqual(3, map.Count, "Initial count should be 3");

            // Remove key 1 and verify it's gone
            map.Remove(1);
            Assert.AreEqual(2, map.Count, "Count should be 2 after removal");
            Assert.IsFalse(map.ContainsKey(1), "Removed key should not be found");

            // Make sure other items are still accessible by key
            Assert.IsTrue(map.TryGetValue(2, out var data, out var column));
            Assert.AreEqual(-1.5f, data, "Value for remaining item should be correct");
            Assert.AreEqual(7, column, "Column for remaining item should be correct");

            Assert.IsTrue(map.TryGetValue(3, out data, out column));
            Assert.AreEqual(9, column, "Column for remaining item should be correct");

            // Remove key 2 and check again
            map.Remove(2);
            Assert.AreEqual(1, map.Count, "Count should be 1 after second removal");
            Assert.IsTrue(map.ContainsKey(3), "Key 3 should still exist");
            Assert.IsFalse(map.ContainsKey(2), "Key 2 should be removed");
        }

        [Test]
        public void WhenAddingManyItems_ShouldHandleResizingCorrectly()
        {
            var map = this.CreateMap();
            const int itemCount = 1024;

            // Add enough items to force multiple internal resizes
            for (var i = 0; i < itemCount; i++)
            {
                map.Add(i, i / 0.37f, (short)(i % 13));
            }

            Assert.AreEqual(itemCount, map.Count, "Should contain all added items");

            // Sample check to verify data integrity after resizes
            for (var i = 0; i < itemCount; i += 100)
            {
                Assert.IsTrue(map.TryGetValue(i, out var value, out var column), $"Should find key {i}");
                Assert.AreEqual(i / 0.37f, value, 0.0001f, $"Value for key {i} should be correct");
                Assert.AreEqual((short)(i % 13), column, $"Column for key {i} should be correct");
            }
        }

        [Test]
        public void WhenChangingCapacity_ShouldPreserveAllData()
        {
            var map = this.CreateMap();
            map.Add(1, 0.5f, 5);
            map.Add(2, -1.5f, 7);
            map.Add(3, 35.5f, 9);

            // Test significant capacity increase
            map.Capacity = 1033;

            // Verify key access still works after resize
            Assert.IsTrue(map.TryGetValue(1, out var data, out var column));
            Assert.AreEqual(0.5f, data);
            Assert.AreEqual(5, column);

            Assert.IsTrue(map.TryGetValue(2, out data, out column));
            Assert.AreEqual(-1.5f, data);
            Assert.AreEqual(7, column);

            Assert.IsTrue(map.TryGetValue(3, out data, out column));
            Assert.AreEqual(35.5f, data);
            Assert.AreEqual(9, column);

            // Test capacity decrease but still above required size
            map.Capacity = 17;

            // Verify key access still works after downsizing
            Assert.IsTrue(map.TryGetValue(1, out data, out column));
            Assert.AreEqual(0.5f, data);
            Assert.AreEqual(5, column);

            Assert.IsTrue(map.TryGetValue(2, out data, out column));
            Assert.AreEqual(-1.5f, data);
            Assert.AreEqual(7, column);

            Assert.IsTrue(map.TryGetValue(3, out data, out column));
            Assert.AreEqual(35.5f, data);
            Assert.AreEqual(9, column);
        }

        [Test]
        public void WhenMapIsEmpty_PropertiesShouldBeCorrect()
        {
            var map = this.CreateMap();

            Assert.AreEqual(0, map.Count, "Count should be 0 for empty map");
            Assert.IsTrue(map.IsEmpty, "IsEmpty should be true for empty map");
            Assert.IsTrue(map.IsCreated, "IsCreated should be true after initialization");
        }

        [Test]
        public void WhenMapIsEmpty_OperationsShouldBehaveAppropriately()
        {
            var map = this.CreateMap();

            Assert.IsFalse(map.ContainsKey(123), "ContainsKey should return false for non-existent key");
            Assert.IsFalse(map.TryGetValue(123, out _, out _), "TryGetValue should return false for non-existent key");
            Assert.IsFalse(map.Remove(789), "Remove should return false for non-existent key");

            var itemCount = 0;
            foreach (var _ in map)
            {
                itemCount++;
            }

            Assert.AreEqual(0, itemCount, "Enumeration should yield no items for empty map");
        }

        [Test]
        public void WhenMapIsCleared_ItShouldBeReusable()
        {
            var map = this.CreateMap();

            // Add some items then clear them
            map.Add(100, 300.5f, 200);
            map.Add(101, 301.5f, 201);
            map.Add(102, 302.5f, 202);
            map.Clear();

            Assert.AreEqual(0, map.Count, "Count should be 0 after clear");
            Assert.IsTrue(map.IsEmpty, "IsEmpty should be true after clear");
            Assert.IsFalse(map.ContainsKey(100), "Items should not exist after clear");

            // Verify we can add new items after clearing
            map.Add(200, 400.5f, 300);
            Assert.AreEqual(1, map.Count, "Should be able to add items after clear");
            Assert.IsTrue(map.ContainsKey(200), "New items should be found after clear");
        }

        [Test]
        public void TryAddVsAdd_ShouldBehaveDifferently_WithDuplicateKeys()
        {
            var map = this.CreateMap();
            map.Add(42, 42.5f, 100);

            // TryAdd returns false and doesn't modify the map for duplicate keys
            var result = map.TryAdd(42, 43.5f, 200);
            Assert.IsFalse(result, "TryAdd should return false for duplicate key");

            Assert.IsTrue(map.TryGetValue(42, out var value, out var column));
            Assert.AreEqual(42.5f, value, "Original value should remain unchanged after TryAdd with duplicate key");
            Assert.AreEqual(100, column, "Original column should remain unchanged after TryAdd with duplicate key");

            // Add throws exception for duplicate keys
            Assert.Throws<ArgumentException>(() => map.Add(42, 44.5f, 300), "Add should throw for duplicate key");
        }

        [Test]
        public void Enumeration_ShouldYieldAllKeyValueColumnTriplets()
        {
            var map = this.CreateMap();

            // Create a test set for verification
            var expectedItems = new Dictionary<int, (float, short)>
            {
                { 10, (30.5f, 20) },
                { 11, (31.5f, 21) },
                { 12, (32.5f, 22) },
            };

            foreach (var kvp in expectedItems)
            {
                map.Add(kvp.Key, kvp.Value.Item1, kvp.Value.Item2);
            }

            // Collect all items through enumeration
            var actualItems = new Dictionary<int, (float, short)>();
            foreach (var item in map)
            {
                actualItems[item.Key] = (item.Value, item.Column);
            }

            Assert.AreEqual(expectedItems.Count, actualItems.Count, "Enumeration should yield all items");

            foreach (var kvp in expectedItems)
            {
                Assert.IsTrue(actualItems.ContainsKey(kvp.Key), $"Key {kvp.Key} should be present in enumeration");
                Assert.AreEqual(kvp.Value.Item1, actualItems[kvp.Key].Item1, "Value should match after enumeration");
                Assert.AreEqual(kvp.Value.Item2, actualItems[kvp.Key].Item2, "Column should match after enumeration");
            }
        }

        [Test]
        public void WithLargeNumberOfItems_MapShouldFunctionCorrectly()
        {
            var map = this.CreateMap();
            const int itemCount = 5000;

            // Add a large number of items to test performance and capacity handling
            for (var i = 0; i < itemCount; i++)
            {
                map.Add(i, i * 1.5f, (short)(i % 50));
            }

            Assert.AreEqual(itemCount, map.Count, "Should contain all added items");

            // Check a sampling of items to verify integrity
            for (var i = 0; i < itemCount; i += 123)
            {
                Assert.IsTrue(map.ContainsKey(i), $"Key {i} should exist");
                Assert.IsTrue(map.TryGetValue(i, out var value, out var column));
                Assert.AreEqual(i * 1.5f, value, 0.0001f, $"Value for key {i} should match");
                Assert.AreEqual((short)(i % 50), column, $"Column for key {i} should match");
            }

            // Remove half the items to test removal in a large collection
            for (var i = 0; i < itemCount; i += 2)
            {
                Assert.IsTrue(map.Remove(i), $"Should be able to remove key {i}");
            }

            Assert.AreEqual(itemCount / 2, map.Count, "Count should be half after removing every other item");

            // Verify the expected items remain and the removed ones are gone
            for (var i = 0; i < itemCount; i++)
            {
                var shouldExist = i % 2 == 1;
                Assert.AreEqual(shouldExist, map.ContainsKey(i), $"Key {i} existence should be {shouldExist}");
            }
        }

        [Test]
        public void ReusingRemovedKeys_ShouldWork()
        {
            var map = this.CreateMap();
            map.Add(111, 333.5f, 222);

            // Test removing and re-adding the same key with different values
            map.Remove(111);
            map.Add(111, 555.5f, 444);

            Assert.IsTrue(map.TryGetValue(111, out var value, out var column));
            Assert.AreEqual(555.5f, value, "Value should be updated for reused key");
            Assert.AreEqual(444, column, "Column should be updated for reused key");
        }

        [Test]
        public void AutoGrowthBehavior_ShouldPreserveAllData()
        {
            // Use small growth factor to ensure resize happens sooner
            var map = this.CreateMap(4);
            var initialCapacity = map.Capacity;

            // Add items until we detect a capacity change
            for (var i = 0; i < 32; i++)
            {
                map.Add(i, i * 10.5f, (short)(100 + i));

                if (map.Capacity > initialCapacity)
                {
                    // Verify all previously added data is intact after resize
                    for (var j = 0; j <= i; j++)
                    {
                        Assert.IsTrue(map.ContainsKey(j), $"Key {j} should exist after resize");
                        Assert.IsTrue(map.TryGetValue(j, out var val, out var idx));
                        Assert.AreEqual((short)(100 + j), idx, $"Column for key {j} should be preserved after resize");
                        Assert.AreEqual(j * 10.5f, val, 0.0001f, $"Value for key {j} should be preserved after resize");
                    }

                    break;
                }
            }

            Assert.Greater(map.Capacity, initialCapacity, "Capacity should have increased");
        }

        [Test]
        public void GetValueAtIndex_ShouldReturnCorrectData()
        {
            var map = this.CreateMap();

            map.Add(100, 100.5f, 10);
            map.Add(200, 200.5f, 20);
            map.Add(300, 300.5f, 30);

            // We need to find the actual storage indices to test GetValueAtIndex
            // We'll use enumeration to find the indices
            var foundEntries = new Dictionary<int, int>(); // key -> storage index

            foreach (var item in map)
            {
                // The KVC struct exposes the storage index
                foundEntries[item.Key] = item.Index;
            }

            // Test GetValueAtIndex with the actual storage indices
            foreach (var kvp in foundEntries)
            {
                var expectedKey = kvp.Key;
                var storageIndex = kvp.Value;

                map.GetAtIndex(storageIndex, out var actualKey, out var actualValue, out var actualColumn);

                Assert.AreEqual(expectedKey, actualKey, $"Key should match for storage index {storageIndex}");

                // Verify the value and column match what we expect
                Assert.IsTrue(map.TryGetValue(expectedKey, out var expectedValue, out var expectedColumn));
                Assert.AreEqual(expectedValue, actualValue, $"Value should match for key {expectedKey}");
                Assert.AreEqual(expectedColumn, actualColumn, $"Column should match for key {expectedKey}");
            }
        }

        [Test]
        public void Replace_WithExistingKey_ShouldUpdateColumnValue()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50);
            map.Add(200, 200.5f, 60);

            // Replace column value for existing key
            ref var value = ref map.Replace(100, 75);

            // Verify the column value was updated
            Assert.IsTrue(map.TryGetValue(100, out var retrievedValue, out var retrievedColumn));
            Assert.AreEqual(100.5f, retrievedValue, "Value should remain unchanged initially");
            Assert.AreEqual(75, retrievedColumn, "Column should be updated to new value");

            // Verify we can modify the value through the returned reference
            value = 999.5f;
            Assert.IsTrue(map.TryGetValue(100, out retrievedValue, out _));
            Assert.AreEqual(999.5f, retrievedValue, "Value should be updated through returned reference");
        }

        [Test]
        public void Replace_WithNonExistentKey_ShouldThrow()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50);

            Assert.Throws<ArgumentException>(() => map.Replace(999, 75), "Replace should throw ArgumentException for non-existent key");
        }

        [Test]
        public void Replace_WithSameColumnValue_ShouldStillUpdateReference()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50);

            // Replace with same column value
            ref var value = ref map.Replace(100, 50);

            // Verify we can still modify through reference
            value = 777.5f;
            Assert.IsTrue(map.TryGetValue(100, out var retrievedValue, out var retrievedColumn));
            Assert.AreEqual(777.5f, retrievedValue, "Value should be updated through reference");
            Assert.AreEqual(50, retrievedColumn, "Column should remain the same");
        }

        [Test]
        public void Replace_AfterResize_ShouldWorkCorrectly()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50);
            map.Add(200, 200.5f, 60);

            // Force resize
            map.Capacity = 1024;

            // Replace after resize
            ref var value = ref map.Replace(100, 75);
            value = 888.5f;

            Assert.IsTrue(map.TryGetValue(100, out var retrievedValue, out var retrievedColumn));
            Assert.AreEqual(888.5f, retrievedValue, "Value should be updated after resize");
            Assert.AreEqual(75, retrievedColumn, "Column should be updated after resize");
        }

        [Test]
        public void Replace_WithManyItems_ShouldHandleCorrectly()
        {
            var map = this.CreateMap();
            const int itemCount = 100;

            // Add many items
            for (var i = 0; i < itemCount; i++)
            {
                map.Add(i, i * 10.5f, (short)(i % 10));
            }

            // Replace every 10th item's column value
            for (var i = 0; i < itemCount; i += 10)
            {
                ref var value = ref map.Replace(i, 99);
                value *= 2; // Also modify the value
            }

            // Verify the replacements worked
            for (var i = 0; i < itemCount; i += 10)
            {
                Assert.IsTrue(map.TryGetValue(i, out var value, out var column));
                Assert.AreEqual(i * 10.5f * 2, value, 0.0001f, $"Value for key {i} should be doubled");
                Assert.AreEqual(99, column, $"Column for key {i} should be updated to 99");
            }

            // Verify non-replaced items are unchanged
            Assert.IsTrue(map.TryGetValue(5, out var value5, out var column5));
            Assert.AreEqual(5 * 10.5f, value5, 0.0001f, "Non-replaced item should have original value");
            Assert.AreEqual(5, column5, "Non-replaced item should have original column");
        }

        [Test]
        public void Replace_MultipleReplacementsOnSameKey_ShouldWork()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50);

            // Replace multiple times
            ref var value1 = ref map.Replace(100, 60);
            value1 = 200.5f;

            ref var value2 = ref map.Replace(100, 70);
            value2 = 300.5f;

            ref var value3 = ref map.Replace(100, 80);
            value3 = 400.5f;

            Assert.IsTrue(map.TryGetValue(100, out var finalValue, out var finalColumn));
            Assert.AreEqual(400.5f, finalValue, "Value should reflect last update");
            Assert.AreEqual(80, finalColumn, "Column should reflect last replacement");
        }

        [Test]
        public void Replace_OnEmptyMap_ShouldThrow()
        {
            var map = this.CreateMap();

            Assert.Throws<ArgumentException>(() => map.Replace(100, 50), "Replace should throw on empty map");
        }

        [Test]
        public void Replace_AfterClearAndReAdd_ShouldWork()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50);

            // Clear and re-add
            map.Clear();
            map.Add(100, 200.5f, 60);

            // Replace should work on the new entry
            ref var value = ref map.Replace(100, 70);
            value = 300.5f;

            Assert.IsTrue(map.TryGetValue(100, out var retrievedValue, out var retrievedColumn));
            Assert.AreEqual(300.5f, retrievedValue, "Value should be updated after clear/re-add/replace");
            Assert.AreEqual(70, retrievedColumn, "Column should be updated after clear/re-add/replace");
        }

        private DynamicVariableMap<int, float, short, MultiHashColumn<short>> CreateMap(int growth = 64)
        {
            var entity = this.Manager.CreateEntity(typeof(TestMap));
            return this
                .Manager
                .GetBuffer<TestMap>(entity)
                .InitializeVariableMap<TestMap, int, float, short, MultiHashColumn<short>>(0, growth)
                .AsVariableMap<TestMap, int, float, short, MultiHashColumn<short>>();
        }

        private struct TestMap : IDynamicVariableMap<int, float, short, MultiHashColumn<short>>
        {
            [UsedImplicitly]
            byte IDynamicVariableMap<int, float, short, MultiHashColumn<short>>.Value { get; }
        }
    }
}