// <copyright file="Reference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Entities.Serialization;
    using Unity.Mathematics;

    /// <summary> Implements a reference type, based off <see cref="BlobAssetReference{T}"/>. </summary>
    /// <typeparam name="T"> The type to hold. </typeparam>
    public unsafe struct Reference<T> : IDisposable, IEquatable<Reference<T>>
        where T : struct
    {
        private ReferenceData data;

        /// <summary>
        /// Gets a "null" blob asset reference that can be used to test if a Reference instance.
        /// </summary>
        public static Reference<T> Null => default(Reference<T>);

        /// <summary> Gets a value indicating whether reports whether this instance references a valid blob asset. </summary>
        /// <value> True, if this instance references a valid blob instance. </value>
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

        /// <summary> Creates a blob asset from a pointer to data and a specified size. </summary>
        /// <remarks><para>The blob asset is created in unmanaged memory. Call <see cref="Dispose"/> to free the asset memory
        /// when it is no longer needed. This function can only be used in an <see cref="Unsafe"/> context.</para></remarks>
        /// <param name="ptr">A pointer to the buffer containing the data to store in the blob asset.</param>
        /// <param name="length">The length of the buffer in bytes.</param>
        /// <returns>A reference to newly created blob asset.</returns>
        /// <seealso cref="BlobBuilder"/>
        public static Reference<T> Create(void* ptr, int length)
        {
            byte* buffer =
                (byte*)UnsafeUtility.Malloc(sizeof(ReferenceHeader) + length, 16, Allocator.Persistent);
            UnsafeUtility.MemCpy(buffer + sizeof(ReferenceHeader), ptr, length);

            ReferenceHeader* header = (ReferenceHeader*)buffer;
            *header = default(ReferenceHeader);

            header->Length = length;
            header->Allocator = Allocator.Persistent;

            // @TODO use 64bit hash
            header->Hash = math.hash(ptr, length);

            Reference<T> reference;
            reference.data.Align8Union = 0;
            header->ValidationPtr = reference.data.Ptr = buffer + sizeof(ReferenceHeader);
            return reference;
        }

        /// <summary> Creates a blob asset from a byte array. </summary>
        /// <remarks><para>The blob asset is created in unmanaged memory. Call <see cref="Dispose"/> to free the asset memory
        /// when it is no longer needed. This function can only be used in an <see cref="Unsafe"/> context.</para></remarks>
        /// <param name="data">The byte array containing the data to store in the blob asset.</param>
        /// <returns>A reference to newly created blob asset.</returns>
        /// <seealso cref="BlobBuilder"/>
        public static Reference<T> Create(byte[] data)
        {
            fixed (byte* ptr = &data[0])
            {
                return Create(ptr, data.Length);
            }
        }

        /// <summary> Creates a blob asset from an instance of a struct. </summary>
        /// <remarks> <para>The struct must only contain blittable fields (primitive types, fixed-length arrays, or other structs
        /// meeting these same criteria). The blob asset is created in unmanaged memory. Call <see cref="Dispose"/> to
        /// free the asset memory when it is no longer needed. This function can only be used in an <see cref="Unsafe"/>
        /// context.</para> </remarks>
        /// <param name="value"> An instance of <typeparamref name="T"/>. </param>
        /// <returns> A reference to newly created blob asset. </returns>
        /// <seealso cref="BlobBuilder"/>
        public static Reference<T> Create(T value)
        {
            return Create(UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<T>());
        }

        /// <summary> Provides an unsafe pointer to the blob asset data. </summary>
        /// <remarks> <para>You can only use unsafe pointers in <see cref="Unsafe"/> contexts.</para> </remarks>
        /// <returns> An unsafe pointer. The pointer is null for invalid Reference instances. </returns>
        public void* GetUnsafePtr()
        {
            this.data.ValidateAllowNull();
            return this.data.Ptr;
        }

        /// <summary> Destroys the referenced blob asset and frees its memory. </summary>
        /// <exception cref="InvalidOperationException">Thrown if you attempt to dispose a blob asset that loaded as
        /// part of a scene or subscene.</exception>
        public void Dispose()
        {
            this.data.Dispose();
        }

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
            return new Reference<T> { data = blobData };
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal unsafe struct ReferenceData
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

        internal void Dispose()
        {
            this.ValidateNotNull();
            var header = this.Header;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (header->Allocator == Allocator.None)
            {
                throw new InvalidOperationException(
                    "It's not possible to release a blob asset reference that was deserialized. It will be automatically released when the scene is unloaded ");
            }

            Header->Invalidate();
#endif

            UnsafeUtility.Free(header, header->Allocator);
            this.Ptr = null;
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

    // TODO: For now the size of ReferenceHeader needs to be multiple of 16 to ensure alignment of blob assets
    // TODO: Add proper alignment support to blob assets
    // TODO: Reduce the size of the header at runtime or remove it completely
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal unsafe struct ReferenceHeader
    {
        [FieldOffset(0)]
        public void* ValidationPtr;

        [FieldOffset(8)]
        public int Length;

        [FieldOffset(12)]
        public Allocator Allocator;

        [FieldOffset(16)]
        public ulong Hash;

        [FieldOffset(24)]
        private ulong padding;

        internal static ReferenceHeader CreateForSerialize(int length, ulong hash)
        {
            return new ReferenceHeader
            {
                ValidationPtr = null,
                Length = length,
                Allocator = Allocator.None,
                Hash = hash,
                padding = 0,
            };
        }

        internal void Invalidate()
        {
            this.ValidationPtr = (void*)0xdddddddddddddddd;
        }
    }
}