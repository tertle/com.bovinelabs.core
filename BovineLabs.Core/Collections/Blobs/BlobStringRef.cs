// <copyright file="BlobStringRef.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary>
    /// Non-owning transient reference to a <see cref="BlobString" /> stored inside a live blob asset.
    /// </summary>
    /// <remarks>
    /// This wrapper hides the raw pointer from public APIs. It does not own or retain the backing blob asset, so consumers must only use it while the
    /// owning blob asset is alive and should convert it to owned data before storing it long term.
    /// </remarks>
    public unsafe struct BlobStringRef
    {
        private readonly BlobString* value;

        private BlobStringRef(BlobString* value)
        {
            this.value = value;
        }

        /// <summary> Gets a value indicating whether this reference points at a blob string. </summary>
        public readonly bool IsCreated => this.value != null;

        /// <summary> Gets the UTF-8 byte length of the referenced blob string, excluding the null terminator. </summary>
        public readonly int Length => this.value == null ? 0 : this.value->Length;

        /// <summary> Creates a transient reference to a blob string. </summary>
        /// <param name="value"> The blob string to reference. The owning blob asset must outlive the returned reference. </param>
        /// <returns> A transient non-owning reference to <paramref name="value" />. </returns>
        public static BlobStringRef From(ref BlobString value)
        {
            return new BlobStringRef((BlobString*)UnsafeUtility.AddressOf(ref value));
        }

        /// <summary> Converts the referenced blob string to a managed string. </summary>
        /// <returns> The managed string, or an empty string when this reference is default. </returns>
        public readonly string GetString()
        {
            return this.value == null ? string.Empty : this.value->ToString();
        }

        /// <summary> Copies the referenced UTF-8 bytes to a native byte list. </summary>
        /// <typeparam name="T"> The destination native list type. </typeparam>
        /// <param name="destination"> The destination list. </param>
        /// <returns>
        /// The conversion result from <see cref="BlobString.CopyTo{T}" />, or <see cref="ConversionError.None" /> when this reference is default or empty.
        /// </returns>
        public readonly ConversionError CopyTo<T>(ref T destination)
            where T : INativeList<byte>
        {
            if (this.value == null || this.value->Length == 0)
            {
                destination.Length = 0;
                return ConversionError.None;
            }

            return this.value->CopyTo(ref destination);
        }
    }
}
