// <copyright file="NativeStreamExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Extensions
{
    using BovineLabs.Core.Extensions;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public class NativeStreamExtensionsTests
    {
        /// <summary> Tests the extensions AllocateLarge and ReadLarge. </summary>
        /// <param name="size"> The size of the allocation. </param>
        [TestCase(512)] // less than max size
        [TestCase(4092)] // max size
        [TestCase(8192)] // requires just more than 2 blocks
        public unsafe void WriteLarge(int size)
        {
            var stream = new NativeStream(1, Allocator.Temp);

            var sourceData = new NativeArray<byte>(size, Allocator.Temp);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            var writer = stream.AsWriter();

            writer.BeginForEachIndex(0);
            writer.Write(size);
            writer.WriteLarge((byte*)sourceData.GetUnsafeReadOnlyPtr(), size);
            writer.EndForEachIndex();

            var reader = stream.AsReader();

            reader.BeginForEachIndex(0);

            var readSize = reader.Read<int>();
            Assert.AreEqual(size, readSize);

            var buffer = new NativeArray<byte>(readSize, Allocator.Temp);

            reader.ReadLarge((byte*)buffer.GetUnsafePtr(), readSize);
            reader.EndForEachIndex();

            for (var i = 0; i < readSize; i++)
            {
                Assert.AreEqual(sourceData[i], buffer[i], 0, $"Failed on {i}");
            }
        }

        /// <summary> Tests the extensions AllocateLarge and ReadLarge. </summary>
        /// <param name="size"> The size of the allocation. </param>
        [TestCase(128)] // less than max size
        [TestCase(1023)] // max size
        [TestCase(2048)] // requires just more than 2 blocks
        public unsafe void WriteLargeSlice(int size)
        {
            var stream = new NativeStream(1, Allocator.Temp);

            var sourceData = new NativeArray<Data>(size, Allocator.Temp);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = new Data
                {
                    Key = i,
                    Value = 123,
                };
            }

            var writer = stream.AsWriter();

            writer.BeginForEachIndex(0);
            writer.Write(size);
            writer.WriteLarge(sourceData.Slice().SliceWithStride<int>());
            writer.EndForEachIndex();

            var reader = stream.AsReader();

            reader.BeginForEachIndex(0);

            var readSize = reader.Read<int>();
            var bytes = readSize * UnsafeUtility.SizeOf<int>();
            Assert.AreEqual(size, readSize);

            var buffer = new NativeArray<int>(readSize, Allocator.Temp);

            reader.ReadLarge((byte*)buffer.GetUnsafePtr(), bytes);
            reader.EndForEachIndex();

            for (var i = 0; i < readSize; i++)
            {
                Assert.AreEqual(sourceData[i].Key, buffer[i]);
            }
        }

        /// <summary> Tests the extensions AllocateLarge and ReadLarge. </summary>
        /// <param name="size"> The size of the allocation. </param>
        [TestCase(128)] // less than max size
        [TestCase(1023)] // max size
        [TestCase(2048)] // requires just more than 2 blocks
        public unsafe void WriteLargeArray(int size)
        {
            var stream = new NativeStream(1, Allocator.Temp);

            var sourceData = new NativeArray<int>(size, Allocator.Temp);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = i;
            }

            var writer = stream.AsWriter();

            writer.BeginForEachIndex(0);
            writer.Write(size);
            writer.WriteLarge(sourceData);
            writer.EndForEachIndex();

            var reader = stream.AsReader();

            reader.BeginForEachIndex(0);

            var readSize = reader.Read<int>();
            var bytes = readSize * UnsafeUtility.SizeOf<int>();
            Assert.AreEqual(size, readSize);

            var buffer = new NativeArray<int>(readSize, Allocator.Temp);

            reader.ReadLarge((byte*)buffer.GetUnsafePtr(), bytes);
            reader.EndForEachIndex();

            for (var i = 0; i < readSize; i++)
            {
                Assert.AreEqual(sourceData[i], buffer[i]);
            }
        }

        public struct Data
        {
            public int Key;
            public int Value;
        }
    }
}
