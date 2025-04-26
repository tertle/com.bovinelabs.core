// <copyright file="MiniString.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct MiniString : INativeList<byte>, IUTF8Bytes, IEquatable<MiniString>
    {
        internal const ushort UTF8MaxLengthInBytes = 15;

        // First byte is utf8LengthInBytes
        [SerializeField]
        private FixedBytes16 bytes;

        /// <summary> Initializes a new instance of the <see cref="MiniString" /> struct. </summary>
        /// <param name="source"> The System.String object to construct this MiniString with. </param>
        public MiniString(string source)
        {
            this.bytes = default;
            unsafe
            {
                fixed (char* sourceptr = source)
                {
                    var error = UTF8ArrayUnsafeUtility.Copy(this.GetUnsafePtr(), out var lengthInBytes, UTF8MaxLengthInBytes, sourceptr, source.Length);
                    this.UTF8LengthInBytes = (byte)lengthInBytes;
                    CheckCopyError(error, source);
                    this.Length = this.UTF8LengthInBytes;
                }
            }
        }

        public MiniString(FixedString32Bytes source)
        {
            this.bytes = default;

            this.UTF8LengthInBytes = (byte)source.Length;
            this.Length = this.UTF8LengthInBytes;

            unsafe
            {
                UnsafeUtility.MemCpy(this.GetUnsafePtr(), source.GetUnsafePtr(), this.Length);
            }
        }

        public int Length
        {
            get => this.UTF8LengthInBytes;
            set
            {
                this.CheckLengthInRange(value);
                this.UTF8LengthInBytes = (byte)value;
            }
        }

        /// <summary>
        /// The maximum available capacity of the UTF-8 encoded string, in bytes.
        /// Due to the UTF-8 encoding, each Unicode code point requires between 1 and 4 bytes to encode.
        /// The null terminating byte is not included in the capacity.  The FixedString always
        /// has space for a null terminating byte.  For FixedString32, attempting to set this value
        /// to anything lower than 29 will throw.  The Capacity will always be 29.
        /// </summary>
        public int Capacity
        {
            get => UTF8MaxLengthInBytes;
            set => this.CheckCapacityInRange(value);
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value> True if this container empty. </value>
        public bool IsEmpty => this.UTF8LengthInBytes == 0;

        private byte UTF8LengthInBytes
        {
            get => this.bytes.byte0000;
            set => this.bytes.byte0000 = value;
        }

        /// <summary>
        /// Return the byte at the given byte (not character) index.  The index
        /// must be in the range of [0..Length)
        /// </summary>
        public byte this[int index]
        {
            get
            {
                unsafe
                {
                    this.CheckIndexInRange(index);
                    return this.GetUnsafePtr()[index];
                }
            }

            set
            {
                unsafe
                {
                    this.CheckIndexInRange(index);
                    this.GetUnsafePtr()[index] = value;
                }
            }
        }

        /// <summary> Enable implicit conversion of System.String to FixedString32. </summary>
        /// <param name="s"> The System.String object to convert to a FixedString32. </param>
        /// <returns> </returns>
        public static implicit operator MiniString(string s)
        {
            return new MiniString(s);
        }

        public static implicit operator MiniString(FixedString32Bytes s)
        {
            return new MiniString(s);
        }

        public static implicit operator FixedString32Bytes(MiniString b)
        {
            var fs = new FixedString32Bytes
            {
                Length = b.Length,
            };

            unsafe
            {
                UnsafeUtility.MemCpy(fs.GetUnsafePtr(), b.GetUnsafePtr(), b.Length);
            }

            return fs;
        }

        public static bool operator ==(in MiniString a, in MiniString b)
        {
            // this must not call any methods on 'a' or 'b'
            unsafe
            {
                int alen = a.UTF8LengthInBytes;
                int blen = b.UTF8LengthInBytes;
                var aptr = a.GetUnsafePtr();
                var bptr = b.GetUnsafePtr();
                return UTF8ArrayUnsafeUtility.EqualsUTF8Bytes(aptr, alen, bptr, blen);
            }
        }

        public static bool operator !=(in MiniString a, in MiniString b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj switch
            {
                null => false,
                string aString => this.Equals(aString),
                MiniString miniString => this.Equals(miniString),
                _ => false,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte* GetUnsafePtr()
        {
            return (byte*)UnsafeUtility.AddressOf(ref this.bytes.byte0001);
        }

        public bool TryResize(int newLength, NativeArrayOptions clearOptions = NativeArrayOptions.ClearMemory)
        {
            if (newLength < 0 || newLength > UTF8MaxLengthInBytes)
            {
                return false;
            }

            if (newLength == this.UTF8LengthInBytes)
            {
                return true;
            }

            unsafe
            {
                if (clearOptions == NativeArrayOptions.ClearMemory)
                {
                    if (newLength > this.UTF8LengthInBytes)
                    {
                        UnsafeUtility.MemClear(this.GetUnsafePtr() + this.UTF8LengthInBytes, newLength - this.UTF8LengthInBytes);
                    }
                    else
                    {
                        UnsafeUtility.MemClear(this.GetUnsafePtr() + newLength, this.UTF8LengthInBytes - newLength);
                    }
                }

                this.UTF8LengthInBytes = (byte)newLength;
            }

            return true;
        }

        /// <summary>
        /// Return a ref to the the byte at the given byte (not character) index.  The index
        /// must be in the range of [0..Length).  The ref byte is a direct reference into
        /// this FixedString, and is only valid while this FixedString is valid.
        /// </summary>
        /// <param name="index"> The byte index to access </param>
        /// <returns> A ref byte for the requested index </returns>
        public ref byte ElementAt(int index)
        {
            unsafe
            {
                this.CheckIndexInRange(index);
                return ref this.GetUnsafePtr()[index];
            }
        }

        /// <summary>
        /// Clear this string by setting its Length to 0.
        /// </summary>
        public void Clear()
        {
            this.Length = 0;
        }

        /// <summary>
        /// Append the given byte value to this string. The string will remain null-terminated after the new
        /// byte. Appending an invalid UTF-8 sequence will cause the contents of this string to be invalid when
        /// converted to UTF-16 or UCS-2. No validation of the appended bytes is done.
        /// </summary>
        /// <param name="value"> The byte to append. </param>
        public void Add(in byte value)
        {
            this[this.Length++] = value;
        }

        /// <summary>
        /// Compare this FixedString32 with a System.String in terms of lexigraphical order,
        /// and return which of the two strings would come first if sorted.
        /// </summary>
        /// <param name="other"> The System.String to compare with </param>
        /// <returns>
        /// -1 if this FixedString32 would appear first if sorted,
        /// 0 if they are identical, or
        /// 1 if the other System.String would appear first if sorted.
        /// </returns>
        public int CompareTo(string other)
        {
            return this.ToString().CompareTo(other);
        }

        /// <summary>
        /// Compare this FixedString32 with a System.String,
        /// and return whether they contain the same string or not.
        /// </summary>
        /// <param name="other"> The System.String to compare with </param>
        /// <returns> true if they are equal, or false if they are not. </returns>
        public bool Equals(string other)
        {
            return this.ToString().Equals(other);
        }

        public bool Equals(MiniString other)
        {
            return this == other;
        }

        /// <summary>
        /// Compare this FixedString32 with a FixedString32 in terms of lexigraphical order,
        /// and return which of the two strings would come first if sorted.
        /// </summary>
        /// <param name="other"> The FixedString to compare with </param>
        /// <returns>
        /// -1 if this FixedString32 would appear first if sorted,
        /// 0 if they are identical, or
        /// 1 if the other FixedString32 would appear first if sorted.
        /// </returns>
        public int CompareTo(MiniString other)
        {
            return FixedStringMethods.CompareTo(ref this, other);
        }

        /// <summary>
        /// Convert this FixedString32 to a System.String.
        /// </summary>
        /// <returns> A System.String with a copy of this FixedString32 </returns>
        public override string ToString()
        {
            return this.ConvertToString();
        }

        /// <summary>
        /// Compute a hash code of this FixedString32: an integer that is likely to be different for
        /// two FixedString32, if their contents are different.
        /// </summary>
        /// <returns> A hash code of this FixedString32 </returns>
        public override int GetHashCode()
        {
            return this.ComputeHashCode();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckIndexInRange(int index)
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException($"Index {index} must be positive.");
            }

            if (index >= this.UTF8LengthInBytes)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in FixedString32 of '{this.UTF8LengthInBytes}' Length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckLengthInRange(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException($"Length {length} must be positive.");
            }

            if (length > UTF8MaxLengthInBytes)
            {
                throw new ArgumentOutOfRangeException($"Length {length} is out of range in FixedString32 of '{UTF8MaxLengthInBytes}' Capacity.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckCapacityInRange(int capacity)
        {
            if (capacity > UTF8MaxLengthInBytes)
            {
                throw new ArgumentOutOfRangeException($"Capacity {capacity} must be lower than {UTF8MaxLengthInBytes}.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyError(CopyError error, string source)
        {
            if (error != CopyError.None)
            {
                throw new ArgumentException($"FixedString32: {error} while copying \"{source}\"");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckFormatError(FormatError error)
        {
            if (error != FormatError.None)
            {
                throw new ArgumentException("Source is too long to fit into fixed string of this size");
            }
        }
    }
}
