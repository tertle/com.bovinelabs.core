// <copyright file="CodecServiceTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using BovineLabs.Core.Utility;
    using NUnit.Framework;

    public unsafe class CodecServiceTests
    {
        private const int SourceLength = 128;

        [Test]
        public void Decompress_ReturnsTrueForExactExpectedLength()
        {
            var source = stackalloc byte[SourceLength];
            PopulateSource(source);
            var boundedLength = CodecService.GetBoundedSize(Codec.LZ4, SourceLength);
            var compressed = stackalloc byte[boundedLength];
            var compressedLength = Compress(source, compressed, boundedLength);
            var decompressed = stackalloc byte[SourceLength];

            var success = CodecService.Decompress(Codec.LZ4, compressed, compressedLength, decompressed, SourceLength);

            Assert.IsTrue(success);
            for (var index = 0; index < SourceLength; index++)
            {
                Assert.AreEqual(source[index], decompressed[index]);
            }
        }

        [Test]
        public void Decompress_ReturnsFalseWhenExpectedLengthIsTooLarge()
        {
            var source = stackalloc byte[SourceLength];
            PopulateSource(source);
            var boundedLength = CodecService.GetBoundedSize(Codec.LZ4, SourceLength);
            var compressed = stackalloc byte[boundedLength];
            var compressedLength = Compress(source, compressed, boundedLength);
            var mismatchedLength = SourceLength + 1;
            var decompressed = stackalloc byte[mismatchedLength];

            var success = CodecService.Decompress(Codec.LZ4, compressed, compressedLength, decompressed, mismatchedLength);

            Assert.IsFalse(success);
        }

        private static void PopulateSource(byte* source)
        {
            for (var index = 0; index < SourceLength; index++)
            {
                source[index] = (byte)((index * 31) % 17);
            }
        }

        private static int Compress(byte* source, byte* compressed, int boundedLength)
        {
            var compressedLength = CodecService.Compress(Codec.LZ4, source, SourceLength, compressed, boundedLength);
            Assert.Greater(compressedLength, 0);
            return compressedLength;
        }
    }
}
