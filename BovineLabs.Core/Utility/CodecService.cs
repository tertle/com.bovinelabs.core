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
        /// Caller must free the newly allocated destination buffer.
        /// </summary>
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
        /// Caller supplies the destination buffer. Returns true only when output is exactly decompressedSize bytes; insufficient capacity fails.
        /// </summary>
        public static bool Decompress(Codec codec, in byte* compressedData, int compressedSize, byte* decompressedData, int decompressedSize)
        {
            switch (codec)
            {
                case Codec.LZ4:
                    return decompressedSize > 0 && DecompressLZ4(compressedData, decompressedData, compressedSize, decompressedSize) == decompressedSize;
                default:
                    throw new ArgumentException($"Invalid codec '{codec}' specified");
            }
        }

        [DllImport(DllName, EntryPoint = "LZ4_compressBound")]
        private static extern int CompressBoundLZ4(int srcSize);

        [DllImport(DllName, EntryPoint = "LZ4_compress_default")]
        private static extern int CompressLZ4(byte* src, byte* dst, int srcSize, int dstCapacity);

        [DllImport(DllName, EntryPoint = "LZ4_decompress_safe")]
        private static extern int DecompressLZ4(byte* src, byte* dst, int compressedSize, int dstCapacity);
    }
}
