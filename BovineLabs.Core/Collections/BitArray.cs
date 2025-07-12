// <copyright file="BitArray.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable SA1649 // Filename must match

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using Unity.Burst.Intrinsics;
    using Unity.Mathematics;
    using Unity.Properties;
    using UnityEngine;

    /// <summary>
    /// IBitArray interface.
    /// Originally based off com.unity.render-pipelines.core@12.0.0\Runtime\Utilities\BitArray but made generic and burst friendly.
    /// </summary>
    /// <typeparam name="T"> The type. </typeparam>
    public interface IBitArray<T> : IEquatable<T>
        where T : unmanaged, IBitArray<T>
    {
        /// <summary> Gets the capacity of this BitArray. This is the number of bits that are usable. </summary>
        uint Capacity { get; }

        /// <summary> Gets a value indicating whether all bits are 0. </summary>
        bool AllFalse { get; }

        /// <summary> Gets a value indicating whether all bits are 1. </summary>
        bool AllTrue { get; }

        /// <summary> Gets the bit array in a human-readable form.This is as a string of 0s and 1s packed by 8 bits. This is useful for debugging. </summary>
        string HumanizedData { get; }

        /// <summary> An indexer that allows access to the bit at a given index. This provides both read and write access. </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        bool this[uint index] { get; set; }

        /// <summary> An indexer that allows access to the bit at a given index. This provides both read and write access. </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        bool this[int index] { get; set; }

        /// <summary>
        /// Perform an AND bitwise operation between this BitArray and the one you pass into the function and return the result.
        /// Both BitArrays must have the same capacity. This will not change current BitArray values.
        /// </summary>
        /// <param name="other"> BitArray with which to the And operation. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T BitAnd(T other);

        /// <summary>
        /// Perform an OR bitwise operation between this BitArray and the one you pass into the function and return the result.
        /// Both BitArrays must have the same capacity. This will not change current BitArray values.
        /// </summary>
        /// <param name="other"> BitArray with which to the Or operation. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T BitOr(T other);

        /// <summary> Return the BitArray with every bit inverted. </summary>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T BitNot();

        /// <summary> Count the number of enabled bits. </summary>
        /// <returns> Number of bits set to 1 within x. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int CountBits();
    }

    // /!\ Important for serialization:
    // Serialization helper will rely on the name of the struct type.
    // In order to work, it must be BitArrayN where N is the capacity without suffix.

    /// <summary> Bit array of size 8. </summary>
    [Serializable]
    [DebuggerDisplay("{this.GetType().Name} {HumanizedData}")]
    public struct BitArray8 : IBitArray<BitArray8>
    {
        public static readonly BitArray8 All = new(byte.MaxValue);
        public static readonly BitArray8 None = default;

        [SerializeField]
        private byte data;

        /// <summary> Initializes a new instance of the <see cref="BitArray8" /> struct. </summary>
        /// <param name="initValue"> Initialization value. </param>
        public BitArray8(byte initValue)
        {
            this.data = initValue;
        }

        /// <summary> Initializes a new instance of the <see cref="BitArray8" /> struct. </summary>
        /// <param name="bitIndexTrue"> List of indices where bits should be set to true. </param>
        public BitArray8(Span<uint> bitIndexTrue)
        {
            this.data = (byte)0u;

            foreach (var bitIndex in bitIndexTrue)
            {
                if (bitIndex >= this.Capacity)
                {
                    continue;
                }

                this.data |= (byte)(1u << (int)bitIndex);
            }
        }

        public byte Data
        {
            readonly get => this.data;
            set => this.data = value;
        }

        /// <inheritdoc />
        public readonly uint Capacity => 8u;

        /// <inheritdoc />
        public readonly bool AllFalse => this.data == 0u;

        /// <inheritdoc />
        public readonly bool AllTrue => this.data == byte.MaxValue;

        /// <inheritdoc />
        public readonly string HumanizedData => $"{Convert.ToString(this.data, 2),8}".Replace(' ', '0');

        /// <inheritdoc />
        public bool this[uint index]
        {
            readonly get => BitArrayUtilities.Get8(index, this.data);
            set => BitArrayUtilities.Set8(index, ref this.data, value);
        }

        /// <inheritdoc />
        public bool this[int index]
        {
            readonly get => this[(uint)index];
            set => this[(uint)index] = value;
        }

        /// <summary> Bit-wise Not operator. </summary>
        /// <param name="a"> Bit array with which to do the operation. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray8 operator ~(BitArray8 a)
        {
            return new BitArray8((byte)~a.data);
        }

        /// <summary> Bit-wise Or operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray8 operator |(BitArray8 a, BitArray8 b)
        {
            return new BitArray8((byte)(a.data | b.data));
        }

        /// <summary> Bit-wise And operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray8 operator &(BitArray8 a, BitArray8 b)
        {
            return new BitArray8((byte)(a.data & b.data));
        }

        /// <summary> Equality operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if both bit arrays are equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BitArray8 a, BitArray8 b)
        {
            return a.data == b.data;
        }

        /// <summary> Inequality operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if the bit arrays are not equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BitArray8 a, BitArray8 b)
        {
            return a.data != b.data;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray8 BitAnd(BitArray8 other)
        {
            return this & other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray8 BitOr(BitArray8 other)
        {
            return this | other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray8 BitNot()
        {
            return ~this;
        }

        /// <inheritdoc />
        public readonly int CountBits()
        {
            return math.countbits((uint)this.data);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="obj"> Bit array to compare to. </param>
        /// <returns> True if the provided bit array is equal to this.. </returns>
        public readonly override bool Equals(object obj)
        {
            return obj is BitArray8 array8 && array8.data == this.data;
        }

        /// <summary>
        /// Get the hashcode of the bit array.
        /// </summary>
        /// <returns> Hashcode of the bit array. </returns>
        public readonly override int GetHashCode()
        {
            return 1768953197 + this.data.GetHashCode();
        }

        public readonly bool Equals(BitArray8 other)
        {
            return this.data == other.data;
        }
    }

    /// <summary> Bit array of size 16. </summary>
    [Serializable]
    [DebuggerDisplay("{this.GetType().Name} {HumanizedData}")]
    public struct BitArray16 : IBitArray<BitArray16>
    {
        public static readonly BitArray16 All = new(ushort.MaxValue);
        public static readonly BitArray16 None = default;

        [SerializeField]
        private ushort data;

        /// <summary> Initializes a new instance of the <see cref="BitArray16" /> struct. </summary>
        /// <param name="initValue"> Initialization value. </param>
        public BitArray16(ushort initValue)
        {
            this.data = initValue;
        }

        /// <summary> Initializes a new instance of the <see cref="BitArray16" /> struct. </summary>
        /// <param name="bitIndexTrue"> List of indices where bits should be set to true. </param>
        public BitArray16(Span<uint> bitIndexTrue)
        {
            this.data = (ushort)0u;

            foreach (var bitIndex in bitIndexTrue)
            {
                if (bitIndex >= this.Capacity)
                {
                    continue;
                }

                this.data |= (ushort)(1u << (int)bitIndex);
            }
        }

        public ushort Data
        {
            readonly get => this.data;
            set => this.data = value;
        }

        /// <inheritdoc />
        public readonly uint Capacity => 16u;

        /// <inheritdoc />
        public readonly bool AllFalse => this.data == 0u;

        /// <inheritdoc />
        public readonly bool AllTrue => this.data == ushort.MaxValue;

        /// <inheritdoc />
        public readonly string HumanizedData => Regex.Replace($"{Convert.ToString(this.data, 2),16}".Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        public bool this[uint index]
        {
            readonly get => BitArrayUtilities.Get16(index, this.data);
            set => BitArrayUtilities.Set16(index, ref this.data, value);
        }

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        public bool this[int index]
        {
            readonly get => this[(uint)index];
            set => this[(uint)index] = value;
        }

        /// <summary> Bit-wise Not operator. </summary>
        /// <param name="a"> Bit array with which to do the operation. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray16 operator ~(BitArray16 a)
        {
            return new BitArray16((ushort)~a.data);
        }

        /// <summary> Bit-wise Or operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray16 operator |(BitArray16 a, BitArray16 b)
        {
            return new BitArray16((ushort)(a.data | b.data));
        }

        /// <summary> Bit-wise And operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray16 operator &(BitArray16 a, BitArray16 b)
        {
            return new BitArray16((ushort)(a.data & b.data));
        }

        /// <summary> Equality operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if both bit arrays are equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BitArray16 a, BitArray16 b)
        {
            return a.data == b.data;
        }

        /// <summary> Inequality operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if the bit arrays are not equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BitArray16 a, BitArray16 b)
        {
            return a.data != b.data;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray16 BitAnd(BitArray16 other)
        {
            return this & other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray16 BitOr(BitArray16 other)
        {
            return this | other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray16 BitNot()
        {
            return ~this;
        }

        /// <inheritdoc />
        public readonly int CountBits()
        {
            return math.countbits((uint)this.data);
        }

        /// <summary> Equality operator. </summary>
        /// <param name="obj"> Bit array to compare to. </param>
        /// <returns> True if the provided bit array is equal to this.. </returns>
        public readonly override bool Equals(object obj)
        {
            return obj is BitArray16 array16 && array16.data == this.data;
        }

        /// <summary> Get the hashcode of the bit array. </summary>
        /// <returns> Hashcode of the bit array. </returns>
        public readonly override int GetHashCode()
        {
            return 1768953197 + this.data.GetHashCode();
        }

        public readonly bool Equals(BitArray16 other)
        {
            return this.data == other.data;
        }
    }

    /// <summary> Bit array of size 32. </summary>
    [Serializable]
    [DebuggerDisplay("{this.GetType().Name} {HumanizedData}")]
    public struct BitArray32 : IBitArray<BitArray32>
    {
        public static readonly BitArray32 All = new(uint.MaxValue);
        public static readonly BitArray32 None = default;

        [SerializeField]
        private uint data;

        /// <summary> Initializes a new instance of the <see cref="BitArray32" /> struct. </summary>
        /// <param name="rawValue"> Initialization value. </param>
        public BitArray32(uint rawValue)
        {
            this.data = rawValue;
        }

        /// <summary> Initializes a new instance of the <see cref="BitArray32" /> struct. </summary>
        /// <param name="bitIndexTrue"> List of indices where bits should be set to true. </param>
        public BitArray32(Span<uint> bitIndexTrue)
        {
            this.data = 0u;

            foreach (var bitIndex in bitIndexTrue)
            {
                if (bitIndex >= this.Capacity)
                {
                    continue;
                }

                this.data |= 1u << (int)bitIndex;
            }
        }

        public uint Data
        {
            readonly get => this.data;
            set => this.data = value;
        }

        /// <inheritdoc />
        public readonly uint Capacity => 32u;

        /// <inheritdoc />
        public readonly bool AllFalse => this.data == 0u;

        /// <inheritdoc />
        public readonly bool AllTrue => this.data == uint.MaxValue;

        /// <inheritdoc />
        public readonly string HumanizedData => Regex.Replace($"{Convert.ToString(this.data, 2),32}".Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        public bool this[uint index]
        {
            readonly get => BitArrayUtilities.Get32(index, this.data);
            set => BitArrayUtilities.Set32(index, ref this.data, value);
        }

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        public bool this[int index]
        {
            readonly get => this[(uint)index];
            set => this[(uint)index] = value;
        }

        /// <summary> Bit-wise Not operator. </summary>
        /// <param name="a"> Bit array with which to do the operation. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray32 operator ~(BitArray32 a)
        {
            return new BitArray32(~a.data);
        }

        /// <summary> Bit-wise Or operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray32 operator |(BitArray32 a, BitArray32 b)
        {
            return new BitArray32(a.data | b.data);
        }

        /// <summary> Bit-wise And operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray32 operator &(BitArray32 a, BitArray32 b)
        {
            return new BitArray32(a.data & b.data);
        }

        /// <summary> Equality operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if both bit arrays are equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BitArray32 a, BitArray32 b)
        {
            return a.data == b.data;
        }

        /// <summary> Inequality operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if the bit arrays are not equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BitArray32 a, BitArray32 b)
        {
            return a.data != b.data;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray32 BitAnd(BitArray32 other)
        {
            return this & other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray32 BitOr(BitArray32 other)
        {
            return this | other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray32 BitNot()
        {
            return ~this;
        }

        /// <inheritdoc />
        public readonly int CountBits()
        {
            return math.countbits(this.data);
        }

        /// <summary> Equality operator. </summary>
        /// <param name="obj"> Bit array to compare to. </param>
        /// <returns> True if the provided bit array is equal to this.. </returns>
        public readonly override bool Equals(object obj)
        {
            return obj is BitArray32 array32 && array32.data == this.data;
        }

        /// <summary> Get the hashcode of the bit array. </summary>
        /// <returns> Hashcode of the bit array. </returns>
        public readonly override int GetHashCode()
        {
            return 1768953197 + this.data.GetHashCode();
        }

        public readonly bool Equals(BitArray32 other)
        {
            return this.data == other.data;
        }
    }

    /// <summary>
    /// Bit array of size 64.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{this.GetType().Name} {HumanizedData}")]
    public struct BitArray64 : IBitArray<BitArray64>
    {
        public static readonly BitArray64 All = new(ulong.MaxValue);
        public static readonly BitArray64 None = default;

        [SerializeField]
        private ulong data;

        /// <summary> Initializes a new instance of the <see cref="BitArray64" /> struct. </summary>
        /// <param name="initValue"> Initialization value. </param>
        public BitArray64(ulong initValue)
        {
            this.data = initValue;
        }

        /// <summary> Initializes a new instance of the <see cref="BitArray64" /> struct. </summary>
        /// <param name="bitIndexTrue"> Single initial index that is set to true. </param>
        public unsafe BitArray64(uint bitIndexTrue)
            : this(new Span<uint>(&bitIndexTrue, 1))
        {
        }

        /// <summary> Initializes a new instance of the <see cref="BitArray64" /> struct. </summary>
        /// <param name="bitIndexTrue"> List of indices where bits should be set to true. </param>
        public BitArray64(Span<uint> bitIndexTrue)
        {
            this.data = 0L;

            foreach (var bitIndex in bitIndexTrue)
            {
                if (bitIndex >= this.Capacity)
                {
                    continue;
                }

                this.data |= 1uL << (int)bitIndex;
            }
        }

        public ulong Data
        {
            readonly get => this.data;
            set => this.data = value;
        }

        /// <inheritdoc />
        public readonly uint Capacity => 64u;

        /// <inheritdoc />
        public readonly bool AllFalse => this.data == 0uL;

        /// <inheritdoc />
        public readonly bool AllTrue => this.data == ulong.MaxValue;

        /// <inheritdoc />
        public readonly string HumanizedData => Regex.Replace($"{Convert.ToString((long)this.data, 2),64}".Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        public bool this[uint index]
        {
            readonly get => BitArrayUtilities.Get64(index, this.data);
            set => BitArrayUtilities.Set64(index, ref this.data, value);
        }

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        public bool this[int index]
        {
            readonly get => this[(uint)index];
            set => this[(uint)index] = value;
        }

        /// <summary> Bit-wise Not operator. </summary>
        /// <param name="a"> Bit array with which to do the operation. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray64 operator ~(BitArray64 a)
        {
            return new BitArray64(~a.data);
        }

        /// <summary> Bit-wise Or operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray64 operator |(BitArray64 a, BitArray64 b)
        {
            return new BitArray64(a.data | b.data);
        }

        /// <summary> Bit-wise And operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray64 operator &(BitArray64 a, BitArray64 b)
        {
            return new BitArray64(a.data & b.data);
        }

        /// <summary> Equality operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if both bit arrays are equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BitArray64 a, BitArray64 b)
        {
            return a.data == b.data;
        }

        /// <summary> Inequality operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if the bit arrays are not equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BitArray64 a, BitArray64 b)
        {
            return a.data != b.data;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray64 BitAnd(BitArray64 other)
        {
            return this & other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray64 BitOr(BitArray64 other)
        {
            return this | other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray64 BitNot()
        {
            return ~this;
        }

        /// <inheritdoc />
        public readonly int CountBits()
        {
            return math.countbits(this.data);
        }

        /// <summary> Equality operator. </summary>
        /// <param name="obj"> Bit array to compare to. </param>
        /// <returns> True if the provided bit array is equal to this.. </returns>
        public readonly override bool Equals(object obj)
        {
            return obj is BitArray64 array64 && array64.data == this.data;
        }

        /// <summary> Get the hashcode of the bit array. </summary>
        /// <returns> Hashcode of the bit array. </returns>
        public readonly override int GetHashCode()
        {
            return 1768953197 + this.data.GetHashCode();
        }

        public readonly bool Equals(BitArray64 other)
        {
            return this.data == other.data;
        }
    }

    /// <summary> Bit array of size 128. </summary>
    [Serializable]
    [DebuggerDisplay("{this.GetType().Name} {HumanizedData}")]
    public struct BitArray128 : IBitArray<BitArray128>
    {
        public static readonly BitArray128 All = new(ulong.MaxValue, ulong.MaxValue);
        public static readonly BitArray128 None = default;

        [SerializeField]
        private ulong data1;

        [SerializeField]
        private ulong data2;

        /// <summary> Initializes a new instance of the <see cref="BitArray128" /> struct. </summary>
        /// <param name="initValue1"> Initialization value 1. </param>
        /// <param name="initValue2"> Initialization value 2. </param>
        public BitArray128(ulong initValue1, ulong initValue2)
        {
            this.data1 = initValue1;
            this.data2 = initValue2;
        }

        /// <summary> Initializes a new instance of the <see cref="BitArray128" /> struct. </summary>
        /// <param name="initValue"> Initialization value. </param>
        public BitArray128(v128 initValue)
        {
            this.data1 = initValue.ULong0;
            this.data2 = initValue.ULong1;
        }

        /// <summary> Initializes a new instance of the <see cref="BitArray128" /> struct. </summary>
        /// <param name="bitIndexTrue"> Single initial index that is set to true. </param>
        public unsafe BitArray128(uint bitIndexTrue)
            : this(new Span<uint>(&bitIndexTrue, 1))
        {
        }

        /// <summary> Initializes a new instance of the <see cref="BitArray128" /> struct. </summary>
        /// <param name="bitIndexTrue"> List of indices where bits should be set to true. </param>
        public BitArray128(Span<uint> bitIndexTrue)
        {
            this.data1 = this.data2 = 0uL;

            foreach (var bitIndex in bitIndexTrue)
            {
                if (bitIndex < 64u)
                {
                    this.data1 |= 1uL << (int)bitIndex;
                }
                else
                {
                    if (bitIndex < this.Capacity)
                    {
                        this.data2 |= 1uL << (int)(bitIndex - 64u);
                    }
                }
            }
        }

        public readonly ulong Data1 => this.data1;

        public readonly ulong Data2 => this.data2;

        /// <inheritdoc />
        public readonly uint Capacity => 128u;

        /// <inheritdoc />
        public readonly bool AllFalse => this.data1 == 0uL && this.data2 == 0uL;

        /// <inheritdoc />
        public readonly bool AllTrue => this.data1 == ulong.MaxValue && this.data2 == ulong.MaxValue;

        /// <inheritdoc />
        public readonly string HumanizedData => Regex.Replace($"{Convert.ToString((long)this.data2, 2),64}".Replace(' ', '0'), ".{8}", "$0.") +
            Regex.Replace($"{Convert.ToString((long)this.data1, 2),64}".Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        /// <summary> Returns the state of the bit at a specific index. </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        public bool this[uint index]
        {
            readonly get => BitArrayUtilities.Get128(index, this.data1, this.data2);
            set => BitArrayUtilities.Set128(index, ref this.data1, ref this.data2, value);
        }

        /// <summary> Returns the state of the bit at a specific index. </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        public bool this[int index]
        {
            readonly get => BitArrayUtilities.Get128(index, this.data1, this.data2);
            set => BitArrayUtilities.Set128(index, ref this.data1, ref this.data2, value);
        }

        /// <summary> Bit-wise Not operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray128 operator ~(BitArray128 a)
        {
            return new BitArray128(~a.data1, ~a.data2);
        }

        /// <summary> Bit-wise Or operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray128 operator |(BitArray128 a, BitArray128 b)
        {
            return new BitArray128(a.data1 | b.data1, a.data2 | b.data2);
        }

        /// <summary> Bit-wise And operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray128 operator &(BitArray128 a, BitArray128 b)
        {
            return new BitArray128(a.data1 & b.data1, a.data2 & b.data2);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if both bit arrays are equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BitArray128 a, BitArray128 b)
        {
            return a.data1 == b.data1 && a.data2 == b.data2;
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if the bit arrays are not equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BitArray128 a, BitArray128 b)
        {
            return a.data1 != b.data1 || a.data2 != b.data2;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray128 BitAnd(BitArray128 other)
        {
            return this & other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray128 BitOr(BitArray128 other)
        {
            return this | other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray128 BitNot()
        {
            return ~this;
        }

        /// <inheritdoc />
        public readonly int CountBits()
        {
            return math.countbits(this.data1) + math.countbits(this.data2);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="obj"> Bit array to compare to. </param>
        /// <returns> True if the provided bit array is equal to this.. </returns>
        public readonly override bool Equals(object obj)
        {
            return obj is BitArray128 array128 && this.data1.Equals(array128.data1) && this.data2.Equals(array128.data2);
        }

        /// <summary>
        /// Get the hashcode of the bit array.
        /// </summary>
        /// <returns> Hashcode of the bit array. </returns>
        public readonly override int GetHashCode()
        {
            var hashCode = 1755735569;
            hashCode = (hashCode * -1521134295) + this.data1.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.data2.GetHashCode();
            return hashCode;
        }

        public readonly bool Equals(BitArray128 other)
        {
            return this.data1 == other.data1 && this.data2 == other.data2;
        }
    }

    /// <summary>
    /// Bit array of size 256.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{this.GetType().Name} {HumanizedData}")]
    public struct BitArray256 : IBitArray<BitArray256>
    {
        public static readonly BitArray256 All = new(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
        public static readonly BitArray256 None = default;

        [SerializeField]
        [DontCreateProperty]
        private ulong data1;

        [SerializeField]
        [DontCreateProperty]
        private ulong data2;

        [SerializeField]
        [DontCreateProperty]
        private ulong data3;

        [SerializeField]
        [DontCreateProperty]
        private ulong data4;

        /// <summary> Initializes a new instance of the <see cref="BitArray256" /> struct. </summary>
        /// <param name="initValue1"> Initialization value 1. </param>
        /// <param name="initValue2"> Initialization value 2. </param>
        /// <param name="initValue3"> Initialization value 3. </param>
        /// <param name="initValue4"> Initialization value 4. </param>
        public BitArray256(ulong initValue1, ulong initValue2, ulong initValue3, ulong initValue4)
        {
            this.data1 = initValue1;
            this.data2 = initValue2;
            this.data3 = initValue3;
            this.data4 = initValue4;
        }

        /// <summary> Initializes a new instance of the <see cref="BitArray256" /> struct. </summary>
        /// <param name="bitIndexTrue"> Single initial index that is set to true. </param>
        public unsafe BitArray256(uint bitIndexTrue)
            : this(new Span<uint>(&bitIndexTrue, 1))
        {
        }

        /// <summary> Initializes a new instance of the <see cref="BitArray256" /> struct. </summary>
        /// <param name="bitIndexTrue"> List of indices where bits should be set to true. </param>
        public BitArray256(Span<uint> bitIndexTrue)
        {
            this.data1 = this.data2 = this.data3 = this.data4 = 0uL;

            foreach (var bitIndex in bitIndexTrue)
            {
                if (bitIndex < 64u)
                {
                    this.data1 |= 1uL << (int)bitIndex;
                }
                else if (bitIndex < 128u)
                {
                    this.data2 |= 1uL << (int)(bitIndex - 64u);
                }
                else if (bitIndex < 192u)
                {
                    this.data3 |= 1uL << (int)(bitIndex - 128u);
                }
                else if (bitIndex < this.Capacity)
                {
                    this.data4 |= 1uL << (int)(bitIndex - 192u);
                }
            }
        }

        [CreateProperty]
        public readonly ulong Data1 => this.data1;

        [CreateProperty]
        public readonly ulong Data2 => this.data2;

        [CreateProperty]
        public readonly ulong Data3 => this.data3;

        [CreateProperty]
        public readonly ulong Data4 => this.data4;

        /// <inheritdoc />
        public readonly uint Capacity => 256u;

        /// <inheritdoc />
        public readonly bool AllFalse => this.data1 == 0uL && this.data2 == 0uL && this.data3 == 0uL && this.data4 == 0uL;

        /// <inheritdoc />
        public readonly bool AllTrue =>
            this.data1 == ulong.MaxValue && this.data2 == ulong.MaxValue && this.data3 == ulong.MaxValue && this.data4 == ulong.MaxValue;

        /// <inheritdoc />
        public readonly string HumanizedData => Regex.Replace($"{Convert.ToString((long)this.data4, 2),64}".Replace(' ', '0'), ".{8}", "$0.") +
            Regex.Replace($"{Convert.ToString((long)this.data3, 2),64}".Replace(' ', '0'), ".{8}", "$0.") +
            Regex.Replace($"{Convert.ToString((long)this.data2, 2),64}".Replace(' ', '0'), ".{8}", "$0.") +
            Regex.Replace($"{Convert.ToString((long)this.data1, 2),64}".Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        /// <summary> Returns the state of the bit at a specific index. </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        public bool this[uint index]
        {
            readonly get => BitArrayUtilities.Get256(index, this.data1, this.data2, this.data3, this.data4);
            set => BitArrayUtilities.Set256(index, ref this.data1, ref this.data2, ref this.data3, ref this.data4, value);
        }

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index"> Index of the bit. </param>
        /// <returns> State of the bit at the provided index. </returns>
        public bool this[int index]
        {
            readonly get => this[(uint)index];
            set => this[(uint)index] = value;
        }

        /// <summary> Bit-wise Not operator. </summary>
        /// <param name="a"> Bit array with which to do the operation. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray256 operator ~(BitArray256 a)
        {
            return new BitArray256(~a.data1, ~a.data2, ~a.data3, ~a.data4);
        }

        /// <summary> Bit-wise Or operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray256 operator |(BitArray256 a, BitArray256 b)
        {
            return new BitArray256(a.data1 | b.data1, a.data2 | b.data2, a.data3 | b.data3, a.data4 | b.data4);
        }

        /// <summary> Bit-wise And operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> The resulting bit array. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitArray256 operator &(BitArray256 a, BitArray256 b)
        {
            return new BitArray256(a.data1 & b.data1, a.data2 & b.data2, a.data3 & b.data3, a.data4 & b.data4);
        }

        /// <summary> Equality operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if both bit arrays are equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BitArray256 a, BitArray256 b)
        {
            return a.data1 == b.data1 && a.data2 == b.data2 && a.data3 == b.data3 && a.data4 == b.data4;
        }

        /// <summary> Inequality operator. </summary>
        /// <param name="a"> First bit array. </param>
        /// <param name="b"> Second bit array. </param>
        /// <returns> True if the bit arrays are not equals. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BitArray256 a, BitArray256 b)
        {
            return a.data1 != b.data1 || a.data2 != b.data2 || a.data3 != b.data3 || a.data4 != b.data4;
        }

        public readonly bool IsPowerOf2()
        {
            // Means only 1 bit set
            return math.countbits(this.data1) + math.countbits(this.data2) + math.countbits(this.data3) + math.countbits(this.data4) == 1;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray256 BitAnd(BitArray256 other)
        {
            return this & other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray256 BitOr(BitArray256 other)
        {
            return this | other;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitArray256 BitNot()
        {
            return ~this;
        }

        /// <inheritdoc />
        public readonly int CountBits()
        {
            return math.countbits(this.data1) + math.countbits(this.data2) + math.countbits(this.data3) + math.countbits(this.data4);
        }

        /// <summary> Equality operator. </summary>
        /// <param name="obj"> Bit array to compare to. </param>
        /// <returns> True if the provided bit array is equal to this.. </returns>
        public readonly override bool Equals(object obj)
        {
            return obj is BitArray256 array256 &&
                this.data1.Equals(array256.data1) &&
                this.data2.Equals(array256.data2) &&
                this.data3.Equals(array256.data3) &&
                this.data4.Equals(array256.data4);
        }

        /// <summary> Get the hashcode of the bit array. </summary>
        /// <returns> Hashcode of the bit array. </returns>
        public readonly override int GetHashCode()
        {
            var hashCode = 1870826326;
            hashCode = (hashCode * -1521134295) + this.data1.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.data2.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.data3.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.data4.GetHashCode();
            return hashCode;
        }

        public readonly bool Equals(BitArray256 other)
        {
            return this.data1 == other.data1 && this.data2 == other.data2 && this.data3 == other.data3 && this.data4 == other.data4;
        }
    }

    /// <summary>
    /// Bit array utility class.
    /// </summary>
    public static class BitArrayUtilities
    {
        /// <summary> Get a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data"> Bit array data. </param>
        /// <returns> The value of the bit at the specific index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Get8(uint index, byte data)
        {
            return (data & (1u << (int)index)) != 0u;
        }

        /// <summary> Get a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data"> Bit array data. </param>
        /// <returns> The value of the bit at the specific index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Get16(uint index, ushort data)
        {
            return (data & (1u << (int)index)) != 0u;
        }

        /// <summary> Get a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data"> Bit array data. </param>
        /// <returns> The value of the bit at the specific index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Get32(uint index, uint data)
        {
            return (data & (1u << (int)index)) != 0u;
        }

        /// <summary> Get a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data"> Bit array data. </param>
        /// <returns> The value of the bit at the specific index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Get64(uint index, ulong data)
        {
            return (data & (1uL << (int)index)) != 0uL;
        }

        /// <summary> Get a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data1"> Bit array data 1. </param>
        /// <param name="data2"> Bit array data 2. </param>
        /// <returns> The value of the bit at the specific index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Get128(uint index, ulong data1, ulong data2)
        {
            return Get128((int)index, data1, data2);
        }

        /// <summary> Get a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data1"> Bit array data 1. </param>
        /// <param name="data2"> Bit array data 2. </param>
        /// <returns> The value of the bit at the specific index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Get128(int index, ulong data1, ulong data2)
        {
            return index < 64u ? (data1 & (1uL << index)) != 0uL : (data2 & (1uL << (index - 64))) != 0uL;
        }

        /// <summary> Get a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data1"> Bit array data 1. </param>
        /// <param name="data2"> Bit array data 2. </param>
        /// <param name="data3"> Bit array data 3. </param>
        /// <param name="data4"> Bit array data 4. </param>
        /// <returns> The value of the bit at the specific index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Get256(uint index, ulong data1, ulong data2, ulong data3, ulong data4)
        {
            return index < 128u ? index < 64u ? (data1 & (1uL << (int)index)) != 0uL : (data2 & (1uL << (int)(index - 64u))) != 0uL :
                index < 192u ? (data3 & (1uL << (int)(index - 128u))) != 0uL : (data4 & (1uL << (int)(index - 192u))) != 0uL;
        }

        /// <summary> Set a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data"> Bit array data. </param>
        /// <param name="value"> Value to set the bit to. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set8(uint index, ref byte data, bool value)
        {
            data = (byte)(value ? data | (1u << (int)index) : data & ~(1u << (int)index));
        }

        /// <summary> Set a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data"> Bit array data. </param>
        /// <param name="value"> Value to set the bit to. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set16(uint index, ref ushort data, bool value)
        {
            data = (ushort)(value ? data | (1u << (int)index) : data & ~(1u << (int)index));
        }

        /// <summary> Set a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data"> Bit array data. </param>
        /// <param name="value"> Value to set the bit to. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set32(uint index, ref uint data, bool value)
        {
            data = value ? data | (1u << (int)index) : data & ~(1u << (int)index);
        }

        /// <summary> Set a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data"> Bit array data. </param>
        /// <param name="value"> Value to set the bit to. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set64(uint index, ref ulong data, bool value)
        {
            data = value ? data | (1uL << (int)index) : data & ~(1uL << (int)index);
        }

        /// <summary> Set a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data1"> Bit array data 1. </param>
        /// <param name="data2"> Bit array data 2. </param>
        /// <param name="value"> Value to set the bit to. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set128(uint index, ref ulong data1, ref ulong data2, bool value)
        {
            Set128((int)index, ref data1, ref data2, value);
        }

        /// <summary> Set a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data1"> Bit array data 1. </param>
        /// <param name="data2"> Bit array data 2. </param>
        /// <param name="value"> Value to set the bit to. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set128(int index, ref ulong data1, ref ulong data2, bool value)
        {
            if (index < 64u)
            {
                data1 = value ? data1 | (1uL << index) : data1 & ~(1uL << index);
            }
            else
            {
                data2 = value ? data2 | (1uL << (index - 64)) : data2 & ~(1uL << (index - 64));
            }
        }

        /// <summary> Set a bit at a specific index. </summary>
        /// <param name="index"> Bit index. </param>
        /// <param name="data1"> Bit array data 1. </param>
        /// <param name="data2"> Bit array data 2. </param>
        /// <param name="data3"> Bit array data 3. </param>
        /// <param name="data4"> Bit array data 4. </param>
        /// <param name="value"> Value to set the bit to. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set256(uint index, ref ulong data1, ref ulong data2, ref ulong data3, ref ulong data4, bool value)
        {
            if (index < 64u)
            {
                data1 = value ? data1 | (1uL << (int)index) : data1 & ~(1uL << (int)index);
            }
            else if (index < 128u)
            {
                data2 = value ? data2 | (1uL << (int)(index - 64u)) : data2 & ~(1uL << (int)(index - 64u));
            }
            else if (index < 192u)
            {
                data3 = value ? data3 | (1uL << (int)(index - 64u)) : data3 & ~(1uL << (int)(index - 128u));
            }
            else
            {
                data4 = value ? data4 | (1uL << (int)(index - 64u)) : data4 & ~(1uL << (int)(index - 192u));
            }
        }
    }
}