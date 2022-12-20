// <copyright file="Reference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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

    /// <summary> Implements a reference type, based off <see cref="BlobAssetReference{T}"/>. </summary>
    /// <typeparam name="T"> The type to hold. </typeparam>
    public readonly unsafe struct Reference<T> : IEquatable<Reference<T>>
        where T : unmanaged
    {
        private readonly ReferenceData data;

        public Reference(ReferenceData value)
        {
            this.data = value;
        }

        /// <summary> Gets a "null" reference that can be used to test if a Reference instance. </summary>
        public static Reference<T> Null => default(Reference<T>);

        /// <summary> Gets a value indicating whether reports whether this instance references a valid asset. </summary>
        /// <value> True, if this instance references a valid instance. </value>
        public bool IsCreated => this.data.Ptr != null;

        /// <summary> Gets a reference to the data. </summary>
        /// <remarks><para>
        /// The property is a
        /// <see href="https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/ref-returns"> reference return</see>.
        /// </para></remarks>
        /// <typeparam name="T"> The struct type stored . </typeparam>
        public ref T Value
        {
            get
            {
                this.data.ValidateNotNull();
                return ref UnsafeUtility.AsRef<T>(this.data.Ptr);
            }
        }

        /// <summary> Two References are equal when they reference the same data. </summary>
        /// <param name="lhs">The Reference on the left side of the operator.</param>
        /// <param name="rhs">The Reference on the right side of the operator.</param>
        /// <returns>True, if both references point to the same data or if both are <see cref="Null"/>.</returns>
        public static bool operator ==(Reference<T> lhs, Reference<T> rhs)
        {
            return lhs.data.Ptr == rhs.data.Ptr;
        }

        /// <summary> Two References are not equal unless they reference the same data. </summary>
        /// <param name="lhs">The Reference on the left side of the operator.</param>
        /// <param name="rhs">The Reference on the right side of the operator.</param>
        /// <returns>True, if the references point to different data in memory or if one is <see cref="Null"/>.</returns>
        public static bool operator !=(Reference<T> lhs, Reference<T> rhs)
        {
            return lhs.data.Ptr != rhs.data.Ptr;
        }

        /// <summary> Creates a asset from a pointer to data and a specified size. </summary>
        /// <remarks><para>The asset is created in unmanaged memory. This function can only be used in an <see cref="Unsafe"/> context.</para></remarks>
        /// <param name="ptr">A pointer to the Buffer containing the data to store in the asset.</param>
        /// <param name="length">The length of the Buffer in bytes.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A reference to newly created asset.</returns>
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

        /// <summary> Creates a reference to data of a specified size. </summary>
        /// <remarks><para>The asset is created in unmanaged memory. This function can only be used in an <see cref="Unsafe"/> context.</para></remarks>
        /// <param name="headerPtr">A pointer to the header to store in the asset.</param>
        /// <param name="headerLength">The length of the header in bytes.</param>
        /// <param name="dataPtr">A pointer to the data to store in the asset. This will stored right after the header.</param>
        /// <param name="dataLength">The length of the data in bytes.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A reference to newly created blob asset.</returns>
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

        /// <summary> Creates a blob asset from a byte array. </summary>
        /// <remarks><para>The blob asset is created in unmanaged memory. This function can only be used in an <see cref="Unsafe"/> context.</para></remarks>
        /// <param name="data">The byte array containing the data to store in the blob asset.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A reference to newly created blob asset.</returns>
        /// <seealso cref="BlobBuilder"/>
        public static Reference<T> Create(byte[] data, MemoryAllocator allocator)
        {
            fixed (byte* ptr = &data[0])
            {
                return Create(ptr, data.Length, allocator);
            }
        }

        /// <summary> Creates a blob asset from an instance of a struct. </summary>
        /// <remarks> <para>
        /// The struct must only contain blittable fields (primitive types, fixed-length arrays, or other structs
        /// meeting these same criteria). The blob asset is created in unmanaged memory. This function can only be used in an <see cref="Unsafe"/> context.
        /// </para> </remarks>
        /// <param name="value"> An instance of <typeparamref name="T"/>. </param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns> A reference to newly created blob asset. </returns>
        /// <seealso cref="BlobBuilder"/>
        public static Reference<T> Create(T value, MemoryAllocator allocator)
        {
            return Create(UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<T>(), allocator);
        }

        /// <summary> Provides an unsafe pointer to the blob asset data. </summary>
        /// <remarks> <para>You can only use unsafe pointers in <see cref="Unsafe"/> contexts.</para> </remarks>
        /// <returns> An unsafe pointer. The pointer is null for invalid Reference instances. </returns>
        public void* GetUnsafePtr()
        {
            this.data.ValidateAllowNull();
            return this.data.Ptr;
        }

        public ReferenceData ReferenceData => this.data;

        // /// <summary> Destroys the referenced blob asset and frees its memory. </summary>
        // /// <exception cref="InvalidOperationException">Thrown if you attempt to dispose a blob asset that loaded as
        // /// part of a scene or subscene.</exception>
        // public void Dispose()
        // {
        //     this.data.Dispose();
        // }

        /// <summary> Two References are equal when they reference the same data. </summary>
        /// <param name="other">The reference to compare to this one.</param>
        /// <returns>True, if both references point to the same data or if both are <see cref="Null"/>.</returns>
        public bool Equals(Reference<T> other)
        {
            return this.data.Equals(other.data);
        }

        /// <summary> Two References are equal when they reference the same data. </summary>
        /// <param name="obj"> The object to compare to this reference. </param>
        /// <returns>True, if the object is a Reference instance that references to the same data as this one,
        /// or if both objects are <see cref="Null"/> Reference instances.</returns>
        public override bool Equals(object obj)
        {
            return this == (Reference<T>)obj;
        }

        /// <summary>
        /// Generates the hash code for this object.
        /// </summary>
        /// <returns>A standard C# value-type hash code.</returns>
        public override int GetHashCode()
        {
            return this.data.GetHashCode();
        }

        /// <summary> Construct a Reference from the blob data. </summary>
        /// <param name="blobData"> The blob data to attach to the returned object. </param>
        /// <returns> The created Reference. </returns>
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
        /// This field overlaps ptr similar to a C union.
        /// It is an internal (so we can initialize the struct) field which
        /// is here to force the alignment of ReferenceData to be 8-bytes.
        /// </summary>
        [FieldOffset(0)]
        internal long Align8Union;

        internal ReferenceHeader* Header
        {
            get { return ((ReferenceHeader*)this.Ptr) - 1; }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateNotNull()
        {
            if (this.Ptr == null)
            {
                throw new InvalidOperationException("The BlobAssetReference is null.");
            }

            this.ValidateNonBurst();
            this.ValidateBurst();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateAllowNull()
        {
            if (this.Ptr == null)
            {
                return;
            }

            this.ValidateNonBurst();
            this.ValidateBurst();
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

            if (validationPtr != this.Ptr)
            {
                throw new InvalidOperationException("The Reference is not valid. Likely it has already been unloaded or released.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidateBurst()
        {
            void* validationPtr = Header->ValidationPtr;
            if (validationPtr != this.Ptr)
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
