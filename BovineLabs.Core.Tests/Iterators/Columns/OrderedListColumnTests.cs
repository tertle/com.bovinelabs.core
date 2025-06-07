// <copyright file="OrderedListColumnTests.cs" company="BovineLabs">
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

    public class OrderedListColumnTests : ECSTestsFixture
    {
        [Test]
        public void WithOrderedColumn_ShouldMaintainSortedOrder()
        {
            var map = this.CreateOrderedMap();

            // Add items with columns in non-sorted order
            map.Add(1, 10.5f, 30);
            map.Add(2, 20.5f, 10);
            map.Add(3, 30.5f, 50);
            map.Add(4, 40.5f, 20);
            map.Add(5, 50.5f, 40);

            // Collect all column values using the iterator - should be in sorted order
            var columnValues = new List<int>();
            if (map.Column.TryGetFirst(out var value, out var it))
            {
                columnValues.Add(value);
                while (map.Column.TryGetNext(out value, ref it))
                {
                    columnValues.Add(value);
                }
            }

            // Verify they come back in ascending sorted order
            var expectedOrder = new int[] { 10, 20, 30, 40, 50 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Column values should be returned in sorted order");

            // Verify we can still find entries by key
            Assert.IsTrue(map.TryGetValue(2, out var item, out var column));
            Assert.AreEqual(20.5f, item);
            Assert.AreEqual(10, column); // This should be the minimum value
        }

        [Test]
        public void WithOrderedColumn_ShouldHandleMultipleEntriesWithSameColumnValue()
        {
            var map = this.CreateOrderedMap();

            // Add multiple entries with the same column value
            map.Add(100, 100.5f, 25);
            map.Add(200, 200.5f, 25);
            map.Add(300, 300.5f, 15);
            map.Add(400, 400.5f, 35);
            map.Add(500, 500.5f, 25);

            // Collect all entries with column value 25 by iterating through all and filtering
            var keysWithColumn25 = new HashSet<int>();
            var valuesWithColumn25 = new HashSet<float>();

            if (map.Column.TryGetFirst(out var columnValue, out var it))
            {
                do
                {
                    if (columnValue == 25)
                    {
                        map.GetAtIndex(it.EntryIndex, out var key, out var v1, out _);
                        keysWithColumn25.Add(key);
                        valuesWithColumn25.Add(v1);
                    }
                }
                while (map.Column.TryGetNext(out columnValue, ref it));
            }

            Assert.AreEqual(3, keysWithColumn25.Count, "Should find 3 entries with column value 25");
            Assert.IsTrue(keysWithColumn25.Contains(100), "Should find key 100");
            Assert.IsTrue(keysWithColumn25.Contains(200), "Should find key 200");
            Assert.IsTrue(keysWithColumn25.Contains(500), "Should find key 500");

            Assert.IsTrue(valuesWithColumn25.Contains(100.5f), "Should find value 100.5");
            Assert.IsTrue(valuesWithColumn25.Contains(200.5f), "Should find value 200.5");
            Assert.IsTrue(valuesWithColumn25.Contains(500.5f), "Should find value 500.5");

            // Verify overall sorted order is maintained
            var allColumnValues = new List<int>();
            if (map.Column.TryGetFirst(out var value, out var globalIt))
            {
                allColumnValues.Add(value);
                while (map.Column.TryGetNext(out value, ref globalIt))
                {
                    allColumnValues.Add(value);
                }
            }

            var expectedOrder = new int[] { 15, 25, 25, 25, 35 };
            CollectionAssert.AreEqual(expectedOrder, allColumnValues, "All column values should be in sorted order");
        }

        [Test]
        public void WithOrderedColumn_RemovalShouldMaintainSortedOrder()
        {
            var map = this.CreateOrderedMap();

            // Add items
            map.Add(1, 10.5f, 30);
            map.Add(2, 20.5f, 10);
            map.Add(3, 30.5f, 50);
            map.Add(4, 40.5f, 20);
            map.Add(5, 50.5f, 40);

            // Remove the item with column value 30 (middle element)
            Assert.IsTrue(map.Remove(1), "Should successfully remove key 1");

            // Verify sorted order is maintained after removal
            var columnValues = new List<int>();
            if (map.Column.TryGetFirst(out var value, out var it))
            {
                columnValues.Add(value);
                while (map.Column.TryGetNext(out value, ref it))
                {
                    columnValues.Add(value);
                }
            }

            var expectedOrder = new int[] { 10, 20, 40, 50 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Column values should remain in sorted order after removal");

            // Remove the minimum element
            Assert.IsTrue(map.Remove(2), "Should successfully remove key 2");

            columnValues.Clear();
            if (map.Column.TryGetFirst(out value, out it))
            {
                columnValues.Add(value);
                while (map.Column.TryGetNext(out value, ref it))
                {
                    columnValues.Add(value);
                }
            }

            expectedOrder = new int[] { 20, 40, 50 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Column values should remain sorted after removing minimum");
        }

        [Test]
        public void WithOrderedColumn_ResizeShouldPreserveSortedOrder()
        {
            var map = this.CreateOrderedMap();

            // Add items in non-sorted order
            var testData = new (int key, float value, int column)[]
            {
                (10, 100.5f, 50), (20, 200.5f, 20), (30, 300.5f, 80), (40, 400.5f, 10), (50, 500.5f, 60)
            };

            foreach (var (key, value, column) in testData)
            {
                map.Add(key, value, column);
            }

            // Force a resize by setting a larger capacity
            map.Capacity = 1024;

            // Verify sorted order is preserved after resize
            var columnValues = new List<int>();
            if (map.Column.TryGetFirst(out var v1, out var it))
            {
                columnValues.Add(v1);
                while (map.Column.TryGetNext(out v1, ref it))
                {
                    columnValues.Add(v1);
                }
            }

            var expectedOrder = new int[] { 10, 20, 50, 60, 80 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Column values should remain sorted after resize");

            // Verify all key-value mappings are intact
            foreach (var (key, expectedValue, expectedColumn) in testData)
            {
                Assert.IsTrue(map.TryGetValue(key, out var actualValue, out var actualColumn));
                Assert.AreEqual(expectedValue, actualValue, $"Value for key {key} should be preserved");
                Assert.AreEqual(expectedColumn, actualColumn, $"Column for key {key} should be preserved");
            }
        }

        [Test]
        public void WithOrderedColumn_EmptyColumnValueShouldReturnFalse()
        {
            var map = this.CreateOrderedMap();

            // Add some items
            map.Add(1, 10.5f, 30);
            map.Add(2, 20.5f, 50);

            // Try to find a column value that doesn't exist (40) by iterating through all
            var found40 = false;
            if (map.Column.TryGetFirst(out var columnValue, out var it))
            {
                do
                {
                    if (columnValue == 40)
                    {
                        found40 = true;
                        break;
                    }
                }
                while (map.Column.TryGetNext(out columnValue, ref it));
            }

            Assert.IsFalse(found40, "Should not find non-existent column value 40");

            // Try to get first when map is empty
            map.Clear();
            Assert.IsFalse(map.Column.TryGetFirst(out _, out _), "Should return false when map is empty");
        }

        [Test]
        public void OrderedColumn_ShouldHandleDuplicateValuesCorrectly()
        {
            var map = this.CreateOrderedMap();

            // Add multiple entries with duplicate column values
            map.Add(1, 10.5f, 25);
            map.Add(2, 20.5f, 25);
            map.Add(3, 30.5f, 25);

            // Count how many times we can iterate through column value 25
            var count = 0;
            if (map.Column.TryGetFirst(out var columnValue, out var it))
            {
                do
                {
                    if (columnValue == 25)
                    {
                        count++;
                    }
                }
                while (map.Column.TryGetNext(out columnValue, ref it));
            }

            Assert.AreEqual(3, count, "Should iterate through all 3 entries with column value 25");

            // Remove one entry and verify count decreases
            Assert.IsTrue(map.Remove(2), "Should remove key 2");

            count = 0;
            if (map.Column.TryGetFirst(out columnValue, out it))
            {
                do
                {
                    if (columnValue == 25)
                    {
                        count++;
                    }
                }
                while (map.Column.TryGetNext(out columnValue, ref it));
            }

            Assert.AreEqual(2, count, "Should iterate through 2 entries after removal");
        }

        [Test]
        public void OrderedColumn_WithNegativeValues_ShouldMaintainCorrectOrder()
        {
            var map = this.CreateOrderedMap();

            map.Add(1, 10.5f, -10);
            map.Add(2, 20.5f, 5);
            map.Add(3, 30.5f, -20);
            map.Add(4, 40.5f, 0);
            map.Add(5, 50.5f, 15);

            var columnValues = new List<int>();
            if (map.Column.TryGetFirst(out var value, out var it))
            {
                columnValues.Add(value);
                while (map.Column.TryGetNext(out value, ref it))
                {
                    columnValues.Add(value);
                }
            }

            var expectedOrder = new int[] { -20, -10, 0, 5, 15 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Should handle negative values correctly");
        }

        [Test]
        public void OrderedColumn_HeadRemoval_ShouldUpdateCorrectly()
        {
            var map = this.CreateOrderedMap();

            map.Add(1, 10.5f, 10);
            map.Add(2, 20.5f, 20);
            map.Add(3, 30.5f, 30);

            // Remove the item with the smallest column value (head)
            Assert.IsTrue(map.Remove(1), "Should remove head element");

            var columnValues = new List<int>();
            if (map.Column.TryGetFirst(out var value, out var it))
            {
                columnValues.Add(value);
                while (map.Column.TryGetNext(out value, ref it))
                {
                    columnValues.Add(value);
                }
            }

            var expectedOrder = new int[] { 20, 30 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Should maintain sorted order after head removal");
        }

        [Test]
        public void OrderedColumn_WithManyElements_ShouldMaintainIntegrity()
        {
            var map = this.CreateOrderedMap();
            const int elementCount = 50;

            // Add many elements in pseudo-random order
            var expectedValues = new List<int>();
            for (int i = 0; i < elementCount; i++)
            {
                var columnValue = (i * 17 + 7) % 100; // Semi-random values
                map.Add(i, i * 10.5f, columnValue);
                expectedValues.Add(columnValue);
            }

            // Sort expected values for comparison
            expectedValues.Sort();

            // Collect all column values in iteration order
            var actualValues = new List<int>();
            if (map.Column.TryGetFirst(out var value, out var it))
            {
                actualValues.Add(value);
                while (map.Column.TryGetNext(out value, ref it))
                {
                    actualValues.Add(value);
                }
            }

            CollectionAssert.AreEqual(expectedValues, actualValues, "Should maintain sorted order with many elements");
            Assert.AreEqual(elementCount, actualValues.Count, "Should contain all added elements");
        }

        [Test]
        public void OrderedColumn_GetFirstAndGetNext_ShouldMatchIteratorPattern()
        {
            var map = this.CreateOrderedMap();

            map.Add(1, 10.5f, 30);
            map.Add(2, 20.5f, 10);
            map.Add(3, 30.5f, 20);

            // Collect using simple GetFirst/GetNext pattern
            var simpleValues = new List<int>();
            var simpleIndices = new List<int>();
            for (int current = map.Column.GetFirst(); current != -1; current = map.Column.GetNext(current))
            {
                simpleValues.Add(map.Column.GetValue(current));
                simpleIndices.Add(current);
            }

            // Collect using iterator pattern
            var iteratorValues = new List<int>();
            var iteratorIndices = new List<int>();
            if (map.Column.TryGetFirst(out var value, out var it))
            {
                iteratorValues.Add(value);
                iteratorIndices.Add(it.EntryIndex);
                while (map.Column.TryGetNext(out value, ref it))
                {
                    iteratorValues.Add(value);
                    iteratorIndices.Add(it.EntryIndex);
                }
            }

            CollectionAssert.AreEqual(simpleValues, iteratorValues, "Both iteration methods should return same values");
            CollectionAssert.AreEqual(simpleIndices, iteratorIndices, "Both iteration methods should return same indices");
        }

        [Test]
        public void OrderedColumn_AfterClear_ShouldAcceptNewElements()
        {
            var map = this.CreateOrderedMap();

            map.Add(1, 10.5f, 30);
            map.Add(2, 20.5f, 10);

            map.Clear();

            map.Add(99, 99.5f, 50);

            Assert.AreEqual(1, map.Count, "Should have one element after clear and add");
            Assert.IsTrue(map.TryGetValue(99, out var value, out var column));
            Assert.AreEqual(99.5f, value);
            Assert.AreEqual(50, column);

            // Verify iteration works
            Assert.IsTrue(map.Column.TryGetFirst(out var columnValue, out _));
            Assert.AreEqual(50, columnValue);
        }

        [Test]
        public void Replace_WithExistingKey_ShouldUpdateColumnAndMaintainOrder()
        {
            var map = this.CreateOrderedMap();
            map.Add(1, 10.5f, 30);
            map.Add(2, 20.5f, 10);
            map.Add(3, 30.5f, 50);

            // Replace with value that changes sort position
            ref var value = ref map.Replace(1, 40);

            // Verify the column value was updated
            Assert.IsTrue(map.TryGetValue(1, out var retrievedValue, out var retrievedColumn));
            Assert.AreEqual(10.5f, retrievedValue, "Value should remain unchanged initially");
            Assert.AreEqual(40, retrievedColumn, "Column should be updated");

            // Verify sort order is maintained
            var columnValues = new List<int>();
            if (map.Column.TryGetFirst(out var columnValue, out var it))
            {
                columnValues.Add(columnValue);
                while (map.Column.TryGetNext(out columnValue, ref it))
                {
                    columnValues.Add(columnValue);
                }
            }

            var expectedOrder = new int[] { 10, 40, 50 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Column values should remain in sorted order");

            // Verify we can modify the value through the returned reference
            value = 999.5f;
            Assert.IsTrue(map.TryGetValue(1, out retrievedValue, out _));
            Assert.AreEqual(999.5f, retrievedValue, "Value should be updated through reference");
        }

        [Test]
        public void Replace_WithSameColumnValue_ShouldOptimizeInPlace()
        {
            var map = this.CreateOrderedMap();
            map.Add(1, 10.5f, 30);
            map.Add(2, 20.5f, 40);

            // Replace with same column value (should optimize)
            map.Replace(1, 30);

            // Verify order is still correct
            var columnValues = new List<int>();
            if (map.Column.TryGetFirst(out var columnValue, out var it))
            {
                columnValues.Add(columnValue);
                while (map.Column.TryGetNext(out columnValue, ref it))
                {
                    columnValues.Add(columnValue);
                }
            }

            var expectedOrder = new int[] { 30, 40 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Order should be maintained");
        }

        [Test]
        public void Replace_MovingToNewSortPosition_ShouldReorderCorrectly()
        {
            var map = this.CreateOrderedMap();
            map.Add(1, 10.5f, 30);
            map.Add(2, 20.5f, 40);
            map.Add(3, 30.5f, 50);

            // Replace middle element to become first
            map.Replace(2, 5);

            var columnValues = new List<int>();
            var keys = new List<int>();
            if (map.Column.TryGetFirst(out var columnValue, out var it))
            {
                columnValues.Add(columnValue);
                map.GetAtIndex(it.EntryIndex, out var key, out _, out _);
                keys.Add(key);

                while (map.Column.TryGetNext(out columnValue, ref it))
                {
                    columnValues.Add(columnValue);
                    map.GetAtIndex(it.EntryIndex, out key, out _, out _);
                    keys.Add(key);
                }
            }

            var expectedOrder = new int[] { 5, 30, 50 };
            var expectedKeys = new int[] { 2, 1, 3 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Column values should be reordered");
            CollectionAssert.AreEqual(expectedKeys, keys, "Keys should follow the new order");
        }

        [Test]
        public void Replace_WithNonExistentKey_ShouldThrow()
        {
            var map = this.CreateOrderedMap();
            map.Add(1, 10.5f, 30);

            Assert.Throws<ArgumentException>(() => map.Replace(999, 40), "Replace should throw for non-existent key");
        }

        [Test]
        public void Replace_WithMultipleSameValues_ShouldMaintainCorrectOrder()
        {
            var map = this.CreateOrderedMap();
            map.Add(1, 10.5f, 30);
            map.Add(2, 20.5f, 30);
            map.Add(3, 30.5f, 50);

            // Replace one of the duplicate values
            map.Replace(1, 40);

            var columnValues = new List<int>();
            if (map.Column.TryGetFirst(out var columnValue, out var it))
            {
                columnValues.Add(columnValue);
                while (map.Column.TryGetNext(out columnValue, ref it))
                {
                    columnValues.Add(columnValue);
                }
            }

            var expectedOrder = new int[] { 30, 40, 50 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Should maintain correct order after replacing duplicate");
        }

        [Test]
        public void Replace_AfterResize_ShouldMaintainSortOrder()
        {
            var map = this.CreateOrderedMap();
            map.Add(1, 10.5f, 30);
            map.Add(2, 20.5f, 20);

            // Force resize
            map.Capacity = 512;

            // Replace after resize
            map.Replace(1, 15);

            var columnValues = new List<int>();
            if (map.Column.TryGetFirst(out var columnValue, out var it))
            {
                columnValues.Add(columnValue);
                while (map.Column.TryGetNext(out columnValue, ref it))
                {
                    columnValues.Add(columnValue);
                }
            }

            var expectedOrder = new[] { 15, 20 };
            CollectionAssert.AreEqual(expectedOrder, columnValues, "Should maintain sorted order after resize and replace");
        }

        private DynamicVariableMap<int, float, int, OrderedListColumn<int>> CreateOrderedMap(int growth = 64)
        {
            var entity = this.Manager.CreateEntity(typeof(TestOrderedMap));
            return this
                .Manager
                .GetBuffer<TestOrderedMap>(entity)
                .InitializeVariableMap<TestOrderedMap, int, float, int, OrderedListColumn<int>>(0, growth)
                .AsVariableMap<TestOrderedMap, int, float, int, OrderedListColumn<int>>();
        }

        private struct TestOrderedMap : IDynamicVariableMap<int, float, int, OrderedListColumn<int>>
        {
            [UsedImplicitly]
            byte IDynamicVariableMap<int, float, int, OrderedListColumn<int>>.Value { get; }
        }
    }
}