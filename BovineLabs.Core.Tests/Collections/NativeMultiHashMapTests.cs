// <copyright file="NativeMultiHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine;
    using Random = Unity.Mathematics.Random;

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

        [TestCase(10000)]
        public unsafe void ClearTest(int count)
        {
            var hashMaps = (UnsafeParallelMultiHashMap<int, Section>*)UnsafeUtility.MallocTracked(sizeof(UnsafeParallelMultiHashMap<int, Section>) * count,
                UnsafeUtility.AlignOf<UnsafeParallelMultiHashMap<int, Section>>(), Allocator.Persistent, 0);

            UnsafeUtility.MemClear(hashMaps, sizeof(UnsafeParallelMultiHashMap<uint, Section>) * count);

            var random = Random.CreateFromIndex(12345);

            for (var i = 0; i < count; i++)
            {
                ref var h = ref UnsafeUtility.ArrayElementAsRef<UnsafeParallelMultiHashMap<int, Section>>(hashMaps, i);
                var c = random.NextInt(64, 512);
                h = new UnsafeParallelMultiHashMap<int, Section>(c, Allocator.Persistent);

                for (var j = 0; j < c; j++)
                {
                    h.Add(j, default);
                }
            }

            for (var i = 0; i < count; i++)
            {
                ref var h = ref UnsafeUtility.ArrayElementAsRef<UnsafeParallelMultiHashMap<int, Section>>(hashMaps, i);

                var capacity = random.NextInt(1024, 16 * 1024);

                h.Clear();
                h.Capacity = capacity;

                for (var j = 0; j < capacity; j++)
                {
                    h.Add(j, default);
                }
            }

            for (var i = 0; i < count; i++)
            {
                hashMaps[i].Dispose();
            }

            UnsafeUtility.FreeTracked(hashMaps, Allocator.Persistent);
        }

        [Test]
        public void ItDoesNotLoseValues()
        {
            var hashmap = new NativeMultiHashMap<ulong, ulong>(4, Allocator.Temp);

            var keys = 66000;
            var values = 3;

            hashmap.Add(0, 0);
            hashmap.Add(0, 1);
            hashmap.Add(0, 2);

            for (var i = 1; i < keys; i++)
            {
                for (var j = 0; j < values; j++)
                {
                    hashmap.Add((ulong)i, (ulong)j);
                }

                var sizeOfKey0 = 0;
                if (hashmap.TryGetFirstValue(0, out _, out var it))
                {
                    do
                    {
                        sizeOfKey0++;
                    }
                    while (hashmap.TryGetNextValue(out _, ref it));

                    Debug.Log($"i: {i}, sizeOfKey0: {sizeOfKey0}");
                    Assert.AreEqual(3, sizeOfKey0);
                }
            }
        }

        private struct Section
        {
            public int Next;
            public int Previous;

            /// <summary> In world space. </summary>
            public float3 Point;

            // Direction will not always be set if it's an incomplete polygon on the final node, but it will always be set if Convex is false
            public float2 Direction;
            public bool Convex;
        }
    }
}
