// <copyright file="NativeMultiHashMapExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Extensions
{
    using System.Collections.Generic;
    using BovineLabs.Core.Extensions;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    public class NativeParallelMultiHashMapExtensionsTests
    {
        [Test]
        public void ClearAndAddBatchTest()
        {
            var length = 9;

            var hashMap = new NativeParallelMultiHashMap<int, short>(4, Allocator.Temp);

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
            var length = 9;
            var split = 3;

            var hashMap = new NativeParallelMultiHashMap<int, short>(4, Allocator.Temp);

            var keys = new NativeArray<int>(length, Allocator.Temp);
            var values = new NativeArray<short>(length, Allocator.Temp);

            for (short i = 0; i < length; i++)
            {
                keys[i] = i;
                values[i] = (short)(length - (i / 2));
            }

            hashMap.AddBatchUnsafe(keys.GetSubArray(0, split), values.GetSubArray(0, split));
            hashMap.AddBatchUnsafe(keys.GetSubArray(split, length - split), values.GetSubArray(split, length - split));
            // hashMap.AddBatchUnsafe(keys, values);

            for (short i = 0; i < length; i++)
            {
                Assert.IsTrue(hashMap.TryGetFirstValue(i, out var value, out _));
                Assert.AreEqual(values[i], value);
            }
        }

        [Test]
        public void AddBatchSliceArrayTest()
        {
            var array = new NativeArray<float2>(16, Allocator.Temp);

            for (var i = 0; i < array.Length; i++)
            {
                array[i] = new float2(i);
            }

            var hashMap = new NativeParallelMultiHashMap<float, float2>(32, Allocator.Temp);

            var v0 = array.Slice().SliceWithStride<float>();
            var v1 = array.Slice().SliceWithStride<float>(UnsafeUtility.SizeOf<float>());
            hashMap.AddBatchUnsafe(v0, array);

            hashMap.AddBatchUnsafe(v1, array);

            var keys = hashMap.GetKeyArray(Allocator.Temp);

            for (var i = 0; i < keys.Length; i++)
            {
                Assert.AreEqual((31 - i) % 16, keys[i]);
            }
        }

        [Test]
        public void AddBatchSingleKeyTest()
        {
            var length = 9;
            var split = 3;

            var hashMap = new NativeParallelMultiHashMap<int, short>(4, Allocator.Temp);
            var values = new NativeArray<short>(length, Allocator.Temp);

            var key = 23;

            for (short i = 0; i < length; i++)
            {
                values[i] = (short)(length - i);
            }

            hashMap.AddBatchUnsafe(key, values.GetSubArray(0, split));
            hashMap.AddBatchUnsafe(key, values.GetSubArray(split, length - split));

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
