namespace BovineLabs.Core.Tests.Collections
{
    using System.Collections.Generic;
    using BovineLabs.Core.Collections;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using EntityValueEntry = BovineLabs.Core.Tests.Collections.DynamicMultiDictionaryEntityValueEntry;
    using TestEntry = BovineLabs.Core.Tests.Collections.DynamicMultiDictionaryTestEntry;

    public class DynamicMultiDictionaryTests : ECSTestsFixture
    {
        [TestCase(0)]
        [TestCase(0x7f7f7f7f)]
        public void InvalidSpareHeaderRecountsEntriesAndRecoversOnWrite(int spareValue)
        {
            var entity = Manager.CreateEntity(typeof(TestEntry));
            var buffer = Manager.GetBuffer<TestEntry>(entity);
            var map = buffer.AsDynamicMultiDictionary<int, int, TestEntry>();
            map.EnsureCapacity(8);
            map.Add(1, 10);
            map.Add(1, 11);
            map.Add(9, 90);
            Assert.AreEqual(1, map.Remove(9));

            // Unity can copy Length entries while leaving the capacity-only header cleared or uninitialized.
            var capacity = buffer.Length;
            buffer.ResizeUninitialized(capacity + 1);
            buffer[capacity] = new TestEntry
            {
                TagField = (uint)spareValue,
                KeyField = spareValue,
            };
            buffer.Length = capacity;

            Assert.AreEqual(2, map.Count);
            Assert.AreEqual(2, map.Remove(1));
            map.Add(25, 250);
            map.Add(25, 251);
            Assert.AreEqual(2, map.Count);
            AssertValues(map, 25, 250, 251);
            Assert.AreEqual(0, map.CountValuesForKey(1));
            Assert.AreEqual(0, map.CountValuesForKey(9));
            Assert.AreEqual(capacity, map.Capacity);
        }

        [Test]
        public void ReconstructAfterRemapUpdatesTags()
        {
            var entity = Manager.CreateEntity(typeof(TestEntry));
            var buffer = Manager.GetBuffer<TestEntry>(entity);

            var map = buffer.AsDynamicMultiDictionary<int, int, TestEntry>();

            map.Add(1, 10);
            map.Add(1, 11);
            map.Add(9, 90);

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

            Assert.IsFalse(map.TryGetFirstValue(101, out _, out _));

            map.ReconstructAfterRemap();

            AssertValues(map, 101, 10, 11);
            AssertValues(map, 109, 90);
        }

        private static void AssertValues(DynamicMultiDictionary<int, int, TestEntry> map, int key, params int[] expected)
        {
            var actual = new List<int>();
            if (map.TryGetFirstValue(key, out var value, out var it))
            {
                do
                {
                    actual.Add(value);
                }
                while (map.TryGetNextValue(out value, ref it));
            }

            CollectionAssert.AreEquivalent(expected, actual);
        }
    }
}
