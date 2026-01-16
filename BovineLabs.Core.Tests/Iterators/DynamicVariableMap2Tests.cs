// <copyright file="DynamicVariableMap2Tests.cs" company="BovineLabs">
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
    using Unity.Collections.LowLevel.Unsafe;

    public class DynamicVariableMap2Tests : ECSTestsFixture
    {
        [Test]
        public unsafe void KeysAndColumnsPointers_AreAligned_SmallCapacity()
        {
            var map = this.CreateSmallCapacityMap();

            var helper = map.Helper;
            Assert.IsNotNull((IntPtr)helper, "Helper pointer should not be null");

            Assert.AreEqual(0ul, (ulong)helper->Values % (ulong)UnsafeUtility.AlignOf<short>(), "Values pointer should be aligned");
            Assert.AreEqual(0ul, (ulong)helper->KeyHash.Keys % (ulong)UnsafeUtility.AlignOf<long>(), "Keys pointer should be aligned to TKey");
            Assert.AreEqual(0ul, (ulong)helper->KeyHash.Next % (ulong)UnsafeUtility.AlignOf<int>(), "Next pointer should be aligned to int");
            Assert.AreEqual(0ul, (ulong)helper->KeyHash.Buckets % (ulong)UnsafeUtility.AlignOf<int>(), "Buckets pointer should be aligned to int");

            // Column1: MultiHashColumn<short>
            ref var column1 = ref map.Column1;
            ref var layout1 = ref UnsafeUtility.As<MultiHashColumn<short>, MultiHashColumnLayout<short>>(ref column1);
            var column1Ptr = (byte*)UnsafeUtility.AddressOf(ref column1);
            Assert.AreEqual(0ul, (ulong)(column1Ptr + layout1.KeysOffset) % (ulong)UnsafeUtility.AlignOf<short>(), "Column1 keys pointer should be aligned to T1");
            Assert.AreEqual(0ul, (ulong)(column1Ptr + layout1.NextOffset) % (ulong)UnsafeUtility.AlignOf<int>(), "Column1 next pointer should be aligned to int");
            Assert.AreEqual(0ul, (ulong)(column1Ptr + layout1.BucketsOffset) % (ulong)UnsafeUtility.AlignOf<int>(), "Column1 buckets pointer should be aligned to int");

            // Column2: MultiHashColumn<byte> (worst case for int alignment when capacity is small)
            ref var column2 = ref map.Column2;
            ref var layout2 = ref UnsafeUtility.As<MultiHashColumn<byte>, MultiHashColumnLayout<byte>>(ref column2);
            var column2Ptr = (byte*)UnsafeUtility.AddressOf(ref column2);
            Assert.AreEqual(0ul, (ulong)(column2Ptr + layout2.KeysOffset) % (ulong)UnsafeUtility.AlignOf<byte>(), "Column2 keys pointer should be aligned to T2");
            Assert.AreEqual(0ul, (ulong)(column2Ptr + layout2.NextOffset) % (ulong)UnsafeUtility.AlignOf<int>(), "Column2 next pointer should be aligned to int");
            Assert.AreEqual(0ul, (ulong)(column2Ptr + layout2.BucketsOffset) % (ulong)UnsafeUtility.AlignOf<int>(), "Column2 buckets pointer should be aligned to int");
        }

        [Test]
        public void WhenAddingItems_ShouldBeRetrievableByKey()
        {
            var map = this.CreateMap();

            map.Add(1, 0.5f, 5, 10);
            map.Add(2, -1.5f, 7, 20);
            map.Add(3, 35.5f, 9, 30);

            // Test key-based retrieval
            Assert.IsTrue(map.TryGetValue(1, out var data, out var column1, out var column2));
            Assert.AreEqual(0.5f, data);
            Assert.AreEqual(5, column1);
            Assert.AreEqual(10, column2);

            Assert.IsTrue(map.TryGetValue(2, out data, out column1, out column2));
            Assert.AreEqual(-1.5f, data);
            Assert.AreEqual(7, column1);
            Assert.AreEqual(20, column2);

            Assert.IsTrue(map.TryGetValue(3, out data, out column1, out column2));
            Assert.AreEqual(35.5f, data);
            Assert.AreEqual(9, column1);
            Assert.AreEqual(30, column2);
        }

        [Test]
        public void WhenRemovingItems_ShouldUpdateCollectionCorrectly()
        {
            var map = this.CreateMap();
            map.Add(1, 0.5f, 5, 10);
            map.Add(2, -1.5f, 7, 20);
            map.Add(3, 35.5f, 9, 30);

            Assert.AreEqual(3, map.Count, "Initial count should be 3");

            // Remove key 1 and verify it's gone
            map.Remove(1);
            Assert.AreEqual(2, map.Count, "Count should be 2 after removal");
            Assert.IsFalse(map.ContainsKey(1), "Removed key should not be found");

            // Make sure other items are still accessible by key
            Assert.IsTrue(map.TryGetValue(2, out var data, out var column1, out var column2));
            Assert.AreEqual(-1.5f, data, "Value for remaining item should be correct");
            Assert.AreEqual(7, column1, "Column1 for remaining item should be correct");
            Assert.AreEqual(20, column2, "Column2 for remaining item should be correct");

            Assert.IsTrue(map.TryGetValue(3, out data, out column1, out column2));
            Assert.AreEqual(9, column1, "Column1 for remaining item should be correct");
            Assert.AreEqual(30, column2, "Column2 for remaining item should be correct");

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
                map.Add(i, i / 0.37f, (short)(i % 13), (byte)(i % 7));
            }

            Assert.AreEqual(itemCount, map.Count, "Should contain all added items");

            // Sample check to verify data integrity after resizes
            for (var i = 0; i < itemCount; i += 100)
            {
                Assert.IsTrue(map.TryGetValue(i, out var value, out var column1, out var column2), $"Should find key {i}");
                Assert.AreEqual(i / 0.37f, value, 0.0001f, $"Value for key {i} should be correct");
                Assert.AreEqual((short)(i % 13), column1, $"Column1 for key {i} should be correct");
                Assert.AreEqual((byte)(i % 7), column2, $"Column2 for key {i} should be correct");
            }
        }

        [Test]
        public void WhenChangingCapacity_ShouldPreserveAllData()
        {
            var map = this.CreateMap();
            map.Add(1, 0.5f, 5, 10);
            map.Add(2, -1.5f, 7, 20);
            map.Add(3, 35.5f, 9, 30);

            // Test significant capacity increase
            map.Capacity = 1033;

            // Verify key access still works after resize
            Assert.IsTrue(map.TryGetValue(1, out var data, out var column1, out var column2));
            Assert.AreEqual(0.5f, data);
            Assert.AreEqual(5, column1);
            Assert.AreEqual(10, column2);

            Assert.IsTrue(map.TryGetValue(2, out data, out column1, out column2));
            Assert.AreEqual(-1.5f, data);
            Assert.AreEqual(7, column1);
            Assert.AreEqual(20, column2);

            Assert.IsTrue(map.TryGetValue(3, out data, out column1, out column2));
            Assert.AreEqual(35.5f, data);
            Assert.AreEqual(9, column1);
            Assert.AreEqual(30, column2);

            // Test capacity decrease but still above required size
            map.Capacity = 17;

            // Verify key access still works after downsizing
            Assert.IsTrue(map.TryGetValue(1, out data, out column1, out column2));
            Assert.AreEqual(0.5f, data);
            Assert.AreEqual(5, column1);
            Assert.AreEqual(10, column2);

            Assert.IsTrue(map.TryGetValue(2, out data, out column1, out column2));
            Assert.AreEqual(-1.5f, data);
            Assert.AreEqual(7, column1);
            Assert.AreEqual(20, column2);

            Assert.IsTrue(map.TryGetValue(3, out data, out column1, out column2));
            Assert.AreEqual(35.5f, data);
            Assert.AreEqual(9, column1);
            Assert.AreEqual(30, column2);
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
            Assert.IsFalse(map.TryGetValue(123, out _, out _, out _), "TryGetValue should return false for non-existent key");
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
            map.Add(100, 300.5f, 200, 50);
            map.Add(101, 301.5f, 201, 51);
            map.Add(102, 302.5f, 202, 52);
            map.Clear();

            Assert.AreEqual(0, map.Count, "Count should be 0 after clear");
            Assert.IsTrue(map.IsEmpty, "IsEmpty should be true after clear");
            Assert.IsFalse(map.ContainsKey(100), "Items should not exist after clear");

            // Verify we can add new items after clearing
            map.Add(200, 400.5f, 300, 60);
            Assert.AreEqual(1, map.Count, "Should be able to add items after clear");
            Assert.IsTrue(map.ContainsKey(200), "New items should be found after clear");
        }

        [Test]
        public void TryAddVsAdd_ShouldBehaveDifferently_WithDuplicateKeys()
        {
            var map = this.CreateMap();
            map.Add(42, 42.5f, 100, 25);

            // TryAdd returns false and doesn't modify the map for duplicate keys
            var result = map.TryAdd(42, 43.5f, 200, 35);
            Assert.IsFalse(result, "TryAdd should return false for duplicate key");

            Assert.IsTrue(map.TryGetValue(42, out var value, out var column1, out var column2));
            Assert.AreEqual(42.5f, value, "Original value should remain unchanged after TryAdd with duplicate key");
            Assert.AreEqual(100, column1, "Original column1 should remain unchanged after TryAdd with duplicate key");
            Assert.AreEqual(25, column2, "Original column2 should remain unchanged after TryAdd with duplicate key");

            // Add throws exception for duplicate keys
            Assert.Throws<ArgumentException>(() => map.Add(42, 44.5f, 300, 45), "Add should throw for duplicate key");
        }

        [Test]
        public void Enumeration_ShouldYieldAllKeyValueColumnTriplets()
        {
            var map = this.CreateMap();

            // Create a test set for verification
            var expectedItems = new Dictionary<int, (float, short, byte)>
            {
                { 10, (30.5f, 20, 5) },
                { 11, (31.5f, 21, 6) },
                { 12, (32.5f, 22, 7) },
            };

            foreach (var kvp in expectedItems)
            {
                map.Add(kvp.Key, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3);
            }

            // Collect all items through enumeration
            var actualItems = new Dictionary<int, (float, short, byte)>();
            foreach (var item in map)
            {
                actualItems[item.Key] = (item.Value, item.Column1, item.Column2);
            }

            Assert.AreEqual(expectedItems.Count, actualItems.Count, "Enumeration should yield all items");

            foreach (var kvp in expectedItems)
            {
                Assert.IsTrue(actualItems.ContainsKey(kvp.Key), $"Key {kvp.Key} should be present in enumeration");
                Assert.AreEqual(kvp.Value.Item1, actualItems[kvp.Key].Item1, "Value should match after enumeration");
                Assert.AreEqual(kvp.Value.Item2, actualItems[kvp.Key].Item2, "Column1 should match after enumeration");
                Assert.AreEqual(kvp.Value.Item3, actualItems[kvp.Key].Item3, "Column2 should match after enumeration");
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
                map.Add(i, i * 1.5f, (short)(i % 50), (byte)(i % 30));
            }

            Assert.AreEqual(itemCount, map.Count, "Should contain all added items");

            // Check a sampling of items to verify integrity
            for (var i = 0; i < itemCount; i += 123)
            {
                Assert.IsTrue(map.ContainsKey(i), $"Key {i} should exist");
                Assert.IsTrue(map.TryGetValue(i, out var value, out var column1, out var column2));
                Assert.AreEqual(i * 1.5f, value, 0.0001f, $"Value for key {i} should match");
                Assert.AreEqual((short)(i % 50), column1, $"Column1 for key {i} should match");
                Assert.AreEqual((byte)(i % 30), column2, $"Column2 for key {i} should match");
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
            map.Add(111, 333.5f, 222, 111);

            // Test removing and re-adding the same key with different values
            map.Remove(111);
            map.Add(111, 555.5f, 444, 222);

            Assert.IsTrue(map.TryGetValue(111, out var value, out var column1, out var column2));
            Assert.AreEqual(555.5f, value, "Value should be updated for reused key");
            Assert.AreEqual(444, column1, "Column1 should be updated for reused key");
            Assert.AreEqual(222, column2, "Column2 should be updated for reused key");
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
                map.Add(i, i * 10.5f, (short)(100 + i), (byte)(50 + i));

                if (map.Capacity > initialCapacity)
                {
                    // Verify all previously added data is intact after resize
                    for (var j = 0; j <= i; j++)
                    {
                        Assert.IsTrue(map.ContainsKey(j), $"Key {j} should exist after resize");
                        Assert.IsTrue(map.TryGetValue(j, out var val, out var col1, out var col2));
                        Assert.AreEqual((short)(100 + j), col1, $"Column1 for key {j} should be preserved after resize");
                        Assert.AreEqual((byte)(50 + j), col2, $"Column2 for key {j} should be preserved after resize");
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

            map.Add(100, 100.5f, 10, 20);
            map.Add(200, 200.5f, 30, 40);
            map.Add(300, 300.5f, 50, 60);

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

                map.GetAtIndex(storageIndex, out var actualKey, out var actualValue, out var actualColumn1, out var actualColumn2);

                Assert.AreEqual(expectedKey, actualKey, $"Key should match for storage index {storageIndex}");

                // Verify the value and columns match what we expect
                Assert.IsTrue(map.TryGetValue(expectedKey, out var expectedValue, out var expectedColumn1, out var expectedColumn2));
                Assert.AreEqual(expectedValue, actualValue, $"Value should match for key {expectedKey}");
                Assert.AreEqual(expectedColumn1, actualColumn1, $"Column1 should match for key {expectedKey}");
                Assert.AreEqual(expectedColumn2, actualColumn2, $"Column2 should match for key {expectedKey}");
            }
        }

        [Test]
        public void Replace_WithExistingKey_ShouldUpdateColumnValues()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50, 25);
            map.Add(200, 200.5f, 60, 35);

            // Replace column values for existing key
            ref var value = ref map.Replace(100, 75, 40);

            // Verify the column values were updated
            Assert.IsTrue(map.TryGetValue(100, out var retrievedValue, out var retrievedColumn1, out var retrievedColumn2));
            Assert.AreEqual(100.5f, retrievedValue, "Value should remain unchanged initially");
            Assert.AreEqual(75, retrievedColumn1, "Column1 should be updated to new value");
            Assert.AreEqual(40, retrievedColumn2, "Column2 should be updated to new value");

            // Verify we can modify the value through the returned reference
            value = 999.5f;
            Assert.IsTrue(map.TryGetValue(100, out retrievedValue, out _, out _));
            Assert.AreEqual(999.5f, retrievedValue, "Value should be updated through returned reference");
        }

        [Test]
        public void Replace_WithNonExistentKey_ShouldThrow()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50, 25);

            Assert.Throws<ArgumentException>(() => map.Replace(999, 75, 40), "Replace should throw ArgumentException for non-existent key");
        }

        [Test]
        public void Replace_WithSameColumnValues_ShouldStillUpdateReference()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50, 25);

            // Replace with same column values
            ref var value = ref map.Replace(100, 50, 25);

            // Verify we can still modify through reference
            value = 777.5f;
            Assert.IsTrue(map.TryGetValue(100, out var retrievedValue, out var retrievedColumn1, out var retrievedColumn2));
            Assert.AreEqual(777.5f, retrievedValue, "Value should be updated through reference");
            Assert.AreEqual(50, retrievedColumn1, "Column1 should remain the same");
            Assert.AreEqual(25, retrievedColumn2, "Column2 should remain the same");
        }

        [Test]
        public void Replace_AfterResize_ShouldWorkCorrectly()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50, 25);
            map.Add(200, 200.5f, 60, 35);

            // Force resize
            map.Capacity = 1024;

            // Replace after resize
            ref var value = ref map.Replace(100, 75, 40);
            value = 888.5f;

            Assert.IsTrue(map.TryGetValue(100, out var retrievedValue, out var retrievedColumn1, out var retrievedColumn2));
            Assert.AreEqual(888.5f, retrievedValue, "Value should be updated after resize");
            Assert.AreEqual(75, retrievedColumn1, "Column1 should be updated after resize");
            Assert.AreEqual(40, retrievedColumn2, "Column2 should be updated after resize");
        }

        [Test]
        public void Replace_WithManyItems_ShouldHandleCorrectly()
        {
            var map = this.CreateMap();
            const int itemCount = 100;

            // Add many items
            for (var i = 0; i < itemCount; i++)
            {
                map.Add(i, i * 10.5f, (short)(i % 10), (byte)(i % 5));
            }

            // Replace every 10th item's column values
            for (var i = 0; i < itemCount; i += 10)
            {
                ref var value = ref map.Replace(i, 99, 88);
                value *= 2; // Also modify the value
            }

            // Verify the replacements worked
            for (var i = 0; i < itemCount; i += 10)
            {
                Assert.IsTrue(map.TryGetValue(i, out var value, out var column1, out var column2));
                Assert.AreEqual(i * 10.5f * 2, value, 0.0001f, $"Value for key {i} should be doubled");
                Assert.AreEqual(99, column1, $"Column1 for key {i} should be updated to 99");
                Assert.AreEqual(88, column2, $"Column2 for key {i} should be updated to 88");
            }

            // Verify non-replaced items are unchanged
            Assert.IsTrue(map.TryGetValue(5, out var value5, out var column1_5, out var column2_5));
            Assert.AreEqual(5 * 10.5f, value5, 0.0001f, "Non-replaced item should have original value");
            Assert.AreEqual(5, column1_5, "Non-replaced item should have original column1");
            Assert.AreEqual(0, column2_5, "Non-replaced item should have original column2");
        }

        [Test]
        public void Replace_MultipleReplacementsOnSameKey_ShouldWork()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50, 25);

            // Replace multiple times
            ref var value1 = ref map.Replace(100, 60, 30);
            value1 = 200.5f;

            ref var value2 = ref map.Replace(100, 70, 35);
            value2 = 300.5f;

            ref var value3 = ref map.Replace(100, 80, 40);
            value3 = 400.5f;

            Assert.IsTrue(map.TryGetValue(100, out var finalValue, out var finalColumn1, out var finalColumn2));
            Assert.AreEqual(400.5f, finalValue, "Value should reflect last update");
            Assert.AreEqual(80, finalColumn1, "Column1 should reflect last replacement");
            Assert.AreEqual(40, finalColumn2, "Column2 should reflect last replacement");
        }

        [Test]
        public void Replace_OnEmptyMap_ShouldThrow()
        {
            var map = this.CreateMap();

            Assert.Throws<ArgumentException>(() => map.Replace(100, 50, 25), "Replace should throw on empty map");
        }

        [Test]
        public void Replace_AfterClearAndReAdd_ShouldWork()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50, 25);

            // Clear and re-add
            map.Clear();
            map.Add(100, 200.5f, 60, 35);

            // Replace should work on the new entry
            ref var value = ref map.Replace(100, 70, 40);
            value = 300.5f;

            Assert.IsTrue(map.TryGetValue(100, out var retrievedValue, out var retrievedColumn1, out var retrievedColumn2));
            Assert.AreEqual(300.5f, retrievedValue, "Value should be updated after clear/re-add/replace");
            Assert.AreEqual(70, retrievedColumn1, "Column1 should be updated after clear/re-add/replace");
            Assert.AreEqual(40, retrievedColumn2, "Column2 should be updated after clear/re-add/replace");
        }

        [Test]
        public void DifferentColumnCombinations_ShouldWorkIndependently()
        {
            var map = this.CreateMap();

            // Add items with various column combinations
            map.Add(1, 1.0f, 10, 100);
            map.Add(2, 2.0f, 20, 100); // Same column2, different column1
            map.Add(3, 3.0f, 10, 200); // Same column1, different column2
            map.Add(4, 4.0f, 30, 240); // Different both columns

            // Verify all combinations are stored and retrievable correctly
            Assert.IsTrue(map.TryGetValue(1, out var v1, out var c1_1, out var c2_1));
            Assert.AreEqual(1.0f, v1); Assert.AreEqual(10, c1_1); Assert.AreEqual(100, c2_1);

            Assert.IsTrue(map.TryGetValue(2, out var v2, out var c1_2, out var c2_2));
            Assert.AreEqual(2.0f, v2); Assert.AreEqual(20, c1_2); Assert.AreEqual(100, c2_2);

            Assert.IsTrue(map.TryGetValue(3, out var v3, out var c1_3, out var c2_3));
            Assert.AreEqual(3.0f, v3); Assert.AreEqual(10, c1_3); Assert.AreEqual(200, c2_3);

            Assert.IsTrue(map.TryGetValue(4, out var v4, out var c1_4, out var c2_4));
            Assert.AreEqual(4.0f, v4); Assert.AreEqual(30, c1_4); Assert.AreEqual(240, c2_4);
        }

        private DynamicVariableMap<int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>> CreateMap(int growth = 64)
        {
            var entity = this.Manager.CreateEntity(typeof(TestTwoColumnMap));
            return this
                .Manager
                .GetBuffer<TestTwoColumnMap>(entity)
                .InitializeVariableMap<TestTwoColumnMap, int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>(0, growth)
                .AsVariableMap<TestTwoColumnMap, int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>();
        }

        private DynamicVariableMap<long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>> CreateSmallCapacityMap()
        {
            var entity = this.Manager.CreateEntity(typeof(TestTwoColumnLongKeyShortValueMap));
            return this
                .Manager
                .GetBuffer<TestTwoColumnLongKeyShortValueMap>(entity)
                .InitializeVariableMap<TestTwoColumnLongKeyShortValueMap, long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>(0, 1)
                .AsVariableMap<TestTwoColumnLongKeyShortValueMap, long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>();
        }

        private struct TestTwoColumnMap : IDynamicVariableMap<int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>
        {
            [UsedImplicitly]
            byte IDynamicVariableMap<int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>.Value { get; }
        }

        private struct TestTwoColumnLongKeyShortValueMap : IDynamicVariableMap<long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>
        {
            [UsedImplicitly]
            byte IDynamicVariableMap<long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>.Value { get; }
        }

        private struct MultiHashColumnLayout<T>
            where T : unmanaged, IEquatable<T>
        {
            public int KeysOffset;
            public int NextOffset;
            public int BucketsOffset;
            public int Capacity;
        }
    }
}
