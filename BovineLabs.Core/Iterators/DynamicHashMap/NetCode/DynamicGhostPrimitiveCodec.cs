// <copyright file="DynamicGhostPrimitiveCodec.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System.Runtime.CompilerServices;

    public static unsafe class DynamicGhostPrimitiveCodec
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(byte* destination, byte value)
        {
            *destination = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(byte* source)
        {
            return *source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(byte* destination, sbyte value)
        {
            *destination = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadSByte(byte* source)
        {
            return (sbyte)*source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(byte* destination, bool value)
        {
            *destination = value ? (byte)1 : (byte)0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBool(byte* source)
        {
            return *source != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16(byte* destination, ushort value)
        {
            destination[0] = (byte)value;
            destination[1] = (byte)(value >> 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(byte* source)
        {
            return (ushort)(source[0] | (source[1] << 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(byte* destination, short value)
        {
            WriteUInt16(destination, (ushort)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(byte* source)
        {
            return (short)ReadUInt16(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteChar(byte* destination, char value)
        {
            WriteUInt16(destination, (ushort)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ReadChar(byte* source)
        {
            return (char)ReadUInt16(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32(byte* destination, uint value)
        {
            destination[0] = (byte)value;
            destination[1] = (byte)(value >> 8);
            destination[2] = (byte)(value >> 16);
            destination[3] = (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(byte* source)
        {
            return source[0] | ((uint)source[1] << 8) | ((uint)source[2] << 16) | ((uint)source[3] << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(byte* destination, int value)
        {
            WriteUInt32(destination, (uint)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(byte* source)
        {
            return (int)ReadUInt32(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64(byte* destination, ulong value)
        {
            destination[0] = (byte)value;
            destination[1] = (byte)(value >> 8);
            destination[2] = (byte)(value >> 16);
            destination[3] = (byte)(value >> 24);
            destination[4] = (byte)(value >> 32);
            destination[5] = (byte)(value >> 40);
            destination[6] = (byte)(value >> 48);
            destination[7] = (byte)(value >> 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(byte* source)
        {
            return source[0] |
                ((ulong)source[1] << 8) |
                ((ulong)source[2] << 16) |
                ((ulong)source[3] << 24) |
                ((ulong)source[4] << 32) |
                ((ulong)source[5] << 40) |
                ((ulong)source[6] << 48) |
                ((ulong)source[7] << 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(byte* destination, long value)
        {
            WriteUInt64(destination, (ulong)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(byte* source)
        {
            return (long)ReadUInt64(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat32(byte* destination, float value)
        {
            WriteUInt32(destination, *(uint*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloat32(byte* source)
        {
            var bits = ReadUInt32(source);
            return *(float*)&bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat64(byte* destination, double value)
        {
            WriteUInt64(destination, *(ulong*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadFloat64(byte* source)
        {
            var bits = ReadUInt64(source);
            return *(double*)&bits;
        }

        public static ulong Hash64(string text)
        {
            text ??= string.Empty;

            var result = 14695981039346656037UL;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                result = 1099511628211UL * (result ^ (byte)(c & 0xff));
                result = 1099511628211UL * (result ^ (byte)(c >> 8));
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong CombineHash64(ulong hash, ulong value)
        {
            return (hash ^ value) * 1099511628211UL;
        }
    }
}
#endif
