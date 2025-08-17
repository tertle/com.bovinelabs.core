// <copyright file="NativeThreadStreamExTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections.ThreadStream
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> Tests for <see cref="NativeStreamExtensions" /> . </summary>
    public class NativeThreadStreamExTests : ECSTestsFixture
    {
        /// <summary> Tests the extensions AllocateLarge and ReadLarge. </summary>
        /// <param name="size"> The size of the allocation. </param>
        [TestCase(512)] // less than max size
        [TestCase(4092)] // max size
        [TestCase(8192)] // requires just more than 2 blocks
        public unsafe void WriteRead(int size)
        {
            var stream = new NativeThreadStream(Allocator.Temp);

            var sourceData = new NativeArray<byte>(size, Allocator.Temp);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            var writer = stream.AsWriter();
            writer.Write(size);
            writer.WriteLarge((byte*)sourceData.GetUnsafeReadOnlyPtr(), size);

            var reader = stream.AsReader();

            reader.BeginForEachIndex(0);

            var readSize = reader.Read<int>();

            Assert.AreEqual(size, readSize);

            var result = new NativeArray<byte>(readSize, Allocator.Temp);

            reader.ReadLarge((byte*)result.GetUnsafePtr(), readSize);

            reader.EndForEachIndex();

            for (var i = 0; i < readSize; i++)
            {
                Assert.AreEqual(sourceData[i], result[i]);
            }
        }
    }
}