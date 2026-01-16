// <copyright file="DynamicHashSetTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections.LowLevel.Unsafe;

    public class DynamicHashSetTests : ECSTestsFixture
    {
        private const int MinGrowth = 64;

        [Test]
        public void Capacity()
        {
            const int newCapacity = 128;

            var set = this.CreateSet();
            Assert.AreEqual(MinGrowth, set.Capacity);

            set.Capacity = newCapacity;

            Assert.AreEqual(newCapacity, set.Capacity);
        }

        [Test]
        public void AddRemove()
        {
            const int count = 1024;

            var set = this.CreateSet();

            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(set.Add(i));
            }

            Assert.AreEqual(count, set.Count);

            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(set.Remove(i));
            }

            Assert.AreEqual(0, set.Count);
            Assert.IsTrue(set.IsEmpty);
        }

        [Test]
        public void Contains()
        {
            var set = this.CreateSet();

            Assert.IsFalse(set.Contains(47));

            set.Add(47);
            Assert.IsTrue(set.Contains(47));

            set.Remove(47);
            Assert.IsFalse(set.Contains(47));
        }

        [Test]
        public void ToNativeArray()
        {
            const int count = 257;
            var set = this.CreateSet();

            for (var i = 0; i < count; i++)
            {
                set.Add(i);
            }

            var values = set.ToNativeArray(Unity.Collections.Allocator.Temp);
            Assert.AreEqual(count, values.Length);

            var seen = new HashSet<int>();
            for (var i = 0; i < values.Length; i++)
            {
                Assert.IsTrue(seen.Add(values[i]));
            }

            Assert.AreEqual(count, seen.Count);
        }

        [Test]
        public void EnumerationConsistency()
        {
            const int count = 100;
            var set = this.CreateSet();

            for (var i = 0; i < count; i++)
            {
                set.Add(i);
            }

            var found = new HashSet<int>();
            foreach (var value in set)
            {
                Assert.IsTrue(found.Add(value), $"Duplicate value {value} found during enumeration");
            }

            Assert.AreEqual(count, found.Count);
        }

        [Test]
        public unsafe void KeysPointer_IsAlignedToKeyType()
        {
            var set = this.CreateSet();

            var align = UnsafeUtility.AlignOf<int>();
            var keysPtr = (ulong)set.Helper->Keys;
            Assert.AreEqual(0u, keysPtr % (ulong)align);
        }

        [Test]
        public unsafe void Resize_WithHoles_RebuildsAndClearsFreeList()
        {
            const int count = 256;
            var set = this.CreateSet();

            for (var i = 0; i < count; i++)
            {
                set.Add(i);
            }

            // Create holes.
            for (var i = 0; i < count; i += 2)
            {
                Assert.IsTrue(set.Remove(i));
            }

            Assert.AreEqual(count / 2, set.Count);

            // Force a resize while the set has holes.
            set.Capacity *= 2;

            var helper = set.Helper;
            Assert.AreEqual(-1, helper->FirstFreeIdx);
            Assert.AreEqual(helper->Count, helper->AllocatedIndex);

            for (var i = 1; i < count; i += 2)
            {
                Assert.IsTrue(set.Contains(i));
            }

            for (var i = 0; i < count; i += 2)
            {
                Assert.IsFalse(set.Contains(i));
            }
        }

        [Test]
        public unsafe void Flatten_RemovesHolesAndClearsFreeList()
        {
            const int count = 128;
            var set = this.CreateSet();

            for (var i = 0; i < count; i++)
            {
                set.Add(i);
            }

            for (var i = 0; i < count; i += 2)
            {
                Assert.IsTrue(set.Remove(i));
            }

            set.Flatten();

            var helper = set.Helper;
            Assert.AreEqual(-1, helper->FirstFreeIdx);
            Assert.AreEqual(helper->Count, helper->AllocatedIndex);

            for (var i = 1; i < count; i += 2)
            {
                Assert.IsTrue(set.Contains(i));
            }

            for (var i = 0; i < count; i += 2)
            {
                Assert.IsFalse(set.Contains(i));
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
        public void Remove_ReadOnlyBuffer_Throws()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashSet));

            var rw = this.Manager.GetBuffer<TestHashSet>(entity).InitializeHashSet<TestHashSet, int>(0, MinGrowth);
            var setRw = rw.AsHashSet<TestHashSet, int>();
            setRw.Add(1);
            Assert.IsTrue(setRw.Contains(1));

            var ro = this.Manager.GetBuffer<TestHashSet>(entity, true);
            var setRo = ro.AsHashSet<TestHashSet, int>();
            Assert.Throws<InvalidOperationException>(() => setRo.Remove(1));
        }
#endif

        private DynamicHashSet<int> CreateSet()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashSet));
            return this
                .Manager
                .GetBuffer<TestHashSet>(entity)
                .InitializeHashSet<TestHashSet, int>(0, MinGrowth)
                .AsHashSet<TestHashSet, int>();
        }

        private struct TestHashSet : IDynamicHashSet<int>
        {
            byte IDynamicHashSet<int>.Value { get; }
        }
    }
}

