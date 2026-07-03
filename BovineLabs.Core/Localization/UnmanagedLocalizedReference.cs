// <copyright file="UnmanagedLocalizedReference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_LOCALIZATION
namespace BovineLabs.Core.Localization
{
    using System;
    using Unity.Entities;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Tables;
    using LocalizationTableEntryReference = UnityEngine.Localization.Tables.TableEntryReference;
    using LocalizationTableReference = UnityEngine.Localization.Tables.TableReference;

    /// <summary> Unmanaged reference to a Unity Localization table entry. </summary>
    public struct UnmanagedLocalizedReference : IEquatable<UnmanagedLocalizedReference>
    {
        /// <summary> Stable table collection guid. </summary>
        public Hash128 TableReference;

        /// <summary> Stable shared table entry id. </summary>
        public long EntryReference;

        /// <summary> Initializes a new instance of the <see cref="UnmanagedLocalizedReference" /> struct. </summary>
        /// <param name="tableReference">Stable table collection guid.</param>
        /// <param name="entryReference">Stable shared table entry id.</param>
        public UnmanagedLocalizedReference(Hash128 tableReference, long entryReference)
        {
            this.TableReference = tableReference;
            this.EntryReference = entryReference;
        }

        /// <summary> Initializes a new instance of the <see cref="UnmanagedLocalizedReference" /> struct. </summary>
        /// <param name="tableReference">Stable table collection guid.</param>
        /// <param name="entryReference">Stable shared table entry id.</param>
        public UnmanagedLocalizedReference(Guid tableReference, long entryReference)
            : this(ToHash128(tableReference), entryReference)
        {
        }

        /// <summary> Gets a value indicating whether this reference points at a non-empty table and entry id. </summary>
        public readonly bool IsValid => this.TableReference.IsValid && this.EntryReference != SharedTableData.EmptyId;

        /// <summary> Converts a Unity Localization reference into an unmanaged reference when it uses stable ids. </summary>
        /// <param name="reference">The localization reference.</param>
        /// <param name="localizedReference">The unmanaged reference.</param>
        /// <returns>True when the reference uses a table guid and entry id.</returns>
        public static bool TryCreate(LocalizedReference reference, out UnmanagedLocalizedReference localizedReference)
        {
            localizedReference = default;
            if (reference == null || reference.IsEmpty)
            {
                return false;
            }

            var table = reference.TableReference;
            var entry = reference.TableEntryReference;
            if (table.ReferenceType != LocalizationTableReference.Type.Guid || table.TableCollectionNameGuid == Guid.Empty)
            {
                return false;
            }

            if (entry.ReferenceType != LocalizationTableEntryReference.Type.Id || entry.KeyId == SharedTableData.EmptyId)
            {
                return false;
            }

            localizedReference = new UnmanagedLocalizedReference(table.TableCollectionNameGuid, entry.KeyId);
            return true;
        }

        /// <summary> Converts a Unity Localization reference into an unmanaged reference, or default when it cannot be represented. </summary>
        /// <param name="reference">The localization reference.</param>
        /// <returns>The unmanaged reference.</returns>
        public static UnmanagedLocalizedReference From(LocalizedReference reference)
        {
            return TryCreate(reference, out var localizedReference) ? localizedReference : default;
        }

        /// <summary> Converts a Unity Localization reference into an unmanaged reference, or default when it cannot be represented. </summary>
        /// <param name="reference">The localization reference.</param>
        /// <returns>The unmanaged reference.</returns>
        public static implicit operator UnmanagedLocalizedReference(LocalizedReference reference)
        {
            return From(reference);
        }

        /// <summary> Creates a managed localized string reference from this unmanaged reference. </summary>
        /// <returns>A managed localized string reference.</returns>
        public readonly LocalizedString AsLocalizedString()
        {
            return this.As(new LocalizedString());
        }

        /// <summary> Applies this unmanaged reference to a managed Unity Localization reference. </summary>
        /// <typeparam name="T">The managed localization reference type.</typeparam>
        /// <returns>A managed localization reference.</returns>
        public readonly T As<T>()
            where T : LocalizedReference, new()
        {
            return this.As(new T());
        }

        /// <summary> Applies this unmanaged reference to a managed Unity Localization reference. </summary>
        /// <typeparam name="T">The managed localization reference type.</typeparam>
        /// <param name="reference">The managed reference to update.</param>
        /// <returns>The updated managed localization reference.</returns>
        public readonly T As<T>(T reference)
            where T : LocalizedReference
        {
            reference.SetReference(this.ToTableReference(), this.EntryReference);
            return reference;
        }

        /// <summary> Converts this reference into a Unity Localization table reference. </summary>
        /// <returns>The table reference.</returns>
        public readonly LocalizationTableReference ToTableReference()
        {
            return this.TableReference.IsValid ? this.ToGuid() : default;
        }

        /// <summary> Converts this reference into a Unity Localization table entry reference. </summary>
        /// <returns>The table entry reference.</returns>
        public readonly LocalizationTableEntryReference ToTableEntryReference()
        {
            return this.EntryReference;
        }

        /// <summary> Checks whether two references are equal. </summary>
        /// <param name="left">The left reference.</param>
        /// <param name="right">The right reference.</param>
        /// <returns>True if the references are equal.</returns>
        public static bool operator ==(UnmanagedLocalizedReference left, UnmanagedLocalizedReference right)
        {
            return left.Equals(right);
        }

        /// <summary> Checks whether two references are not equal. </summary>
        /// <param name="left">The left reference.</param>
        /// <param name="right">The right reference.</param>
        /// <returns>True if the references are not equal.</returns>
        public static bool operator !=(UnmanagedLocalizedReference left, UnmanagedLocalizedReference right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public readonly bool Equals(UnmanagedLocalizedReference other)
        {
            return this.TableReference.Equals(other.TableReference) && this.EntryReference == other.EntryReference;
        }

        /// <inheritdoc />
        public override readonly bool Equals(object obj)
        {
            return obj is UnmanagedLocalizedReference other && this.Equals(other);
        }

        /// <inheritdoc />
        public override readonly int GetHashCode()
        {
            unchecked
            {
                return (this.TableReference.GetHashCode() * 397) ^ this.EntryReference.GetHashCode();
            }
        }

        private static Hash128 ToHash128(Guid guid)
        {
            return guid == Guid.Empty ? default : new Hash128(guid.ToString("N"));
        }

        private readonly Guid ToGuid()
        {
            return Guid.ParseExact(this.TableReference.ToString(), "N");
        }
    }
}
#endif
