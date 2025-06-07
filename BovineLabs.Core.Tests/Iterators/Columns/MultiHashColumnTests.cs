// <copyright file="MultiHashColumnTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators.Columns
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Iterators.Columns;
    using BovineLabs.Testing;
    using JetBrains.Annotations;
    using NUnit.Framework;

    public class MultiHashColumnTests : ECSTestsFixture
    {
        [Test]
        public void WhenAddingItemsWithSameColumnValue_ShouldFindAllEntries()
        {
            var map = this.CreateMap();

            map.Add(1, 0.5f, 5);
            map.Add(2, -1.5f, 5);
            map.Add(3, 35.5f, 7);

            // Test finding entries by column value
            var foundKeysForColumn5 = new HashSet<int>();
            var foundValuesForColumn5 = new HashSet<float>();

            if (map.Column.TryGetFirst(5, out var it))
            {
                map.GetAtIndex(it.EntryIndex, out var key, out var data, out _);
                foundKeysForColumn5.Add(key);
                foundValuesForColumn5.Add(data);

                while (map.Column.TryGetNext(ref it))
                {
                    map.GetAtIndex(it.EntryIndex, out key, out data, out _);
                    foundKeysForColumn5.Add(key);
                    foundValuesForColumn5.Add(data);
                }
            }

            Assert.AreEqual(2, foundKeysForColumn5.Count, "Should find 2 keys with column value 5");
            Assert.IsTrue(foundKeysForColumn5.Contains(1), "Should find key 1");
            Assert.IsTrue(foundKeysForColumn5.Contains(2), "Should find key 2");
            Assert.IsTrue(foundValuesForColumn5.Contains(0.5f), "Should find value 0.5");
            Assert.IsTrue(foundValuesForColumn5.Contains(-1.5f), "Should find value -1.5");

            // Test finding entries by different column value
            if (map.Column.TryGetFirst(7, out it))
            {
                map.GetAtIndex(it.EntryIndex, out var key, out var data, out _);
                Assert.AreEqual(3, key, "Should find key 3 for column value 7");
                Assert.AreEqual(35.5f, data, "Should find value 35.5 for column value 7");

                // Should be only one entry with column value 7
                Assert.IsFalse(map.Column.TryGetNext(ref it), "Should not find additional entries for column value 7");
            }
            else
            {
                Assert.Fail("Failed to find any keys with column value 7");
            }
        }

        [Test]
        public void WhenRemovingItemsWithSameColumnValue_ShouldUpdateColumnCorrectly()
        {
            var map = this.CreateMap();
            map.Add(1, 0.5f, 5);
            map.Add(2, -1.5f, 5);
            map.Add(3, 35.5f, 7);

            // Remove one item with column value 5
            map.Remove(1);

            // Should still find the remaining item with column value 5
            var foundKeysForColumn5 = new HashSet<int>();
            if (map.Column.TryGetFirst(5, out var it))
            {
                map.GetAtIndex(it.EntryIndex, out var key, out _, out _);
                foundKeysForColumn5.Add(key);

                while (map.Column.TryGetNext(ref it))
                {
                    map.GetAtIndex(it.EntryIndex, out key, out _, out _);
                    foundKeysForColumn5.Add(key);
                }
            }

            Assert.AreEqual(1, foundKeysForColumn5.Count, "Should find 1 key with column value 5 after removal");
            Assert.IsTrue(foundKeysForColumn5.Contains(2), "Should find key 2 for column value 5");

            // Remove the last item with column value 5
            map.Remove(2);

            // Should not find any items with column value 5
            Assert.IsFalse(map.Column.TryGetFirst(5, out _), "Should not find any entries for column value 5 after removing all");
        }

        [Test]
        public void WhenSearchingForNonExistentColumnValue_ShouldReturnFalse()
        {
            var map = this.CreateMap();
            map.Add(1, 0.5f, 5);
            map.Add(2, -1.5f, 7);

            Assert.IsFalse(map.Column.TryGetFirst(99, out _), "Should return false for non-existent column value");
        }

        [Test]
        public void WithManyItemsWithSameColumnValue_ShouldFindAllEntries()
        {
            var map = this.CreateMap();
            const short commonColumn = 42;

            // Add multiple keys with the same column value
            for (var i = 1; i <= 10; i++)
            {
                map.Add(i * 10, i * 100.5f, commonColumn);
            }

            // Retrieve all values for the common column
            var foundKeys = new HashSet<int>();
            var foundValues = new HashSet<float>();

            if (map.Column.TryGetFirst(commonColumn, out var it))
            {
                map.GetAtIndex(it.EntryIndex, out var key, out var value, out _);
                foundKeys.Add(key);
                foundValues.Add(value);

                while (map.Column.TryGetNext(ref it))
                {
                    map.GetAtIndex(it.EntryIndex, out key, out value, out _);
                    foundKeys.Add(key);
                    foundValues.Add(value);
                }
            }

            Assert.AreEqual(10, foundKeys.Count, "Should find all 10 keys with the same column value");
            Assert.AreEqual(10, foundValues.Count, "Should find all 10 values with the same column value");

            for (var i = 1; i <= 10; i++)
            {
                Assert.IsTrue(foundKeys.Contains(i * 10), $"Should find key {i * 10}");
                Assert.IsTrue(foundValues.Contains(i * 100.5f), $"Should find value {i * 100.5f}");
            }
        }

        [Test]
        public void AfterResizing_ShouldStillFindColumnValues()
        {
            var map = this.CreateMap();

            // Add items with specific column values
            map.Add(10, 100.5f, 50);
            map.Add(20, 200.5f, 50);
            map.Add(30, 300.5f, 51);

            // Force resize
            map.Capacity = 1024;

            // Verify we can still find items by column value after resize
            var foundForColumn50 = new Dictionary<int, float>();
            if (map.Column.TryGetFirst(50, out var it))
            {
                map.GetAtIndex(it.EntryIndex, out var key, out var value, out _);
                foundForColumn50[key] = value;

                while (map.Column.TryGetNext(ref it))
                {
                    map.GetAtIndex(it.EntryIndex, out key, out value, out _);
                    foundForColumn50[key] = value;
                }
            }

            Assert.AreEqual(2, foundForColumn50.Count, "Should find 2 entries with column value 50 after resize");
            Assert.IsTrue(foundForColumn50.ContainsKey(10), "Should find key 10 for column value 50");
            Assert.IsTrue(foundForColumn50.ContainsKey(20), "Should find key 20 for column value 50");
            Assert.AreEqual(100.5f, foundForColumn50[10], "Value for key 10 should be correct");
            Assert.AreEqual(200.5f, foundForColumn50[20], "Value for key 20 should be correct");

            var foundForColumn51 = new Dictionary<int, float>();
            if (map.Column.TryGetFirst(51, out it))
            {
                map.GetAtIndex(it.EntryIndex, out var key, out var value, out _);
                foundForColumn51[key] = value;

                while (map.Column.TryGetNext(ref it))
                {
                    map.GetAtIndex(it.EntryIndex, out key, out value, out _);
                    foundForColumn51[key] = value;
                }
            }

            Assert.AreEqual(1, foundForColumn51.Count, "Should find 1 entry with column value 51 after resize");
            Assert.IsTrue(foundForColumn51.ContainsKey(30), "Should find key 30 for column value 51");
            Assert.AreEqual(300.5f, foundForColumn51[30], "Value for key 30 should be correct");
        }

        [Test]
        public void AfterRemovalAndResize_ColumnIndicesShouldBeUpdatedCorrectly()
        {
            var map = this.CreateMap();

            // Setup multiple keys with the same column value
            map.Add(10, 100.5f, 50);
            map.Add(20, 200.5f, 50);
            map.Add(30, 300.5f, 51);

            var removed = map.Remove(10);
            Assert.IsTrue(removed, "Remove should return true for existing key");

            // Verify the other key with the same column value is still accessible
            Assert.IsTrue(map.Column.TryGetFirst(50, out var it));
            map.GetAtIndex(it.EntryIndex, out var key, out var value, out _);
            Assert.AreEqual(20, key, "Should still find the other key with the same column value");
            Assert.AreEqual(200.5f, value, "Should find the correct value for the remaining key");
            Assert.IsFalse(map.Column.TryGetNext(ref it), "Should not find any more items with this column value");

            // Verify other column values are unaffected
            Assert.IsTrue(map.Column.TryGetFirst(51, out it));
            map.GetAtIndex(it.EntryIndex, out key, out value, out _);
            Assert.AreEqual(30, key, "Should find the key with a different column value");
            Assert.AreEqual(300.5f, value, "Should find the correct value for the different column value");

            // Force resize after removal
            map.Capacity = 512;

            // Verify column lookups still work after resize
            Assert.IsTrue(map.Column.TryGetFirst(50, out it));
            map.GetAtIndex(it.EntryIndex, out key, out value, out _);
            Assert.AreEqual(20, key, "Should still find key 20 after resize");
            Assert.AreEqual(200.5f, value, "Should find correct value after resize");
        }

        [Test]
        public void WithDifferentColumnTypes_ShouldHashCorrectly()
        {
            var map = this.CreateMap();

            // Test with various column values that might have hash collisions
            var testData = new (int key, float value, short column)[]
            {
                (1, 10.5f, 0), (2, 20.5f, short.MaxValue), (3, 30.5f, short.MinValue), (4, 40.5f, 1), (5, 50.5f, -1), (6, 60.5f, 100), (7, 70.5f, -100)
            };

            foreach (var (key, value, column) in testData)
            {
                map.Add(key, value, column);
            }

            // Verify each column value can be found correctly
            foreach (var (expectedKey, expectedValue, column) in testData)
            {
                Assert.IsTrue(map.Column.TryGetFirst(column, out var it), $"Should find column value {column}");
                map.GetAtIndex(it.EntryIndex, out var actualKey, out var actualValue, out _);
                Assert.AreEqual(expectedKey, actualKey, $"Should find correct key for column {column}");
                Assert.AreEqual(expectedValue, actualValue, $"Should find correct value for column {column}");

                // Should be only one entry per column in this test
                Assert.IsFalse(map.Column.TryGetNext(ref it), $"Should be only one entry for column {column}");
            }
        }

        [Test]
        public void WhenReusingRemovedKeys_ColumnValuesShouldBeUpdated()
        {
            var map = this.CreateMap();
            map.Add(111, 333.5f, 222);

            // Verify original column association
            Assert.IsTrue(map.Column.TryGetFirst(222, out var it));
            map.GetAtIndex(it.EntryIndex, out var foundKey, out var foundValue, out _);
            Assert.AreEqual(111, foundKey);
            Assert.AreEqual(333.5f, foundValue);

            // Remove and re-add with different column value
            map.Remove(111);
            map.Add(111, 555.5f, 444);

            // Verify the old column association is removed
            Assert.IsFalse(map.Column.TryGetFirst(222, out _), "Old column value should have no entries");

            // Verify the new column association is created
            Assert.IsTrue(map.Column.TryGetFirst(444, out it));
            map.GetAtIndex(it.EntryIndex, out foundKey, out foundValue, out _);
            Assert.AreEqual(111, foundKey, "Should find the correct key with the new column value");
            Assert.AreEqual(555.5f, foundValue, "Should find the correct value with the new column value");
        }

        [Test]
        public void IteratorPattern_ShouldOnlyFindEntriesWithSpecificColumnValue()
        {
            var map = this.CreateMap();

            // Add items with mixed column values
            map.Add(1, 10.5f, 100);
            map.Add(2, 20.5f, 200);
            map.Add(3, 30.5f, 100);
            map.Add(4, 40.5f, 300);
            map.Add(5, 50.5f, 200);

            // Verify iterator only finds items with column value 200
            var foundKeys = new HashSet<int>();
            if (map.Column.TryGetFirst(200, out var it))
            {
                map.GetAtIndex(it.EntryIndex, out var key, out _, out var column);
                Assert.AreEqual(200, column, "Found column should match search criteria");
                foundKeys.Add(key);

                while (map.Column.TryGetNext(ref it))
                {
                    map.GetAtIndex(it.EntryIndex, out key, out _, out column);
                    Assert.AreEqual(200, column, "All found columns should match search criteria");
                    foundKeys.Add(key);
                }
            }

            Assert.AreEqual(2, foundKeys.Count, "Should find exactly 2 keys with column value 200");
            Assert.IsTrue(foundKeys.Contains(2), "Should find key 2");
            Assert.IsTrue(foundKeys.Contains(5), "Should find key 5");
        }

        [Test]
        public void EmptyMap_ShouldReturnFalseForAnyColumnValue()
        {
            var map = this.CreateMap();

            Assert.IsFalse(map.Column.TryGetFirst(1, out _), "Empty map should return false for any column value");
            Assert.IsFalse(map.Column.TryGetFirst(0, out _), "Empty map should return false for column value 0");
            Assert.IsFalse(map.Column.TryGetFirst(-1, out _), "Empty map should return false for negative column value");
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
            Assert.AreEqual(100.5f, retrievedValue, "Value should remain unchanged");
            Assert.AreEqual(75, retrievedColumn, "Column should be updated");

            // Verify we can modify the value through the returned reference
            value = 999.5f;
            Assert.IsTrue(map.TryGetValue(100, out retrievedValue, out _));
            Assert.AreEqual(999.5f, retrievedValue, "Value should be updated through reference");
        }

        [Test]
        public void Replace_WithSameColumnValue_ShouldOptimizeInPlace()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50);

            // Replace with same column value (should optimize)
            ref var value = ref map.Replace(100, 50);

            // Verify column value is unchanged and we can still find it
            Assert.IsTrue(map.Column.TryGetFirst(50, out var it));
            map.GetAtIndex(it.EntryIndex, out var key, out var foundValue, out _);
            Assert.AreEqual(100, key);
            Assert.AreEqual(100.5f, foundValue);
        }

        [Test]
        public void Replace_MovingToDifferentBucket_ShouldUpdateCorrectly()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50);
            map.Add(200, 200.5f, 50);
            map.Add(300, 300.5f, 60);

            // Replace with column value that likely hashes to different bucket
            map.Replace(100, 99);

            // Verify old column association is updated
            var foundForOldColumn = new HashSet<int>();
            if (map.Column.TryGetFirst(50, out var it))
            {
                map.GetAtIndex(it.EntryIndex, out var key, out _, out _);
                foundForOldColumn.Add(key);

                while (map.Column.TryGetNext(ref it))
                {
                    map.GetAtIndex(it.EntryIndex, out key, out _, out _);
                    foundForOldColumn.Add(key);
                }
            }

            Assert.AreEqual(1, foundForOldColumn.Count, "Should find only one key with old column value");
            Assert.IsTrue(foundForOldColumn.Contains(200), "Should find key 200 with old column value");

            // Verify new column association exists
            Assert.IsTrue(map.Column.TryGetFirst(99, out it));
            map.GetAtIndex(it.EntryIndex, out var newKey, out var newValue, out _);
            Assert.AreEqual(100, newKey, "Should find key 100 with new column value");
            Assert.AreEqual(100.5f, newValue, "Value should remain unchanged");
        }

        [Test]
        public void Replace_WithNonExistentKey_ShouldThrow()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50);

            Assert.Throws<ArgumentException>(() => map.Replace(999, 75), "Replace should throw for non-existent key");
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
            map.Replace(100, 75);

            Assert.IsTrue(map.TryGetValue(100, out var value, out var column));
            Assert.AreEqual(100.5f, value);
            Assert.AreEqual(75, column);

            // Verify column lookup works
            Assert.IsTrue(map.Column.TryGetFirst(75, out var it));
            map.GetAtIndex(it.EntryIndex, out var key, out _, out _);
            Assert.AreEqual(100, key);
        }

