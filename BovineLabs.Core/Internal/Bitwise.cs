// <copyright file="Bitwise.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using System.Runtime.CompilerServices;

    public static class Bitwise
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignDown(int value, int alignPow2)
        {
            return Unity.Collections.Bitwise.AlignDown(value, alignPow2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignUp(int value, int alignPow2)
        {
            return Unity.Collections.Bitwise.AlignUp(value, alignPow2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FromBool(bool value)
        {
            return Unity.Collections.Bitwise.FromBool(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ExtractBits(uint input, int pos, uint mask)
        {
            return Unity.Collections.Bitwise.ExtractBits(input, pos, mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReplaceBits(uint input, int pos, uint mask, uint value)
        {
            return Unity.Collections.Bitwise.ReplaceBits(input, pos, mask, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SetBits(uint input, int pos, uint mask, bool value)
        {
            return Unity.Collections.Bitwise.SetBits(input, pos, mask, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ExtractBits(ulong input, int pos, ulong mask)
        {
            return Unity.Collections.Bitwise.ExtractBits(input, pos, mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReplaceBits(ulong input, int pos, ulong mask, ulong value)
        {
            return Unity.Collections.Bitwise.ReplaceBits(input, pos, mask, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SetBits(ulong input, int pos, ulong mask, bool value)
        {
            return Unity.Collections.Bitwise.SetBits(input, pos, mask, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int lzcnt(byte value)
        {
            return Unity.Collections.Bitwise.lzcnt(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int tzcnt(byte value)
        {
            return Unity.Collections.Bitwise.tzcnt(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int lzcnt(ushort value)
        {
            return Unity.Collections.Bitwise.lzcnt(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int tzcnt(ushort value)
        {
            return Unity.Collections.Bitwise.tzcnt(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int FindWithBeginEnd(ulong* ptr, int beginBit, int endBit, int numBits)
        {
            return Unity.Collections.Bitwise.FindWithBeginEnd(ptr, beginBit, endBit, numBits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int Find(ulong* ptr, int pos, int count, int numBits)
        {
            return Unity.Collections.Bitwise.Find(ptr, pos, count, numBits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TestNone(ulong* ptr, int length, int pos, int numBits = 1)
        {
            return Unity.Collections.Bitwise.TestNone(ptr, length, pos, numBits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TestAny(ulong* ptr, int length, int pos, int numBits = 1)
        {
            return Unity.Collections.Bitwise.TestAny(ptr, length, pos, numBits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TestAll(ulong* ptr, int length, int pos, int numBits = 1)
        {
            return Unity.Collections.Bitwise.TestAll(ptr, length, pos, numBits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int CountBits(ulong* ptr, int length, int pos, int numBits = 1)
        {
            return Unity.Collections.Bitwise.CountBits(ptr, length, pos, numBits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsSet(ulong* ptr, int pos)
        {
            return Unity.Collections.Bitwise.IsSet(ptr, pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong GetBits(ulong* ptr, int length, int pos, int numBits = 1)
        {
            return Unity.Collections.Bitwise.GetBits(ptr, length, pos, numBits);
        }
    }
}
