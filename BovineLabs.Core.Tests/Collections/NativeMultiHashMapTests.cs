// <copyright file="NativeMultiHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Collections;

    public class NativeMultiHashMapTests
    {
        [Test]
        public void Add()
        {
            var hashMap = new NativeMultiHashMap<int, ushort>(0, Allocator.Temp);

            hashMap.Add(1, 3);
            hashMap.Add(4, 7);
            hashMap.Add(1, 9);

            Assert.AreEqual(3, hashMap.Count);

            Assert.IsTrue(hashMap.TryGetFirstValue(1, out var item, out var it));
            Assert.AreEqual(9, item);
            Assert.IsTrue(hashMap.TryGetNextValue(out item, ref it));
            Assert.AreEqual(3, item);
            Assert.IsFalse(hashMap.TryGetNextValue(out item, ref it));

            Assert.IsTrue(hashMap.TryGetFirstValue(4, out item, out it));
            Assert.AreEqual(7, item);
            Assert.IsFalse(hashMap.TryGetNextValue(out item, ref it));
        }
    }
}
