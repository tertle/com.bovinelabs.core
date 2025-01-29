// <copyright file="CodecService.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;

    public enum Codec : byte
    {
        LZ4,
    }

    public static unsafe class CodecService
    {
        private const string DllName = "liblz4";

        /// <summary> Return the maximum size that a codec may output in a "worst case" scenario when compressing data. </summary>
        /// <param name="codec"> The codec to use. </param>
        /// <param name="srcSize"> The source size. </param>
        /// <returns> The maximum bound. </returns>
        public static int GetBoundedSize(Codec codec, int srcSize)
        {
            switch (codec)
            {
                case Codec.LZ4:
                    return CompressBoundLZ4(srcSize);
                default:
                    throw new ArgumentException($"Invalid codec '{codec}' specified");
            }
        }

        /// <summary> Compresses the passed in `src` data into the existing `dst` buffer. </summary>
        /// <param name="codec"> The codec to use. </param>
        /// <param name="src"> The source buffer. </param>
        /// <param name="srcSize"> The length of the source buffer. </param>
        /// <param name="dst"> The destination buffer. </param>
        /// <param name="boundedSize"> The destination buffer size. </param>
        /// <returns> The compressed length. </returns>
        public static int Compress(Codec codec, byte* src, int srcSize, byte* dst, int boundedSize)
        {
            switch (codec)
            {
                case Codec.LZ4:
                    return CompressLZ4(src, dst, srcSize, boundedSize);
                default:
                    throw new ArgumentException($"Invalid codec '{codec}' specified");
            }
        }

        public static int Compress(Codec codec, byte* src, int srcSize, out byte* dst, Allocator allocator = Allocator.Temp)
        {
            return Compress(codec, src, srcSize, out dst, (AllocatorManager.AllocatorHandle)allocator);
        }

        /// <summary>
        /// Compresses the passed in `src` data into newly allocated `dst` buffer. Users must free dst manually after calling `Compress`.
        /// </summary>
        /// <param name="codec"> The codec to use. </param>
        /// <param name="src"> The source buffer. </param>
        /// <param name="srcSize"> The length of the source buffer. </param>
        /// <param name="dst"> The destination buffer. </param>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns> The compressed length. </returns>
        public static int Compress(Codec codec, byte* src, int srcSize, out byte* dst, AllocatorManager.AllocatorHandle allocator)
        {
            var boundedSize = GetBoundedSize(codec, srcSize);
            dst = (byte*)Memory.Unmanaged.Allocate(boundedSize, 16, allocator);

            var compressedSize = Compress(codec, src, srcSize, dst, boundedSize);

            if (compressedSize < 0)
            {
                Memory.Unmanaged.Free(dst, allocator);
                dst = null;
            }

            return compressedSize;
        }

        /// <summary>
        /// Decompresses data in `src` buffer and returns true with the decompressed data stored in the passed in, previously allocated `decompressedData` buffer.
        /// Users thus should know ahead of time how large a `decompressedData` buffer to use before calling this function. Not
        /// passing a large enough buffer will result in this function failing and returning false.
        /// </summary>
        /// <param name="codec"> The codec to use. </param>
        /// <param name="compressedData"> The compressed data. </param>
        /// <param name="compressedSize"> The compressed data size. </param>
        /// <param name="decompressedData"> The destination buffer to store the uncompressed data. </param>
        /// <param name="decompressedSize"> The decompressed size. </param>
        /// <returns> True if decompression was successful. </returns>
        public static bool Decompress(Codec codec, in byte* compressedData, int compressedSize, byte* decompressedData, int decompressedSize)
        {
            switch (codec)
            {
                case Codec.LZ4:
                    return DecompressLZ4(compressedData, decompressedData, compressedSize, decompressedSize) > 0;
                default:
                    throw new ArgumentException($"Invalid codec '{codec}' specified");
            }
        }

        /*/// <summary>
        ///
        /// </summary>
        /// <param name="src"></param>
        /// <param name="srcSize"></param>
        /// <param name="compressionLevel"> Define the compression level. 0 - store only to 9 - means best compression. </param>
        public static int Zip(byte* src, int srcSize, int compressionLevel, out byte* dst)
        {
            using var memoryStream = new MemoryStream();

            using (var outputStream = new ZipOutputStream(memoryStream))
            {
                outputStream.SetLevel(compressionLevel);

                var entry = new ZipEntry("save")
                {
                    DateTime = DateTime.Now,
                };
                outputStream.PutNextEntry(entry);

                outputStream.Write(new ReadOnlySpan<byte>(src, srcSize));
            }

            var buffer = memoryStream.GetBuffer();
        }*/

        [DllImport(DllName, EntryPoint = "LZ4_compressBound")]
        private static extern int CompressBoundLZ4(int srcSize);

        [DllImport(DllName, EntryPoint = "LZ4_compress_default")]
        private static extern int CompressLZ4(byte* src, byte* dst, int srcSize, int dstCapacity);

        [DllImport(DllName, EntryPoint = "LZ4_decompress_safe")]
        private static extern int DecompressLZ4(byte* src, byte* dst, int compressedSize, int dstCapacity);
    }
}
