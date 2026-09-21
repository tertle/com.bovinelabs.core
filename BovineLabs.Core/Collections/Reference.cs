namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Memory;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Entities.Serialization;
    using Unity.Mathematics;

    /// <summary>
    /// Based on BlobAssetReference&lt;T&gt;.
    /// </summary>
    public readonly unsafe struct Reference<T> : IEquatable<Reference<T>>
        where T : unmanaged
    {
        private readonly ReferenceData _data;

        public Reference(ReferenceData value)
        {
            _data = value;
        }

        public static Reference<T> Null => default(Reference<T>);

        public bool IsCreated => _data.Ptr != null;

        public ref T Value
        {
            get
            {
                _data.ValidateNotNull();
                return ref UnsafeUtility.AsRef<T>(_data.Ptr);
            }
        }

        public static bool operator ==(Reference<T> lhs, Reference<T> rhs)
        {
            return lhs._data.Ptr == rhs._data.Ptr;
        }

        public static bool operator !=(Reference<T> lhs, Reference<T> rhs)
        {
            return lhs._data.Ptr != rhs._data.Ptr;
        }

        public static Reference<T> Create(void* ptr, int length, MemoryAllocator allocator)
        {
            var buffer = (byte*)allocator.Allocate(sizeof(ReferenceHeader) + length, 16);
            UnsafeUtility.MemCpy(buffer + sizeof(ReferenceHeader), ptr, length);

            ReferenceHeader* header = (ReferenceHeader*)buffer;
            *header = default(ReferenceHeader);

            header->Length = length;

            ReferenceData data;
            data.Align8Union = 0;
            header->ValidationPtr = data.Ptr = buffer + sizeof(ReferenceHeader);
            return new Reference<T>(data);
        }

        /// <summary>
        /// Sizes are in bytes; data is stored immediately after the header.
        /// </summary>
        public static Reference<T> Create(void* headerPtr, int headerLength, void* dataPtr, int dataLength, MemoryAllocator allocator)
        {
            byte* buffer = (byte*)allocator.Allocate(sizeof(ReferenceHeader) + headerLength + dataLength, 16);
            UnsafeUtility.MemCpy(buffer + sizeof(ReferenceHeader), headerPtr, headerLength);
            UnsafeUtility.MemCpy(buffer + sizeof(ReferenceHeader) + headerLength, dataPtr, dataLength);

            ReferenceHeader* header = (ReferenceHeader*)buffer;
            *header = default(ReferenceHeader);

            header->Length = headerLength + dataLength;

            ReferenceData data;
            data.Align8Union = 0;
            header->ValidationPtr = data.Ptr = buffer + sizeof(ReferenceHeader);
            return new Reference<T>(data);
        }

        public static Reference<T> Create(byte[] data, MemoryAllocator allocator)
        {
            fixed (byte* ptr = &data[0])
            {
                return Create(ptr, data.Length, allocator);
            }
        }

        /// <summary>
        /// T must contain only blittable fields.
        /// </summary>
        public static Reference<T> Create(T value, MemoryAllocator allocator)
        {
            return Create(UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<T>(), allocator);
        }

        /// <summary>
        /// Returns null for an invalid Reference.
        /// </summary>
        public void* GetUnsafePtr()
        {
            _data.ValidateAllowNull();
            return _data.Ptr;
        }

        public ReferenceData ReferenceData => _data;

        // /// <summary> Destroys the referenced blob asset and frees its memory. </summary>
        // /// <exception cref="InvalidOperationException">Thrown if you attempt to dispose a blob asset that loaded as
        // /// part of a scene or subscene.</exception>
        // public void Dispose()
        // {
        //     this.data.Dispose();
        // }

        public bool Equals(Reference<T> other)
        {
            return _data.Equals(other._data);
        }

        public override bool Equals(object obj)
        {
            return this == (Reference<T>)obj;
        }

        public override int GetHashCode()
        {
            return _data.GetHashCode();
        }

        internal static Reference<T> Create(ReferenceData blobData)
        {
            return new Reference<T>(blobData);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public unsafe struct ReferenceData
    {
        [NativeDisableUnsafePtrRestriction]
        [FieldOffset(0)]
        public byte* Ptr;

        /// <summary>
        /// Overlaps the pointer to force eight-byte alignment, as in a C union.
        /// </summary>
        [FieldOffset(0)]
        internal long Align8Union;

        internal ReferenceHeader* Header
        {
            get { return ((ReferenceHeader*)Ptr) - 1; }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateNotNull()
        {
            if (Ptr == null)
            {
                throw new InvalidOperationException("The BlobAssetReference is null.");
            }

            ValidateNonBurst();
            ValidateBurst();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateAllowNull()
        {
            if (Ptr == null)
            {
                return;
            }

            ValidateNonBurst();
            ValidateBurst();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        [SuppressMessage("ReSharper", "ERP022", Justification = "Intentional for the check.")]
        private void ValidateNonBurst()
        {
            void* validationPtr = null;
            try
            {
                // Try to read ValidationPtr, this might throw if the memory has been unmapped
                validationPtr = Header->ValidationPtr;
            }
            catch (Exception)
            {
            }

            if (validationPtr != Ptr)
            {
                throw new InvalidOperationException("The Reference is not valid. Likely it has already been unloaded or released.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidateBurst()
        {
            void* validationPtr = Header->ValidationPtr;
            if (validationPtr != Ptr)
            {
                throw new InvalidOperationException("The Reference is not valid. Likely it has already been unloaded or released.");
            }
        }
    }

    internal unsafe struct ReferenceHeader
    {
        public void* ValidationPtr;
        public int Length;
    }
}
