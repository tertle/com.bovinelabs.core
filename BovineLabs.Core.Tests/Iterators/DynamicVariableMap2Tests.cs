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
        public void Replace_WithNonExistentKey_ShouldThrow()
        {
            var map = this.CreateMap();
            map.Add(100, 100.5f, 50, 25);

            Assert.Throws<ArgumentException>(() => map.Replace(999, 75, 40), "Replace should throw ArgumentException for non-existent key");
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
            var entity = this.Manager.CreateEntity(typeof(DynamicVariableMap2TestsBuffer));
            return this
                .Manager
                .GetBuffer<DynamicVariableMap2TestsBuffer>(entity)
                .InitializeVariableMap<DynamicVariableMap2TestsBuffer, int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>(0, growth)
                .AsVariableMap<DynamicVariableMap2TestsBuffer, int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>();
        }

        private DynamicVariableMap<long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>> CreateSmallCapacityMap()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicVariableMap2TestsLongKeyShortValueBuffer));
            return this
                .Manager
                .GetBuffer<DynamicVariableMap2TestsLongKeyShortValueBuffer>(entity)
                .InitializeVariableMap<DynamicVariableMap2TestsLongKeyShortValueBuffer, long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>(0, 1)
                .AsVariableMap<DynamicVariableMap2TestsLongKeyShortValueBuffer, long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>();
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
