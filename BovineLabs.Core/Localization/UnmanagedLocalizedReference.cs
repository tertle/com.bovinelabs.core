#if UNITY_LOCALIZATION
namespace BovineLabs.Core.Localization
{
    using System;
    using Unity.Entities;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Tables;
    using LocalizationTableEntryReference = UnityEngine.Localization.Tables.TableEntryReference;
    using LocalizationTableReference = UnityEngine.Localization.Tables.TableReference;

    public struct UnmanagedLocalizedReference : IEquatable<UnmanagedLocalizedReference>
    {
        public Hash128 TableReference;

        public long EntryReference;

        public UnmanagedLocalizedReference(Hash128 tableReference, long entryReference)
        {
            this.TableReference = tableReference;
            this.EntryReference = entryReference;
        }

        public UnmanagedLocalizedReference(Guid tableReference, long entryReference)
            : this(ToHash128(tableReference), entryReference)
        {
        }

        public readonly bool IsValid => this.TableReference.IsValid && this.EntryReference != SharedTableData.EmptyId;

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

        /// <summary>
        /// Returns default if the reference cannot be represented using stable IDs.
        /// </summary>
        public static UnmanagedLocalizedReference From(LocalizedReference reference)
        {
            return TryCreate(reference, out var localizedReference) ? localizedReference : default;
        }

        /// <summary>
        /// Returns default if the reference cannot be represented using stable IDs.
        /// </summary>
        public static implicit operator UnmanagedLocalizedReference(LocalizedReference reference)
        {
            return From(reference);
        }

        public readonly LocalizedString AsLocalizedString()
        {
            return this.As(new LocalizedString());
        }

        public readonly T As<T>()
            where T : LocalizedReference, new()
        {
            return this.As(new T());
        }

        public readonly T As<T>(T reference)
            where T : LocalizedReference
        {
            reference.SetReference(this.ToTableReference(), this.EntryReference);
            return reference;
        }

        public readonly LocalizationTableReference ToTableReference()
        {
            return this.TableReference.IsValid ? this.ToGuid() : default;
        }

        public readonly LocalizationTableEntryReference ToTableEntryReference()
        {
            return this.EntryReference;
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
            return this.TableReference.Equals(other.TableReference) && this.EntryReference == other.EntryReference;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is UnmanagedLocalizedReference other && this.Equals(other);
        }

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
