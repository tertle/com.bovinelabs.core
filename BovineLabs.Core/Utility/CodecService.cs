// <copyright file="CodecService.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <remarks> Based off com.unity.entities@0.50.0-preview.24\Unity.Core\Compression\Codec.cs . </remarks>
    public static unsafe class CodecService
    {
        private const string DllName = "liblz4";

        public static int GetBoundedSize(int srcSize)
        {
            return CompressBoundLZ4(srcSize);
        }

        public static int Compress(in byte* src, int srcSize, in byte* dst, int boundedSize, Allocator allocator = Allocator.Temp)
        {
            return CompressLZ4(src, dst, srcSize, boundedSize);
        }

        public static int Compress(in byte* src, int srcSize, out byte* dst, Allocator allocator = Allocator.Temp)
        {
            var boundedSize = CompressBoundLZ4(srcSize);
            dst = (byte*)UnsafeUtility.MallocTracked(boundedSize, 16, allocator, 0);

            var compressedSize = CompressLZ4(src, dst, srcSize, boundedSize);

            if (compressedSize < 0)
            {
                UnsafeUtility.FreeTracked(dst, allocator);
                dst = null;
            }

            return compressedSize;
        }

        public static bool Decompress(in byte* compressedData, int compressedSize, byte* decompressedData, int decompressedSize)
        {
            return DecompressLZ4(compressedData, decompressedData, compressedSize, decompressedSize) > 0;
        }

        [DllImport(DllName, EntryPoint = "LZ4_compressBound")]
        private static extern int CompressBoundLZ4(int srcSize);

        [DllImport(DllName, EntryPoint = "LZ4_compress_default")]
        private static extern int CompressLZ4(byte* src, byte* dst, int srcSize, int dstCapacity);

        [DllImport(DllName, EntryPoint = "LZ4_decompress_safe")]
        private static extern int DecompressLZ4(byte* src, byte* dst, int compressedSize, int dstCapacity);
    }
}