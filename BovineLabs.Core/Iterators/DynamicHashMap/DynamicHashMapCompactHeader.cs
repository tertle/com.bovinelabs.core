// <copyright file="DynamicHashMapCompactHeader.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;

    internal unsafe struct DynamicHashMapCompactHeader
    {
        internal const int Size = 16;
        internal const byte CurrentFormatVersion = 1;
        internal const ushort CurrentFlags = 0;

        internal uint Count;
        internal uint Capacity;
        internal uint PayloadBytes;
        internal byte Log2MinGrowth;
        internal byte FormatVersion;
        internal ushort Flags;

        internal readonly bool IsCurrentFormat => this.FormatVersion == CurrentFormatVersion && this.Flags == CurrentFlags;

        internal static DynamicHashMapCompactHeader Create(int count, int capacity, int payloadBytes, int log2MinGrowth)
        {
            Check.Assume(count >= 0, "Count must be non-negative.");
            Check.Assume(capacity >= count, "Capacity must be greater than or equal to count.");
            Check.Assume(payloadBytes >= Size, "Payload must contain the compact header.");
            Check.Assume(log2MinGrowth >= 0 && log2MinGrowth <= byte.MaxValue, "Log2MinGrowth is out of range.");

            return new DynamicHashMapCompactHeader
            {
                Count = (uint)count,
                Capacity = (uint)capacity,
                PayloadBytes = (uint)payloadBytes,
                Log2MinGrowth = (byte)log2MinGrowth,
                FormatVersion = CurrentFormatVersion,
                Flags = CurrentFlags,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void Write(byte* destination)
        {
            Check.Assume(destination != null, "Compact header destination must not be null.");

            WriteUInt32(destination, 0, this.Count);
            WriteUInt32(destination, 4, this.Capacity);
            WriteUInt32(destination, 8, this.PayloadBytes);
            destination[12] = this.Log2MinGrowth;
            destination[13] = this.FormatVersion;
            WriteUInt16(destination, 14, this.Flags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DynamicHashMapCompactHeader Read(byte* source)
        {
            Check.Assume(source != null, "Compact header source must not be null.");

            return new DynamicHashMapCompactHeader
            {
                Count = ReadUInt32(source, 0),
                Capacity = ReadUInt32(source, 4),
                PayloadBytes = ReadUInt32(source, 8),
                Log2MinGrowth = source[12],
                FormatVersion = source[13],
                Flags = ReadUInt16(source, 14),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteUInt32(byte* destination, int offset, uint value)
        {
            destination[offset] = (byte)value;
            destination[offset + 1] = (byte)(value >> 8);
            destination[offset + 2] = (byte)(value >> 16);
            destination[offset + 3] = (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReadUInt32(byte* source, int offset)
        {
            return source[offset] | ((uint)source[offset + 1] << 8) | ((uint)source[offset + 2] << 16) | ((uint)source[offset + 3] << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteUInt16(byte* destination, int offset, ushort value)
        {
            destination[offset] = (byte)value;
            destination[offset + 1] = (byte)(value >> 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ReadUInt16(byte* source, int offset)
        {
            return (ushort)(source[offset] | (source[offset + 1] << 8));
        }
    }
}
