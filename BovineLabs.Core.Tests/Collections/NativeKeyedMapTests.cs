// <copyright file="NativeKeyedMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Collections;

    public class NativeKeyedMapTests
    {
        [Test]
        public void Add()
        {
            var map = new NativeKeyedMap<int>(0, 4, Allocator.Temp);
            map.Add(1, 1);
            map.Add(1, 2);
            map.Add(1, 3);
            map.Add(3, 5);

            ExpectCount(map, 0, 0);
            ExpectCount(map, 1, 3);
            ExpectCount(map, 2, 0);
            ExpectCount(map, 3, 1);

            map.Clear();

            ExpectCount(map, 0, 0);
            ExpectCount(map, 1, 0);
            ExpectCount(map, 2, 0);
            ExpectCount(map, 3, 0);
        }

        private static void ExpectCount(NativeKeyedMap<int> map, int key, int expected)
        {
            var count = 0;

            if (map.TryGetFirstValue(key, out _, out var it))
            {
                do
                {
                    count++;
                }
                while (map.TryGetNextValue(out _, ref it));
            }

            Assert.AreEqual(expected, count);
        }
    }
}
