// <copyright file="ObjectWindowServiceTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Windows
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.Windows.Base;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class ObjectWindowServiceTests
    {
        [Test]
        public void CreateSerializableItems_CopiesRequestedRowsWithoutIconLookup()
        {
            var firstTimestamp = new DateTime(2026, 5, 7, 1, 2, 3, DateTimeKind.Utc);
            var secondTimestamp = new DateTime(2026, 5, 7, 4, 5, 6, DateTimeKind.Utc);
            var items = new List<TestObjectItem>
            {
                new((Object)null, "Skipped", nameof(ObjectWindowTestAsset), "Assets/Skipped.asset", default, firstTimestamp),
                new((Object)null, "First", nameof(ObjectWindowTestAsset), "Assets/First.asset", default, firstTimestamp),
                new((Object)null, "Second", nameof(ObjectWindowTestAsset), "Assets/Second.asset", default, secondTimestamp),
            };

            var rows = TestObjectService.Serialize(items, 1, 2);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("First", rows[0].Name);
            Assert.AreEqual(nameof(ObjectWindowTestAsset), rows[0].TypeName);
            Assert.AreEqual("Assets/First.asset", rows[0].AssetPath);
            Assert.AreEqual(firstTimestamp.ToBinary(), rows[0].Timestamp);
            Assert.AreEqual(items[1].GlobalId.ToString(), rows[0].GlobalIdString);
            Assert.AreEqual(string.Empty, rows[0].Icon);
            Assert.IsTrue(rows[0].WasConfigured);

            Assert.AreEqual("Second", rows[1].Name);
            Assert.AreEqual(secondTimestamp.ToBinary(), rows[1].Timestamp);
            Assert.IsTrue(rows[1].WasConfigured);
        }

        [Test]
        public void LoadedObjectLookup_DoesNotBindInvalidGlobalIdByPath()
        {
            var item = new TestSerializableItem
            {
                Name = "InvalidGlobalId",
                TypeName = nameof(ObjectWindowTestAsset),
                AssetPath = "Assets/InvalidGlobalId.asset",
                Timestamp = DateTime.Now.ToBinary(),
                GlobalIdString = string.Empty,
            };

            var actual = TestObjectService.TryGetLoadedObject(item, out var parsedId);

            Assert.AreEqual(default(GlobalObjectId), parsedId);
            Assert.IsNull(actual);
        }

        private sealed class TestObjectItem : BaseObjectItem
        {
            public TestObjectItem(Object obj, string name, string typeName, string assetPath, GlobalObjectId globalObjectId, DateTime timestamp)
                : base(obj, name, typeName, assetPath, globalObjectId, null, timestamp)
            {
            }
        }

        private sealed class TestObjectService : BaseObjectService<TestObjectItem, TestPreferences>
        {
            private TestObjectService()
                : base("BovineLabs.Core.Tests.ObjectWindowServiceTests")
            {
            }

            public override IReadOnlyList<TestObjectItem> Items => Array.Empty<TestObjectItem>();

            public static List<TestSerializableItem> Serialize(IReadOnlyList<TestObjectItem> items, int startIndex, int count)
            {
                return CreateSerializableItems<TestObjectItem, TestSerializableItem>(items, startIndex, count, (_, serializableItem) =>
                {
                    serializableItem.WasConfigured = true;
                });
            }

            public static Object TryGetLoadedObject(SerializableObjectItem item, out GlobalObjectId objectId)
            {
                var lookup = new LoadedObjectLookup();
                return lookup.TryGetObject(item, out objectId);
            }

            protected override bool TryRemoveItem(TestObjectItem item)
            {
                return false;
            }

            protected override void Save()
            {
            }

            protected override void Load()
            {
            }
        }

        private sealed class TestPreferences : BaseDisplayPreferences
        {
            public override string[] GetSearchKeywords()
            {
                return Array.Empty<string>();
            }
        }

        private sealed class TestSerializableItem : SerializableObjectItem
        {
            public bool WasConfigured;
        }
    }

    internal sealed class ObjectWindowTestAsset : ScriptableObject
    {
    }

}
