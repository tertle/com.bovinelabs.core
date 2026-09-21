#if UNITY_LOCALIZATION
namespace BovineLabs.Core.Localization
{
    using System;
    using Unity.Entities;
    using UnityEngine;
    using Hash128 = Unity.Entities.Hash128;
    using Unity.Localization;
    using LocalizationTableEntryReference = Unity.Localization.TableEntryReference;
    using LocalizationTableReference = Unity.Localization.TableReference;

    public struct UnmanagedLocalizedReference : IEquatable<UnmanagedLocalizedReference>
    {
        public Hash128 TableReference;

        public long EntryReference;

        public UnmanagedLocalizedReference(Hash128 tableReference, long entryReference)
        {
            TableReference = tableReference;
            EntryReference = entryReference;
        }

        public UnmanagedLocalizedReference(GUID tableReference, long entryReference)
            : this(new Hash128(tableReference.ToString()), entryReference)
        {
        }

        public readonly bool IsValid => TableReference.IsValid && EntryReference != 0;

        public static bool TryCreate<TEntry>(LocalizedEntry<TEntry> reference, out UnmanagedLocalizedReference localizedReference)
            where TEntry : class, IResourceEntry
        {
            localizedReference = default;
            if (reference == null || reference.IsEmpty)
            {
                return false;
            }

            var table = reference.TableReference;
            var entry = reference.TableEntryReference;
            if (table.ReferenceType != LocalizationTableReference.Type.Guid || table.TableCollectionNameGuid.Empty())
            {
                return false;
            }

            if (entry.ReferenceType != LocalizationTableEntryReference.Type.Id || entry.KeyId == 0)
            {
                return false;
            }

            localizedReference = new UnmanagedLocalizedReference(table.TableCollectionNameGuid, entry.KeyId);
            return true;
        }

        /// <summary>
        /// Returns default if the reference cannot be represented using stable IDs.
        /// </summary>
        public static UnmanagedLocalizedReference From<TEntry>(LocalizedEntry<TEntry> reference)
            where TEntry : class, IResourceEntry
        {
            return TryCreate(reference, out var localizedReference) ? localizedReference : default;
        }

        /// <summary>
        /// Returns default if the reference cannot be represented using stable IDs.
        /// </summary>
        public static implicit operator UnmanagedLocalizedReference(LocalizedString reference)
        {
            return From(reference);
        }

        public readonly LocalizedString AsLocalizedString()
        {
            var reference = new LocalizedString();
            reference.SetReference(ToTableReference(), EntryReference);
            return reference;
        }

        public readonly LocalizationTableReference ToTableReference()
        {
            return TableReference.IsValid ? LocalizationTableReference.FromGuid(TableReference.ToString()) : default;
        }

        public readonly LocalizationTableEntryReference ToTableEntryReference()
        {
            return EntryReference;
        }

        public static bool operator ==(UnmanagedLocalizedReference left, UnmanagedLocalizedReference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnmanagedLocalizedReference left, UnmanagedLocalizedReference right)
        {
            return !left.Equals(right);
        }

        public readonly bool Equals(UnmanagedLocalizedReference other)
        {
            return TableReference.Equals(other.TableReference) && EntryReference == other.EntryReference;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is UnmanagedLocalizedReference other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            unchecked
            {
                return (TableReference.GetHashCode() * 397) ^ EntryReference.GetHashCode();
            }
        }

    }
}
#endif