[Test]
        public void RemoveInternal_WhenRemovingNonFirstItemInBucket_ShouldNotCauseInfiniteLoop()
        {
            var map = this.CreateMap();

            // Add multiple items that might hash to the same bucket
            // Using a small initial capacity to increase collision likelihood
            map.Capacity = 16;

            // Add items with the same column value
            const short columnValue = 42;
            var addedKeys = new List<int>();

            // Keep adding items until we get some that hash to the same bucket
            for (int i = 0; i < 100; i++)
            {
                map.Add(i, i * 0.5f, columnValue);
                addedKeys.Add(i);
            }

            // Remove items in reverse order to ensure we're removing non-first items
            for (int i = addedKeys.Count - 1; i >= 0; i--)
            {
                var keyToRemove = addedKeys[i];

                // This should not cause an infinite loop
                var removed = map.Remove(keyToRemove);
                Assert.IsTrue(removed, $"Should successfully remove key {keyToRemove}");

                // Verify the key is no longer in the map
                Assert.IsFalse(map.ContainsKey(keyToRemove), $"Key {keyToRemove} should not exist after removal");

                // Verify we can still find other items with the same column value
                var remainingCount = 0;
                if (map.Column.TryGetFirst(columnValue, out var it))
                {
                    remainingCount++;
                    while (map.Column.TryGetNext(ref it))
                    {
                        remainingCount++;
                    }
                }

                Assert.AreEqual(i, remainingCount,
                    $"Should have {i} items remaining after removing {addedKeys.Count - i} items");
            }

            // Verify the column is completely empty
            Assert.IsFalse(map.Column.TryGetFirst(columnValue, out _),
                "Should not find any entries after removing all items");
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