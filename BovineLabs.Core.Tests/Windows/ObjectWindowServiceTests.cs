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
        private string testRoot;

        [SetUp]
        public void Setup()
        {
            var folderName = $"__CoreObjectWindowTests_{Guid.NewGuid():N}";
            AssetDatabase.CreateFolder("Assets", folderName);
            this.testRoot = $"Assets/{folderName}";
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(this.testRoot))
            {
                AssetDatabase.DeleteAsset(this.testRoot);
                AssetDatabase.SaveAssets();
            }
        }

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
        public void GetObject_ReloadsAssetByPath_WhenGlobalIdIsMissing()
        {
            var asset = this.CreateAsset<ObjectWindowTestAsset>("PathFallback");
            var item = new TestObjectItem(null, asset.name, nameof(ObjectWindowTestAsset), AssetDatabase.GetAssetPath(asset), default, DateTime.Now);

            var reloaded = item.GetObject();

            AssertSameUnityObject(asset, reloaded);
        }

        [Test]
        public void GetObject_DoesNotReloadAssetByPath_WhenGlobalIdIsPresent()
        {
            var original = this.CreateAsset<ObjectWindowTestAsset>("Original");
            var originalId = GlobalObjectId.GetGlobalObjectIdSlow(original);
            var originalPath = AssetDatabase.GetAssetPath(original);
            AssetDatabase.DeleteAsset(originalPath);
            AssetDatabase.SaveAssets();

            var fallback = this.CreateAsset<ObjectWindowTestAsset>("Fallback");
            var item = new TestObjectItem(null, fallback.name, nameof(ObjectWindowTestAsset), AssetDatabase.GetAssetPath(fallback), originalId, DateTime.Now);

            Assert.IsNull(item.GetObject());
        }

        [Test]
        public void GetObject_DoesNotReloadPath_WhenTypeNameDoesNotMatch()
        {
            var asset = this.CreateAsset<ObjectWindowTestAsset>("WrongType");
            var item = new TestObjectItem(null, asset.name, nameof(ObjectWindowOtherTestAsset), AssetDatabase.GetAssetPath(asset), default, DateTime.Now);

            Assert.IsNull(item.GetObject());
        }

        [Test]
        public void LoadedObjectLookup_ResolvesSubAssetsByGlobalObjectId()
        {
            var path = $"{this.testRoot}/SubAssetContainer.asset";
            var main = ScriptableObject.CreateInstance<ObjectWindowTestAsset>();
            main.name = "Main";
            var firstSubAsset = ScriptableObject.CreateInstance<ObjectWindowSubTestAsset>();
            firstSubAsset.name = "FirstSubAsset";
            var secondSubAsset = ScriptableObject.CreateInstance<ObjectWindowSubTestAsset>();
            secondSubAsset.name = "SecondSubAsset";

            AssetDatabase.CreateAsset(main, path);
            AssetDatabase.AddObjectToAsset(firstSubAsset, path);
            AssetDatabase.AddObjectToAsset(secondSubAsset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);

            var expected = FindLoadedAsset<ObjectWindowSubTestAsset>(path, "SecondSubAsset");
            var expectedId = GlobalObjectId.GetGlobalObjectIdSlow(expected);
            var item = new TestSerializableItem
            {
                Name = expected.name,
                TypeName = nameof(ObjectWindowSubTestAsset),
                AssetPath = path,
                Timestamp = DateTime.Now.ToBinary(),
                GlobalIdString = expectedId.ToString(),
            };

            var actual = TestObjectService.TryGetLoadedObject(item, out var parsedId);

            Assert.AreEqual(expectedId, parsedId);
            AssertSameUnityObject(expected, actual);
        }

        [Test]
        public void LoadedObjectLookup_DoesNotBindInvalidGlobalIdByPath()
        {
            var asset = this.CreateAsset<ObjectWindowTestAsset>("InvalidGlobalId");
            var item = new TestSerializableItem
            {
                Name = asset.name,
                TypeName = nameof(ObjectWindowTestAsset),
                AssetPath = AssetDatabase.GetAssetPath(asset),
                Timestamp = DateTime.Now.ToBinary(),
                GlobalIdString = string.Empty,
            };

            var actual = TestObjectService.TryGetLoadedObject(item, out var parsedId);

            Assert.AreEqual(default(GlobalObjectId), parsedId);
            Assert.IsNull(actual);
        }

        private T CreateAsset<T>(string name)
            where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            var path = $"{this.testRoot}/{name}.asset";

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static T FindLoadedAsset<T>(string path, string name)
            where T : Object
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is T typedAsset && asset.name == name)
                {
                    return typedAsset;
                }
            }

            Assert.Fail($"Could not find asset '{name}' at '{path}'");
            return null;
        }

        private static void AssertSameUnityObject(Object expected, Object actual)
        {
            Assert.IsNotNull(actual);
            Assert.IsTrue(expected == actual);
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

    internal sealed class ObjectWindowOtherTestAsset : ScriptableObject
    {
    }

    internal sealed class ObjectWindowSubTestAsset : ScriptableObject
    {
    }
}
