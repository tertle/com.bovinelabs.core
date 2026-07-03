// <copyright file="UnmanagedLocalizedReferenceTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_LOCALIZATION
namespace BovineLabs.Core.Tests.Utility
{
    using System;
    using BovineLabs.Core.Localization;
    using NUnit.Framework;
    using Unity.Entities;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Tables;

    public class UnmanagedLocalizedReferenceTests
    {
        [Test]
        public void StableLocalizedString_RoundTripsThroughUnmanagedReference()
        {
            var tableGuid = new Guid("01234567-89ab-cdef-0123-456789abcdef");
            const long EntryId = 42;
            var localizedString = new LocalizedString(tableGuid, EntryId);

            Assert.IsTrue(UnmanagedLocalizedReference.TryCreate(localizedString, out var reference));
            Assert.IsTrue(reference.IsValid);
            Assert.AreEqual(new Hash128(tableGuid.ToString("N")), reference.TableReference);
            Assert.AreEqual(EntryId, reference.EntryReference);

            var roundTrip = reference.AsLocalizedString();
            Assert.AreEqual(TableReference.Type.Guid, roundTrip.TableReference.ReferenceType);
            Assert.AreEqual(tableGuid, roundTrip.TableReference.TableCollectionNameGuid);
            Assert.AreEqual(TableEntryReference.Type.Id, roundTrip.TableEntryReference.ReferenceType);
            Assert.AreEqual(EntryId, roundTrip.TableEntryReference.KeyId);
        }

        [Test]
        public void NameBasedLocalizedString_CannotBecomeStableUnmanagedReference()
        {
            var localizedString = new LocalizedString("Dialogue", "line.intro");

            Assert.IsFalse(UnmanagedLocalizedReference.TryCreate(localizedString, out var reference));
            Assert.IsFalse(reference.IsValid);
        }

        [Test]
        public void DefaultAndZeroEntryReferences_AreInvalid()
        {
            var tableGuid = new Guid("01234567-89ab-cdef-0123-456789abcdef");

            Assert.IsFalse(default(UnmanagedLocalizedReference).IsValid);
            Assert.IsFalse(new UnmanagedLocalizedReference(tableGuid, 0).IsValid);
        }

    }
}
#endif
