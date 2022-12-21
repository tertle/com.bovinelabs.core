// <copyright file="FixedHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Keys;
    using NUnit.Framework;

    public class FixedHashMapTests
    {
        [Test]
        public void AddGet()
        {
            var hashMap = new FixedHashMap<MiniString, int, Size>(default);

            Assert.IsTrue(hashMap.TryAdd("test", 1));
            Assert.IsTrue(hashMap.TryAdd("test2", 2));
            Assert.IsFalse(hashMap.TryAdd("test2", 3));

            Assert.AreEqual(2, hashMap.Count);

            Assert.IsTrue(hashMap.TryGetValue("test", out var v1));
            Assert.AreEqual(1, v1);

            Assert.IsTrue(hashMap.TryGetValue("test2", out var v2));
            Assert.AreEqual(2, v2);

            Assert.IsFalse(hashMap.TryGetValue("test3", out _));
        }

        [StructLayout(LayoutKind.Explicit, Size = 4096)]
        private struct Size
        {
        }
    }
}
