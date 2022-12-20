// <copyright file="ListExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Extensions
{
    using System.Collections.Generic;
    using BovineLabs.Core.Extensions;
    using NUnit.Framework;
    using Unity.Collections;

    /// <summary> Tests for ListExtensions. </summary>
    public class ListExtensionsTests
    {
        /// <summary> Tests <see cref="Core.Extensions.ListExtensions.AddRangeNative{T}(System.Collections.Generic.List{T},Unity.Collections.NativeArray{T})" />. </summary>
        [Test]
        public void AddRangeNative()
        {
            var list = new List<int> { 123 };
            var nativeArray = new NativeArray<int>(16, Allocator.Temp);

            for (var i = 0; i < nativeArray.Length; i++)
            {
                nativeArray[i] = i;
            }

            list.AddRangeNative(nativeArray);

            Assert.AreEqual(17, list.Count);
            Assert.AreEqual(123, list[0]);

            for (var i = 1; i < 17; i++)
            {
                Assert.AreEqual(i - 1, list[i]);
            }
        }

        /// <summary> Tests <see cref="Core.Extensions.ListExtensions.AddRangeNative{T}(System.Collections.Generic.List{T},Unity.Collections.NativeArray{T},int)" />. </summary>
        [Test]
        public void AddRangeNativeLength()
        {
            var list = new List<int> { 123 };
            var nativeArray = new NativeArray<int>(16, Allocator.Temp);

            for (var i = 0; i < nativeArray.Length; i++)
            {
                nativeArray[i] = i;
            }

            list.AddRangeNative(nativeArray, 5);

            Assert.AreEqual(6, list.Count);
            Assert.AreEqual(123, list[0]);

            for (var i = 1; i < 6; i++)
            {
                Assert.AreEqual(i - 1, list[i]);
            }
        }

        /// <summary> Tests <see cref="Core.Extensions.ListExtensions.AddRangeNative{T}(System.Collections.Generic.List{T},Unity.Collections.NativeSlice{T})" />. </summary>
        [Test]
        public void AddRangeNativeSlice()
        {
            var list = new List<int> { 123 };
            var nativeArray = new NativeArray<int>(16, Allocator.Temp);

            for (var i = 0; i < nativeArray.Length; i++)
            {
                nativeArray[i] = i;
            }

            var slice = nativeArray.Slice();
            list.AddRangeNative(slice);

            Assert.AreEqual(17, list.Count);
            Assert.AreEqual(123, list[0]);

            for (var i = 1; i < 17; i++)
            {
                Assert.AreEqual(i - 1, list[i]);
            }
        }
    }
}
