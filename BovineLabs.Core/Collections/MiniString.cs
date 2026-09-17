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

        public int Capacity
        {
            get => UTF8MaxLengthInBytes;
            set => this.CheckCapacityInRange(value);
        }

        public bool IsEmpty => this.UTF8LengthInBytes == 0;

        private byte UTF8LengthInBytes
        {
            get => this.bytes.byte0000;
            set => this.bytes.byte0000 = value;
        }

        /// <summary>
        /// Indexes UTF-8 bytes, not characters.
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
        /// Returns a reference to a UTF-8 byte, not a character; valid only while the string is valid.
        /// </summary>
        public ref byte ElementAt(int index)
        {
            unsafe
            {
                this.CheckIndexInRange(index);
                return ref this.GetUnsafePtr()[index];
            }
        }

        public void Clear()
        {
            this.Length = 0;
        }

        /// <summary>
        /// Does not validate appended UTF-8 bytes; invalid sequences corrupt conversion to UTF-16 or UCS-2.
        /// </summary>
        public void Add(in byte value)
        {
            this[this.Length++] = value;
        }

        public int CompareTo(string other)
        {
            return this.ToString().CompareTo(other);
        }

        public bool Equals(string other)
        {
            return this.ToString().Equals(other);
        }

        public bool Equals(MiniString other)
        {
            return this == other;
        }

        public int CompareTo(MiniString other)
        {
            return FixedStringMethods.CompareTo(ref this, other);
        }

        public override string ToString()
        {
            return this.ConvertToString();
        }

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
