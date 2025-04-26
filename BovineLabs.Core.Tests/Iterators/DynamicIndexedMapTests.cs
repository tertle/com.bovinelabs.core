// <copyright file="DynamicIndexedMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using JetBrains.Annotations;
    using NUnit.Framework;

    public class DynamicIndexedMapTests : ECSTestsFixture
    {
        [Test]
        public void WhenAddingItems_ShouldBeRetrievableByKeyAndIndex()
        {
            var map = this.CreateMap();

            map.Add(1, 5, 0.5f);
            map.Add(2, 5, -1.5f);
            map.Add(3, 7, 35.5f);

            // Test key-based retrieval
            Assert.IsTrue(map.TryGetValue(1, out var index, out var data));
            Assert.AreEqual(5, index);
            Assert.AreEqual(0.5f, data);

            Assert.IsTrue(map.TryGetValue(2, out index, out data));
            Assert.AreEqual(5, index);
            Assert.AreEqual(-1.5f, data);

            Assert.IsTrue(map.TryGetValue(3, out index, out data));
            Assert.AreEqual(7, index);
            Assert.AreEqual(35.5f, data);

            // Test index-based retrieval without assuming order
            var foundKeysForIndex5 = new HashSet<int>();
            var foundValuesForIndex5 = new HashSet<float>();

            if (map.TryGetFirstValue(5, out var key, out data, out var it))
            {
                foundKeysForIndex5.Add(key);
                foundValuesForIndex5.Add(data);

                while (map.TryGetNextValue(out key, out data, ref it))
                {
                    foundKeysForIndex5.Add(key);
                    foundValuesForIndex5.Add(data);
                }
            }

            Assert.AreEqual(2, foundKeysForIndex5.Count, "Should find 2 keys with index 5");
            Assert.IsTrue(foundKeysForIndex5.Contains(1), "Should find key 1");
            Assert.IsTrue(foundKeysForIndex5.Contains(2), "Should find key 2");
            Assert.IsTrue(foundValuesForIndex5.Contains(0.5f), "Should find value 0.5");
            Assert.IsTrue(foundValuesForIndex5.Contains(-1.5f), "Should find value -1.5");

            // Test index 7 similarly
            if (map.TryGetFirstValue(7, out key, out data, out _))
            {
                Assert.AreEqual(3, key, "Should find key 3 for index 7");
                Assert.AreEqual(35.5f, data, "Should find value 35.5 for index 7");
            }
            else
            {
                Assert.Fail("Failed to find any keys with index 7");
            }
        }

        [Test]
        public void WhenRemovingItems_ShouldUpdateCollectionCorrectly()
        {
            var map = this.CreateMap();
            map.Add(1, 5, 0.5f);
            map.Add(2, 5, -1.5f);
            map.Add(3, 7, 35.5f);

            Assert.AreEqual(3, map.Count, "Initial count should be 3");

            // Remove key 1 and verify it's gone
            map.Remove(1);
            Assert.AreEqual(2, map.Count, "Count should be 2 after removal");
            Assert.IsFalse(map.ContainsKey(1), "Removed key should not be found");

            // Make sure other items are still accessible by key
            Assert.IsTrue(map.TryGetValue(2, out var index, out var item));
            Assert.AreEqual(5, index, "Index for remaining item should be correct");
            Assert.AreEqual(-1.5f, item, "Value for remaining item should be correct");

            Assert.IsTrue(map.TryGetValue(3, out index, out item));
            Assert.AreEqual(7, index, "Index for remaining item should be correct");

            // Verify we can still find the remaining item by index 5a
            var foundKeysForIndex5 = new HashSet<int>();
            if (map.TryGetFirstValue(5, out var key, out _, out var it))
            {
                foundKeysForIndex5.Add(key);
                while (map.TryGetNextValue(out key, out _, ref it))
                {
                    foundKeysForIndex5.Add(key);
                }
            }

            Assert.AreEqual(1, foundKeysForIndex5.Count, "Should find 1 key with index 5");
            Assert.IsTrue(foundKeysForIndex5.Contains(2), "Should find key 2 for index 5");

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
                map.Add(i, (short)(i % 13), i / 0.37f);
            }

            Assert.AreEqual(itemCount, map.Count, "Should contain all added items");

            // Sample check to verify data integrity after resizes
            for (var i = 0; i < itemCount; i += 100)
            {
                Assert.IsTrue(map.TryGetValue(i, out var index, out var value), $"Should find key {i}");
                Assert.AreEqual((short)(i % 13), index, $"Index for key {i} should be correct");
                Assert.AreEqual(i / 0.37f, value, 0.0001f, $"Value for key {i} should be correct");
            }
        }

        [Test]
        public void WhenChangingCapacity_ShouldPreserveAllData()
        {
            var map = this.CreateMap();
            map.Add(1, 5, 0.5f);
            map.Add(2, 5, -1.5f);
            map.Add(3, 7, 35.5f);

            // Test significant capacity increase
            map.Capacity = 1033;

            // Verify key access still works after resize
            Assert.IsTrue(map.TryGetValue(1, out var index, out var data));
            Assert.AreEqual(5, index);
            Assert.AreEqual(0.5f, data);

            Assert.IsTrue(map.TryGetValue(2, out index, out data));
            Assert.AreEqual(5, index);
            Assert.AreEqual(-1.5f, data);

            Assert.IsTrue(map.TryGetValue(3, out index, out data));
            Assert.AreEqual(7, index);
            Assert.AreEqual(35.5f, data);

            // Verify index access still works after resize
            var foundForIndex5 = new Dictionary<int, float>();
            if (map.TryGetFirstValue(5, out var key, out data, out var it))
            {
                foundForIndex5[key] = data;
                while (map.TryGetNextValue(out key, out data, ref it))
                {
                    foundForIndex5[key] = data;
                }
            }

            Assert.AreEqual(2, foundForIndex5.Count, "Should find 2 entries with index 5");
            Assert.IsTrue(foundForIndex5.ContainsKey(1), "Should find key 1 for index 5");
            Assert.IsTrue(foundForIndex5.ContainsKey(2), "Should find key 2 for index 5");
            Assert.AreEqual(0.5f, foundForIndex5[1], "Value for key 1 should be 0.5");
            Assert.AreEqual(-1.5f, foundForIndex5[2], "Value for key 2 should be -1.5");

            var foundForIndex7 = new Dictionary<int, float>();
            if (map.TryGetFirstValue(7, out key, out data, out it))
            {
                foundForIndex7[key] = data;
                while (map.TryGetNextValue(out key, out data, ref it))
                {
                    foundForIndex7[key] = data;
                }
            }

            Assert.AreEqual(1, foundForIndex7.Count, "Should find 1 entry with index 7");
            Assert.IsTrue(foundForIndex7.ContainsKey(3), "Should find key 3 for index 7");
            Assert.AreEqual(35.5f, foundForIndex7[3], "Value for key 3 should be 35.5");

            // Test capacity decrease but still above required size
            map.Capacity = 17;

            // Verify key access still works after downsizing
            Assert.IsTrue(map.TryGetValue(1, out index, out data));
            Assert.AreEqual(5, index);
            Assert.AreEqual(0.5f, data);

            Assert.IsTrue(map.TryGetValue(2, out index, out data));
            Assert.AreEqual(5, index);
            Assert.AreEqual(-1.5f, data);

            Assert.IsTrue(map.TryGetValue(3, out index, out data));
            Assert.AreEqual(7, index);
            Assert.AreEqual(35.5f, data);

            // Verify index access still works after downsizing
            foundForIndex5.Clear();
            if (map.TryGetFirstValue(5, out key, out data, out it))
            {
                foundForIndex5[key] = data;
                while (map.TryGetNextValue(out key, out data, ref it))
                {
                    foundForIndex5[key] = data;
                }
            }

            Assert.AreEqual(2, foundForIndex5.Count, "Should still find 2 entries with index 5 after downsize");
            Assert.IsTrue(foundForIndex5.ContainsKey(1), "Should find key 1 for index 5 after downsize");
            Assert.IsTrue(foundForIndex5.ContainsKey(2), "Should find key 2 for index 5 after downsize");
            Assert.AreEqual(0.5f, foundForIndex5[1], "Value for key 1 should be 0.5 after downsize");
            Assert.AreEqual(-1.5f, foundForIndex5[2], "Value for key 2 should be -1.5 after downsize");

            foundForIndex7.Clear();
            if (map.TryGetFirstValue(7, out key, out data, out it))
            {
                foundForIndex7[key] = data;
                while (map.TryGetNextValue(out key, out data, ref it))
                {
                    foundForIndex7[key] = data;
                }
            }

            Assert.AreEqual(1, foundForIndex7.Count, "Should find 1 entry with index 7 after downsize");
            Assert.IsTrue(foundForIndex7.ContainsKey(3), "Should find key 3 for index 7 after downsize");
            Assert.AreEqual(35.5f, foundForIndex7[3], "Value for key 3 should be 35.5 after downsize");
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
            Assert.IsFalse(map.TryGetFirstValue(456, out _, out _, out _), "TryGetFirstValue should return false for non-existent index");
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
            map.Add(100, 200, 300.5f);
            map.Add(101, 201, 301.5f);
            map.Add(102, 202, 302.5f);
            map.Clear();

            Assert.AreEqual(0, map.Count, "Count should be 0 after clear");
            Assert.IsTrue(map.IsEmpty, "IsEmpty should be true after clear");
            Assert.IsFalse(map.ContainsKey(100), "Items should not exist after clear");

            // Verify we can add new items after clearing
            map.Add(200, 300, 400.5f);
            Assert.AreEqual(1, map.Count, "Should be able to add items after clear");
            Assert.IsTrue(map.ContainsKey(200), "New items should be found after clear");
        }

        [Test]
        public void TryAddVsAdd_ShouldBehaveDifferently_WithDuplicateKeys()
        {
            var map = this.CreateMap();
            map.Add(42, 100, 42.5f);

            // TryAdd returns false and doesn't modify the map for duplicate keys
            var result = map.TryAdd(42, 200, 43.5f);
            Assert.IsFalse(result, "TryAdd should return false for duplicate key");

            Assert.IsTrue(map.TryGetValue(42, out var index, out var value));
            Assert.AreEqual(100, index, "Original index should remain unchanged after TryAdd with duplicate key");
            Assert.AreEqual(42.5f, value, "Original value should remain unchanged after TryAdd with duplicate key");

            // Add throws exception for duplicate keys
            Assert.Throws<ArgumentException>(() => map.Add(42, 300, 44.5f), "Add should throw for duplicate key");
        }

        [Test]
        public void Enumeration_ShouldYieldAllKeyIndexValueTriplets()
        {
            var map = this.CreateMap();

            // Create a test set for verification
            var expectedItems = new Dictionary<int, (short, float)>
            {
                { 10, (20, 30.5f) },
                { 11, (21, 31.5f) },
                { 12, (22, 32.5f) },
            };

            foreach (var kvp in expectedItems)
            {
                map.Add(kvp.Key, kvp.Value.Item1, kvp.Value.Item2);
            }

            // Collect all items through enumeration
            var actualItems = new Dictionary<int, (short, float)>();
            foreach (var item in map)
            {
                actualItems[item.Key] = (item.Indexed, item.Value);
            }

            Assert.AreEqual(expectedItems.Count, actualItems.Count, "Enumeration should yield all items");

            foreach (var kvp in expectedItems)
            {
                Assert.IsTrue(actualItems.ContainsKey(kvp.Key), $"Key {kvp.Key} should be present in enumeration");
                Assert.AreEqual(kvp.Value.Item1, actualItems[kvp.Key].Item1, "Index should match after enumeration");
                Assert.AreEqual(kvp.Value.Item2, actualItems[kvp.Key].Item2, "Value should match after enumeration");
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
                map.Add(i, (short)(i % 50), i * 1.5f);
            }

            Assert.AreEqual(itemCount, map.Count, "Should contain all added items");

            // Check a sampling of items to verify integrity
            for (var i = 0; i < itemCount; i += 123)
            {
                Assert.IsTrue(map.ContainsKey(i), $"Key {i} should exist");
                Assert.IsTrue(map.TryGetValue(i, out var index, out var value));
                Assert.AreEqual((short)(i % 50), index, $"Index for key {i} should match");
                Assert.AreEqual(i * 1.5f, value, 0.0001f, $"Value for key {i} should match");
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
        public void SameIndex_ShouldAllowMultipleDifferentKeys()
        {
            var map = this.CreateMap();
            const short commonIndex = 42;

            // Add multiple keys with the same index to test multi-key per index functionality
            for (var i = 1; i <= 5; i++)
            {
                map.Add(i * 10, commonIndex, i * 100.5f);
            }

            // Retrieve all values for the common index
            var foundKeys = new HashSet<int>();
            var foundValues = new HashSet<float>();

            if (map.TryGetFirstValue(commonIndex, out var key, out var value, out var it))
            {
                foundKeys.Add(key);
                foundValues.Add(value);

                while (map.TryGetNextValue(out key, out value, ref it))
                {
                    foundKeys.Add(key);
                    foundValues.Add(value);
                }
            }

            Assert.AreEqual(5, foundKeys.Count, "Should find all 5 keys with the same index");
            Assert.AreEqual(5, foundValues.Count, "Should find all 5 values with the same index");

            for (var i = 1; i <= 5; i++)
            {
                Assert.IsTrue(foundKeys.Contains(i * 10), $"Should find key {i * 10}");
                Assert.IsTrue(foundValues.Contains(i * 100.5f), $"Should find value {i * 100.5f}");
            }
        }

        [Test]
        public void AfterRemoval_IndexesShouldBeUpdatedCorrectly()
        {
            var map = this.CreateMap();

            // Setup multiple keys with the same index to test proper index chain updates
            map.Add(10, 50, 100.5f);
            map.Add(20, 50, 200.5f);
            map.Add(30, 51, 300.5f);

            var removed = map.Remove(10);
            Assert.IsTrue(removed, "Remove should return true for existing key");

            // Verify the other key with the same index is still accessible
            Assert.IsTrue(map.TryGetFirstValue(50, out var key, out var value, out var it));
            Assert.AreEqual(20, key, "Should still find the other key with the same index");
            Assert.AreEqual(200.5f, value, "Should find the correct value for the remaining key");
            Assert.IsFalse(map.TryGetNextValue(out _, out _, ref it), "Should not find any more items with this index");

            // Verify other indices are unaffected
            Assert.IsTrue(map.TryGetFirstValue(51, out key, out value, out _));
            Assert.AreEqual(30, key, "Should find the key with a different index");
            Assert.AreEqual(300.5f, value, "Should find the correct value for the different index");
        }

        [Test]
        public void ReusingRemovedKeys_ShouldWork()
        {
            var map = this.CreateMap();
            map.Add(111, 222, 333.5f);

            // Test removing and re-adding the same key with different values
            map.Remove(111);
            map.Add(111, 444, 555.5f);

            Assert.IsTrue(map.TryGetValue(111, out var index, out var value));
            Assert.AreEqual(444, index, "Index should be updated for reused key");
            Assert.AreEqual(555.5f, value, "Value should be updated for reused key");

            // Verify the old index association is removed
            Assert.IsFalse(map.TryGetFirstValue(222, out _, out _, out _), "Old index should have no entries");

            // Verify the new index association is created
            Assert.IsTrue(map.TryGetFirstValue(444, out var foundKey, out var foundValue, out _));
            Assert.AreEqual(111, foundKey, "Should find the correct key with the new index");
            Assert.AreEqual(555.5f, foundValue, "Should find the correct value with the new index");
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
                map.Add(i, (short)(100 + i), i * 10.5f);

                if (map.Capacity > initialCapacity)
                {
                    // Verify all previously added data is intact after resize
                    for (var j = 0; j <= i; j++)
                    {
                        Assert.IsTrue(map.ContainsKey(j), $"Key {j} should exist after resize");
                        Assert.IsTrue(map.TryGetValue(j, out var idx, out var val));
                        Assert.AreEqual((short)(100 + j), idx, $"Index for key {j} should be preserved after resize");
                        Assert.AreEqual(j * 10.5f, val, 0.0001f, $"Value for key {j} should be preserved after resize");
                    }

                    break;
                }
            }

            Assert.Greater(map.Capacity, initialCapacity, "Capacity should have increased");
        }

        [Test]
        public void CustomStructs_ShouldWorkAsGenericParameters()
        {
            // Test with custom struct types for key, index and value
            var entity = this.Manager.CreateEntity(typeof(CustomStructMap));
            var map = this
                .Manager
                .GetBuffer<CustomStructMap>(entity)
                .InitializeIndexed<CustomStructMap, CustomKey, CustomIndex, CustomValue>()
                .AsIndexedMap<CustomStructMap, CustomKey, CustomIndex, CustomValue>();

            var key1 = new CustomKey
            {
                Id = 1,
                Tag = 100,
            };

            var key2 = new CustomKey
            {
                Id = 2,
                Tag = 200,
            };

            var index1 = new CustomIndex { GroupId = 50 };
            var index2 = new CustomIndex { GroupId = 51 };
            var value1 = new CustomValue
            {
                Amount = 1.5f,
                Priority = 10,
            };

            var value2 = new CustomValue
            {
                Amount = 2.5f,
                Priority = 20,
            };

            map.Add(key1, index1, value1);
            map.Add(key2, index2, value2);

            Assert.AreEqual(2, map.Count, "Should have 2 items");

            // Check retrieval by key with custom struct types
            Assert.IsTrue(map.TryGetValue(key1, out var foundIndex, out var foundValue));
            Assert.AreEqual(index1, foundIndex, "Retrieved index should match");
            Assert.AreEqual(value1, foundValue, "Retrieved value should match");

            // Check retrieval by index with custom struct types
            Assert.IsTrue(map.TryGetFirstValue(index1, out var foundKey, out var foundVal, out _));
            Assert.AreEqual(key1, foundKey, "Retrieved key should match");
            Assert.AreEqual(value1, foundVal, "Retrieved value should match");
        }

        [Test]
        public void EnumWrappers_ShouldWorkAsGenericParameters()
        {
            // Test with enum wrapper structs to satisfy IEquatable constraint
            var entity = this.Manager.CreateEntity(typeof(EnumWrapperMap));
            var map = this
                .Manager
                .GetBuffer<EnumWrapperMap>(entity)
                .InitializeIndexed<EnumWrapperMap, TestEnumKey, CategoryEnumIndex, byte>()
                .AsIndexedMap<EnumWrapperMap, TestEnumKey, CategoryEnumIndex, byte>();

            map.Add(new TestEnumKey(TestEnum.Value1), new CategoryEnumIndex(Category.CategoryA), 10);
            map.Add(new TestEnumKey(TestEnum.Value2), new CategoryEnumIndex(Category.CategoryA), 20);
            map.Add(new TestEnumKey(TestEnum.Value3), new CategoryEnumIndex(Category.CategoryB), 30);

            Assert.AreEqual(3, map.Count, "Should have 3 items");

            // Check key lookup with enum wrapper structs
            Assert.IsTrue(map.TryGetValue(new TestEnumKey(TestEnum.Value2), out var category, out var value));
            Assert.AreEqual(new CategoryEnumIndex(Category.CategoryA), category, "Category should match");
            Assert.AreEqual(20, value, "Value should match");

            // Check index lookup with enum wrapper structs
            var foundCount = 0;
            if (map.TryGetFirstValue(new CategoryEnumIndex(Category.CategoryA), out var key, out _, out var it))
            {
                foundCount++;
                Assert.IsTrue(key.Equals(new TestEnumKey(TestEnum.Value1)) || key.Equals(new TestEnumKey(TestEnum.Value2)), "Should find Value1 or Value2");

                if (map.TryGetNextValue(out key, out _, ref it))
                {
                    foundCount++;
                    Assert.IsTrue(key.Equals(new TestEnumKey(TestEnum.Value1)) || key.Equals(new TestEnumKey(TestEnum.Value2)), "Should find Value1 or Value2");
                }
            }

            Assert.AreEqual(2, foundCount, "Should find 2 items with CategoryA");
        }

        [Test]
        public void Flatten_ShouldRemoveHolesAndPreserveData()
        {
            var map = this.CreateMap();

            // Add several items
            for (int i = 0; i < 10; i++)
            {
                map.Add(i, (short)(i % 3), i * 1.5f);
            }

            // Remove some items to create "holes"
            map.Remove(2);
            map.Remove(5);
            map.Remove(8);

            // Capture the data before flattening
            var beforeData = new Dictionary<int, (short, float)>();
            foreach (var item in map)
            {
                beforeData[item.Key] = (item.Indexed, item.Value);
            }

            // Call Flatten
            map.Flatten();

            // Verify all data is preserved
            Assert.AreEqual(7, map.Count, "Count should be unchanged after flattening");
            foreach (var kvp in beforeData)
            {
                Assert.IsTrue(map.TryGetValue(kvp.Key, out var index, out var value));
                Assert.AreEqual(kvp.Value.Item1, index, $"Index for key {kvp.Key} should be preserved");
                Assert.AreEqual(kvp.Value.Item2, value, $"Value for key {kvp.Key} should be preserved");
            }

            // Verify we can still enumerate all items
            var afterData = new Dictionary<int, (short, float)>();
            foreach (var item in map)
            {
                afterData[item.Key] = (item.Indexed, item.Value);
            }

            Assert.AreEqual(beforeData.Count, afterData.Count, "Same number of items should be enumerable after flattening");
            foreach (var kvp in beforeData)
            {
                Assert.IsTrue(afterData.ContainsKey(kvp.Key), $"Key {kvp.Key} should still be present after flattening");
                Assert.AreEqual(kvp.Value, afterData[kvp.Key], $"Data for key {kvp.Key} should be unchanged");
            }
        }

        [Test]
        public void Flatten_WithLargeMap_ShouldOptimizeCapacity()
        {
            var map = this.CreateMap();
            const int initialCount = 1000;

            // Add a large number of items
            for (int i = 0; i < initialCount; i++)
            {
                map.Add(i, (short)(i % 50), i * 0.5f);
            }

            // Remove a significant portion to create holes
            for (int i = 0; i < initialCount; i += 2)
            {
                map.Remove(i);
            }

            var countBeforeFlattening = map.Count;
            var capacityBeforeFlattening = map.Capacity;

            map.Flatten();

            Assert.AreEqual(countBeforeFlattening, map.Count, "Count should be unchanged after flattening");
            Assert.LessOrEqual(map.Capacity, capacityBeforeFlattening, "Capacity should be optimized (equal or less) after flattening");

            // Verify all remaining data is intact
            for (int i = 1; i < initialCount; i += 2)
            {
                Assert.IsTrue(map.TryGetValue(i, out var index, out var value));
                Assert.AreEqual((short)(i % 50), index, $"Index for key {i} should be preserved");
                Assert.AreEqual(i * 0.5f, value, 0.0001f, $"Value for key {i} should be preserved");
            }
        }

        [Test]
        public void UnsafeRemoveRangeShiftDown_ShouldRemoveElementsAndShiftRemaining()
        {
            var map = this.CreateMap();

            // Add items in a way that ensures predictable storage order
            for (int i = 0; i < 10; i++)
            {
                map.Add(i, (short)(i % 3), i * 1.5f);
            }

            // Make sure map is flattened first
            map.Flatten();

            // Save keys for verification
            var keysBeforeRemove = new List<int>();
            foreach (var item in map)
            {
                keysBeforeRemove.Add(item.Key);
            }

            // Remove a range from the middle
            const int startIdx = 3;
            const int rangeLength = 4;
            map.UnsafeRemoveRangeShiftDown(startIdx, rangeLength);

            // Verify count is updated
            Assert.AreEqual(6, map.Count, "Count should be reduced by range length");

            // Get remaining keys
            var keysAfterRemove = new List<int>();
            foreach (var item in map)
            {
                keysAfterRemove.Add(item.Key);
            }

            // Verify the removed keys are gone and the order is as expected
            Assert.AreEqual(keysBeforeRemove.Count - rangeLength, keysAfterRemove.Count, "Should have fewer keys after remove");

            // First part should be unchanged
            for (int i = 0; i < startIdx; i++)
            {
                Assert.AreEqual(keysBeforeRemove[i], keysAfterRemove[i], $"Key at position {i} should be unchanged");
            }

            // Last part should be shifted down
            for (int i = startIdx; i < keysAfterRemove.Count; i++)
            {
                Assert.AreEqual(keysBeforeRemove[i + rangeLength], keysAfterRemove[i], $"Key at position {i} should be shifted down");
            }
        }

        [Test]
        public void UnsafeRemoveRangeShiftDown_WithEdgeCases()
        {
            var map = this.CreateMap();

            // Add items
            for (int i = 0; i < 10; i++)
            {
                map.Add(i, (short)(i + 100), i * 10.0f);
            }

            map.Flatten();

            // Get initial keys in order
            var initialKeys = new List<int>();
            foreach (var item in map)
            {
                initialKeys.Add(item.Key);
            }

            // Test removing from the beginning
            map.UnsafeRemoveRangeShiftDown(0, 3);
            Assert.AreEqual(7, map.Count, "Count should be updated after removing from beginning");

            // Get keys after first removal
            var keysAfterFirstRemove = new List<int>();
            foreach (var item in map)
            {
                keysAfterFirstRemove.Add(item.Key);
            }

            // Verify we removed the first 3 elements
            Assert.AreEqual(7, keysAfterFirstRemove.Count, "Should have 7 keys remaining");
            for (int i = 0; i < keysAfterFirstRemove.Count; i++)
            {
                Assert.AreEqual(initialKeys[i + 3], keysAfterFirstRemove[i], $"Key at position {i} should be shifted down correctly");
            }

            // Flatten again to ensure a clean state
            map.Flatten();

            // Test removing from the end
            map.UnsafeRemoveRangeShiftDown(4, 3);
            Assert.AreEqual(4, map.Count, "Count should be updated after removing from middle");

            // Get keys after second removal
            var keysAfterSecondRemove = new List<int>();
            foreach (var item in map)
            {
                keysAfterSecondRemove.Add(item.Key);
            }

            // Verify first 4 keys are intact, last 3 are gone
            Assert.AreEqual(4, keysAfterSecondRemove.Count, "Should have 4 keys remaining");
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(keysAfterFirstRemove[i], keysAfterSecondRemove[i], $"Key at position {i} should be unchanged");
            }

            // Test removing zero elements (should be a no-op)
            map.UnsafeRemoveRangeShiftDown(0, 0);
            Assert.AreEqual(4, map.Count, "Count should be unchanged when removing zero elements");
        }

        [Test]
        public void UnsafeRemoveRangeShiftDown_RequiresFlattenFirst()
        {
            var map = this.CreateMap();

            // Add items
            for (int i = 0; i < 10; i++)
            {
                map.Add(i, (short)(i % 3), i * 1.5f);
            }

            // Create holes by removing some items
            map.Remove(3);
            map.Remove(7);

            // Get data before operations for verification
            var dataBeforeOperations = new Dictionary<int, (short, float)>();
            foreach (var item in map)
            {
                dataBeforeOperations[item.Key] = (item.Indexed, item.Value);
            }

            // Flatten to fix the holes
            map.Flatten();

            // This should now work correctly
            map.UnsafeRemoveRangeShiftDown(0, 2);

            // Verify count is reduced by removed range
            Assert.AreEqual(6, map.Count, "Count should be reduced by range length after flattening");

            // Verify remaining data is intact (excluding removed items)
            foreach (var item in map)
            {
                var key = item.Key;
                Assert.IsTrue(dataBeforeOperations.ContainsKey(key), $"Key {key} should be from original data");
                Assert.AreEqual(dataBeforeOperations[key].Item1, item.Indexed, $"Index for key {key} should be preserved");
                Assert.AreEqual(dataBeforeOperations[key].Item2, item.Value, $"Value for key {key} should be preserved");
            }
        }

        private DynamicIndexedMap<int, short, float> CreateMap(int growth = 64)
        {
            var entity = this.Manager.CreateEntity(typeof(TestIndexedMap));
            return this
                .Manager
                .GetBuffer<TestIndexedMap>(entity)
                .InitializeIndexed<TestIndexedMap, int, short, float>(0, growth)
                .AsIndexedMap<TestIndexedMap, int, short, float>();
        }

        private struct TestIndexedMap : IDynamicIndexedMap<int, short, float>
        {
            [UsedImplicitly]
            byte IDynamicIndexedMap<int, short, float>.Value { get; }
        }

        private struct CustomStructMap : IDynamicIndexedMap<CustomKey, CustomIndex, CustomValue>
        {
            [UsedImplicitly]
            byte IDynamicIndexedMap<CustomKey, CustomIndex, CustomValue>.Value { get; }
        }

        private struct EnumWrapperMap : IDynamicIndexedMap<TestEnumKey, CategoryEnumIndex, byte>
        {
            [UsedImplicitly]
            byte IDynamicIndexedMap<TestEnumKey, CategoryEnumIndex, byte>.Value { get; }
        }

        private struct CustomKey : IEquatable<CustomKey>
        {
            public int Id;
            public int Tag;

            public bool Equals(CustomKey other)
            {
                return this.Id == other.Id && this.Tag == other.Tag;
            }

            public override int GetHashCode()
            {
                return this.Id.GetHashCode() ^ this.Tag.GetHashCode();
            }
        }

        private struct CustomIndex : IEquatable<CustomIndex>
        {
            public int GroupId;

            public bool Equals(CustomIndex other)
            {
                return this.GroupId == other.GroupId;
            }

            public override int GetHashCode()
            {
                return this.GroupId.GetHashCode();
            }
        }

        private struct CustomValue
        {
            public float Amount;
            public int Priority;
        }

        private enum TestEnum : byte
        {
            Value1 = 1,
            Value2 = 2,
            Value3 = 3,
        }

        private enum Category : byte
        {
            CategoryA = 10,
            CategoryB = 20,
            CategoryC = 30,
        }

        private struct TestEnumKey : IEquatable<TestEnumKey>
        {
            public TestEnum Value;

            public TestEnumKey(TestEnum value)
            {
                this.Value = value;
            }

            public bool Equals(TestEnumKey other)
            {
                return this.Value == other.Value;
            }

            public override int GetHashCode()
            {
                return this.Value.GetHashCode();
            }
        }

        private struct CategoryEnumIndex : IEquatable<CategoryEnumIndex>
        {
            public Category Value;

            public CategoryEnumIndex(Category value)
            {
                this.Value = value;
            }

            public bool Equals(CategoryEnumIndex other)
            {
                return this.Value == other.Value;
            }

            public override int GetHashCode()
            {
                return this.Value.GetHashCode();
            }
        }
    }
}