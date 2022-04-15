// <copyright file="NativeMultiHashMapExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Extensions
{
    using System.Collections.Generic;
    using BovineLabs.Core.Extensions;
    using NUnit.Framework;
    using Unity.Collections;
    using UnityEngine;

    public class NativeMultiHashMapExtensionsTests
    {
        [Test]
        public void ClearAndAddBatchTest()
        {
            int length = 9;

            var hashMap = new NativeMultiHashMap<int, short>(4, Allocator.Temp);

            var keys = new NativeArray<int>(length, Allocator.Temp);
            var values = new NativeArray<short>(length, Allocator.Temp);

            for (short i = 0; i < length; i++)
            {
                keys[i] = i;
                values[i] = (short)(length - (i / 2));
            }

            hashMap.ClearAndAddBatch(keys, values);

            for (short i = 0; i < length; i++)
            {
                Assert.IsTrue(hashMap.TryGetFirstValue(i, out var value, out _));
                Assert.AreEqual(values[i], value);
            }
        }

        [Test]
        public void AddBatchTest()
        {
            int length = 9;
            int split = 3;

            var hashMap = new NativeMultiHashMap<int, short>(4, Allocator.Temp);

            var keys = new NativeArray<int>(length, Allocator.Temp);
            var values = new NativeArray<short>(length, Allocator.Temp);

            for (short i = 0; i < length; i++)
            {
                keys[i] = i;
                values[i] = (short)(length - (i / 2));
            }

            hashMap.AddBatch(keys.GetSubArray(0, split), values.GetSubArray(0, split));
            hashMap.AddBatch(keys.GetSubArray(split, length - split), values.GetSubArray(split, length - split));
            // hashMap.AddBatchUnsafe(keys, values);

            for (short i = 0; i < length; i++)
            {
                Assert.IsTrue(hashMap.TryGetFirstValue(i, out var value, out _));
                Assert.AreEqual(values[i], value);
            }
        }

        [Test]
        public void AddBatchSingleKeyTest()
        {
            int length = 9;
            int split = 3;

            var hashMap = new NativeMultiHashMap<int, short>(4, Allocator.Temp);
            var values = new NativeArray<short>(length, Allocator.Temp);

            var key = 23;

            for (short i = 0; i < length; i++)
            {
                values[i] = (short)(length - i);
            }

            hashMap.AddBatch(key, values.GetSubArray(0, split));
            hashMap.AddBatch(key, values.GetSubArray(split, length - split));

            Assert.IsTrue(hashMap.TryGetFirstValue(key, out var value, out var it));

            var results = new HashSet<int>();
            foreach (var v in values)
            {
                results.Add(v);
            }

            do
            {
                Assert.IsTrue(results.Remove(value));
            }
            while (hashMap.TryGetNextValue(out value, ref it));
            Assert.AreEqual(0, results.Count);
        }
    }
}
