// <copyright file="DynamicMultiDictionaryTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        [Test]
        public void ReconstructAfterRemapUpdatesTags()
        {
            var entity = this.Manager.CreateEntity(typeof(TestEntry));
            var buffer = this.Manager.GetBuffer<TestEntry>(entity);

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
