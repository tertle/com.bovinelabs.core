namespace BovineLabs.Core.Tests.Extensions
{
    using System.Collections.Generic;
    using BovineLabs.Core.Extensions;
    using NUnit.Framework;
    using Unity.Collections;

    public class ListExtensionsTests
    {
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
