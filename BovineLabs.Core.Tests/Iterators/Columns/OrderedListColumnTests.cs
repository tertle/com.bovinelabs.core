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
    using Unity.Collections.LowLevel.Unsafe;

    public class OrderedListColumnTests : ECSTestsFixture
    {
        [Test]
        public unsafe void OrderedListColumnPointers_AreAligned_SmallCapacity()
        {
            var map = this.CreateSmallCapacityOrderedMap();

            ref var column = ref map.Column;
            ref var layout = ref UnsafeUtility.As<OrderedListColumn<short>, OrderedListColumnLayout<short>>(ref column);

            var columnPtr = (byte*)UnsafeUtility.AddressOf(ref column);
            var keysPtr = columnPtr + layout.KeysOffset;
            var nextPtr = columnPtr + layout.NextOffset;
            var prevPtr = columnPtr + layout.PrevOffset;

            Assert.AreEqual(0ul, (ulong)keysPtr % (ulong)UnsafeUtility.AlignOf<short>(), "Keys pointer should be aligned to T");
            Assert.AreEqual(0ul, (ulong)nextPtr % (ulong)UnsafeUtility.AlignOf<int>(), "Next pointer should be aligned to int");
            Assert.AreEqual(0ul, (ulong)prevPtr % (ulong)UnsafeUtility.AlignOf<int>(), "Prev pointer should be aligned to int");
        }

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

        private DynamicVariableMap<int, float, int, OrderedListColumn<int>> CreateOrderedMap(int growth = 64)
        {
            var entity = this.Manager.CreateEntity(typeof(OrderedListColumnTestsBuffer));
            return this
                .Manager
                .GetBuffer<OrderedListColumnTestsBuffer>(entity)
                .InitializeVariableMap<OrderedListColumnTestsBuffer, int, float, int, OrderedListColumn<int>>(0, growth)
                .AsVariableMap<OrderedListColumnTestsBuffer, int, float, int, OrderedListColumn<int>>();
        }

        private DynamicVariableMap<long, short, short, OrderedListColumn<short>> CreateSmallCapacityOrderedMap()
        {
            var entity = this.Manager.CreateEntity(typeof(OrderedListColumnTestsSmallBuffer));
            return this
                .Manager
                .GetBuffer<OrderedListColumnTestsSmallBuffer>(entity)
                .InitializeVariableMap<OrderedListColumnTestsSmallBuffer, long, short, short, OrderedListColumn<short>>(0, 1)
                .AsVariableMap<OrderedListColumnTestsSmallBuffer, long, short, short, OrderedListColumn<short>>();
        }

        private struct OrderedListColumnLayout<T>
            where T : unmanaged, IEquatable<T>, IComparable<T>
        {
            public int KeysOffset;
            public int NextOffset;
            public int PrevOffset;
            public int Head;
            public int Capacity;
        }
    }
}
