// <copyright file="BlobHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections.Blobs
{
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    public class BlobHashMapTests
    {
        [Test]
        public void NestedBlobs()
        {
            const int count = 100;

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<Test>();

            var hashMapBuilder = builder.AllocateHashMap(ref root.HashMap, count);

            for (var i = 0; i < count; i++)
            {
                ref var value = ref hashMapBuilder.AddUnique(i);
                var array = builder.Allocate(ref value, i);

                for (var j = 0; j < i; j++)
                {
                    array[j] = j;
                }
            }

            using var t = builder.CreateBlobAssetReference<Test>(Allocator.Persistent);

            for (var i = 0; i < count; i++)
            {
                ref var b = ref t.Value.HashMap[i];
                Assert.AreEqual(i, b.Length);

                for (var j = 0; j < b.Length; j++)
                {
                    Assert.AreEqual(j, b[j]);
                }

                Assert.IsTrue(t.Value.HashMap.TryGetValue(i, out var ptr));
                ref var b2 = ref ptr.Ref;
                Assert.AreEqual(i, b2.Length);

                for (var j = 0; j < b2.Length; j++)
                {
                    Assert.AreEqual(j, b2[j]);
                }
            }

            using var e = t.Value.HashMap.GetEnumerator();
            var iterations = 0;

            while (e.MoveNext())
            {
                iterations++;

                var key = e.Current.Key;
                ref var value = ref e.Current.Value;

                Assert.AreEqual(key, value.Length);

                for (var j = 0; j < value.Length; j++)
                {
                    Assert.AreEqual(j, value[j]);
                }
            }

            Assert.AreEqual(count, iterations);
        }

        private struct Test
        {
            public BlobHashMap<int, BlobArray<int>> HashMap;
        }
    }
}
