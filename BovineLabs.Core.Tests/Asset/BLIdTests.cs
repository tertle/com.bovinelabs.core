// <copyright file="BLIdTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Asset
{
    using System;
    using BovineLabs.Core.Asset;
#if BL_CORE_EXTENSIONS && !BL_DISABLE_OBJECT_DEFINITION
    using BovineLabs.Core.ObjectManagement;
#endif
    using NUnit.Framework;

    public class BLIdTests
    {
        [Test]
        public void BLId_PacksLocalAndModIds()
        {
            var id = new BLId(42, 7);
            var maxModId = new BLId(42, (ushort)(BLId.MaxModsIds - 1));

            Assert.AreEqual(42, id.ID);
            Assert.AreEqual(7, id.Mod);
            Assert.IsFalse(id.IsNull);
            Assert.AreEqual(id, new BLId(42, 7));
            Assert.AreNotEqual(id, new BLId(42, 8));
            Assert.AreEqual(BLId.MaxModsIds - 1, maxModId.Mod);
            Assert.AreEqual(42, maxModId.ID);
        }

        [Test]
        public void BLId_ZeroLocalIdIsNull()
        {
            var id = new BLId(0, 7);

            Assert.AreEqual(BLId.Null, id);
            Assert.AreEqual(0, id.RawValue);
            Assert.AreEqual(0, id.ID);
            Assert.AreEqual(0, id.Mod);
            Assert.IsTrue(id.IsNull);
            Assert.AreEqual(BLId.Null, BLId.Null.WithMod(7));
        }

        [Test]
        public void BLId_RejectsOutOfRangeValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BLId(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BLId(BLId.MaxLocalId + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BLId(1, (ushort)BLId.MaxModsIds));
        }

#if BL_CORE_EXTENSIONS && !BL_DISABLE_OBJECT_DEFINITION
        [Test]
        public void ObjectId_PacksLocalAndModIds()
        {
            var id = new ObjectId(42, 7);
            var fromBLId = (ObjectId)new BLId(43, 8);

            Assert.AreEqual(42, id.ID);
            Assert.AreEqual(7, id.Mod);
            Assert.AreEqual(new BLId(42, 7), (BLId)id);
            Assert.AreEqual(43, fromBLId.ID);
            Assert.AreEqual(8, fromBLId.Mod);
            Assert.AreEqual(new ObjectId(43, 8), fromBLId);
        }
#endif
    }
}
