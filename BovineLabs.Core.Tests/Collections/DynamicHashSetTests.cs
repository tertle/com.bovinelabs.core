// <copyright file="DynamicHashSetTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    public class DynamicHashSetTests : ECSTestsFixture
    {
        [Test]
        public void EmptyBufferSupportsZeroCapacityReads()
        {
            var set = this.CreateSet(out _, out _);

            set.EnsureCapacity(0);

            Assert.AreEqual(0, set.Count);
            Assert.AreEqual(0, set.Capacity);
            Assert.IsTrue(set.IsEmpty);
            Assert.IsFalse(set.Contains(1));
            Assert.IsFalse(set.TryRemove(1));
        }

        [Test]
        public void DuplicateInsertionUsesSetSemantics()
        {
            var set = this.CreateSet(out _, out _);

            Assert.IsTrue(set.TryAdd(7));
            Assert.IsFalse(set.TryAdd(7));
            Assert.AreEqual(1, set.Count);
            Assert.Throws<ArgumentException>(() => set.Add(7));
        }

        [Test]
        public void CollisionsRemainQueryableAcrossGrowth()
        {
            var set = this.CreateSet(out _, out var buffer);
            set.EnsureCapacity(8);

            for (var i = 0; i < 32; i++)
            {
                Assert.IsTrue(set.TryAdd(1 + (i * 8)));
            }

            Assert.Greater(buffer.Length, 8);
            Assert.AreEqual(32, set.Count);
            for (var i = 0; i < 32; i++)
            {
                Assert.IsTrue(set.Contains(1 + (i * 8)));
            }
        }

        [Test]
        public void RemovalMaintainsClusterAndReusesTombstone()
        {
            var set = this.CreateSet(out _, out var buffer);
            set.EnsureCapacity(8);

            Assert.IsTrue(set.TryAdd(1));
            Assert.IsTrue(set.TryAdd(9));
            Assert.IsTrue(set.TryAdd(17));
            Assert.IsTrue(set.TryRemove(9));
            Assert.IsFalse(set.Contains(9));
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(17));

            Assert.IsTrue(set.TryAdd(25));
            Assert.AreEqual(3, set.Count);
            Assert.IsTrue(set.Contains(25));

            var tombstones = 0;
            for (var i = 0; i < buffer.Length; i++)
            {
                var tag = buffer[i].TagField;
                if (tag != 0 && (tag & 1u) == 0)
                {
                    tombstones++;
                }
            }

            Assert.AreEqual(0, tombstones);
        }

        [Test]
        public void ClearPreservesCapacityAndRemovesKeys()
        {
            var set = this.CreateSet(out _, out _);
            for (var i = 0; i < 12; i++)
            {
                set.Add(i);
            }

            var capacity = set.Capacity;
            set.Clear();

            Assert.AreEqual(0, set.Count);
            Assert.IsTrue(set.IsEmpty);
            Assert.AreEqual(capacity, set.Capacity);
            for (var i = 0; i < 12; i++)
            {
                Assert.IsFalse(set.Contains(i));
            }

            Assert.IsTrue(set.TryAdd(100));
            Assert.IsTrue(set.Contains(100));
        }

        [Test]
        public void ReadOnlyBufferSupportsContainsCountAndKeyCopy()
        {
            var set = this.CreateSet(out var entity, out _);
            set.Add(3);
            set.Add(11);
            set.Add(19);

            var readOnlyBuffer = this.Manager.GetBuffer<TestEntry>(entity, true);
            var readOnlySet = readOnlyBuffer.AsDynamicHashSet<int, TestEntry>();
            var keys = readOnlySet.GetKeyArray(Allocator.Temp);

            Assert.AreEqual(3, readOnlySet.Count);
            Assert.IsTrue(readOnlySet.Contains(3));
            Assert.IsTrue(readOnlySet.Contains(11));
            Assert.IsTrue(readOnlySet.Contains(19));
            CollectionAssert.AreEquivalent(new[] { 3, 11, 19 }, keys);
        }

        [Test]
        public void ReconstructAfterRemapUpdatesTagsAndPositions()
        {
            var set = this.CreateSet(out _, out var buffer);
            set.Add(1);
            set.Add(9);

            for (var i = 0; i < buffer.Length; i++)
            {
                var entry = buffer[i];
                if ((entry.TagField & 1u) == 0)
                {
                    continue;
                }

                entry.KeyField += 100;
                buffer[i] = entry;
            }

            Assert.IsFalse(set.Contains(101));

            set.ReconstructAfterRemap();

            Assert.IsTrue(set.Contains(101));
            Assert.IsTrue(set.Contains(109));
            Assert.AreEqual(2, set.Count);
        }

        private DynamicHashSet<int, TestEntry> CreateSet(out Entity entity, out DynamicBuffer<TestEntry> buffer)
        {
            entity = this.Manager.CreateEntity(typeof(TestEntry));
            buffer = this.Manager.GetBuffer<TestEntry>(entity);
            return buffer.AsDynamicHashSet<int, TestEntry>();
        }

        private struct TestEntry : IDynamicHashSetEntry<int>
        {
            public uint TagField;
            public int KeyField;

            public uint Tag
            {
                get => this.TagField;
                set => this.TagField = value;
            }

            public int Key
            {
                get => this.KeyField;
                set => this.KeyField = value;
            }
        }
    }
}
