// <copyright file="ObjectManagementProcessorTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.ObjectManagement
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using BovineLabs.Core.Editor.ObjectManagement;
    using BovineLabs.Core.ObjectManagement;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Object = UnityEngine.Object;

    [ObjectManagementImportExtension("dialogue")]
    public class ObjectManagementProcessorTests
    {
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        [Test]
        public void UpdateAutoRefEntries_PreservesRetainedEntryDataAndCreatesDefaults()
        {
            var manager = ScriptableObject.CreateInstance<EntryManager>();
            var firstRetained = ScriptableObject.CreateInstance<EntryAsset>();
            var secondRetained = ScriptableObject.CreateInstance<EntryAsset>();
            var removed = ScriptableObject.CreateInstance<EntryAsset>();
            var added = ScriptableObject.CreateInstance<EntryAsset>();

            try
            {
                manager.SetEntries(new[]
                {
                    new EntryManager.Entry(firstRetained, 7),
                    new EntryManager.Entry(secondRetained, 8),
                    new EntryManager.Entry(removed, 9),
                });

                var attribute = typeof(EntryAsset).GetCustomAttribute<AutoRefAttribute>();
                var result = Invoke<bool>(
                    "UpdateAutoRefEntries",
                    manager,
                    attribute,
                    typeof(EntryAsset),
                    new List<Object> { added, secondRetained, firstRetained });

                Assert.IsTrue(result);
                Assert.AreEqual(3, manager.Entries.Count);
                Assert.AreSame(firstRetained, manager.Entries[0].Asset);
                Assert.AreEqual(7, manager.Entries[0].Id);
                Assert.AreSame(secondRetained, manager.Entries[1].Asset);
                Assert.AreEqual(8, manager.Entries[1].Id);
                Assert.AreSame(added, manager.Entries[2].Asset);
                Assert.AreEqual(0, manager.Entries[2].Id);
            }
            finally
            {
                Object.DestroyImmediate(manager);
                Object.DestroyImmediate(firstRetained);
                Object.DestroyImmediate(secondRetained);
                Object.DestroyImmediate(removed);
                Object.DestroyImmediate(added);
            }
        }

        [Test]
        public void UpdateAutoRefEntries_RejectsMissingReferenceField()
        {
            var manager = ScriptableObject.CreateInstance<EntryManager>();
            var asset = ScriptableObject.CreateInstance<EntryAsset>();

            try
            {
                var attribute = new AutoRefAttribute(nameof(EntryManager), "entries") { ReferenceFieldName = "missing" };
                LogAssert.Expect(LogType.Error, new Regex("Property missing not found on entry type Entry for EntryManager"));
                var result = Invoke<bool>("UpdateAutoRefEntries", manager, attribute, typeof(EntryAsset), new List<Object> { asset });

                Assert.IsFalse(result);
            }
            finally
            {
                Object.DestroyImmediate(manager);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void UpdateAutoRefEntries_RejectsNonSerializedEntryCollection()
        {
            var manager = ScriptableObject.CreateInstance<EntryManager>();
            var asset = ScriptableObject.CreateInstance<EntryAsset>();

            try
            {
                var attribute = new AutoRefAttribute(nameof(EntryManager), "reflectionOnlyEntries") { ReferenceFieldName = "asset" };
                LogAssert.Expect(LogType.Error, new Regex("Property reflectionOnlyEntries not found for EntryManager"));
                var result = Invoke<bool>("UpdateAutoRefEntries", manager, attribute, typeof(EntryAsset), new List<Object> { asset });

                Assert.IsFalse(result);
            }
            finally
            {
                Object.DestroyImmediate(manager);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void UpdateAutoRefEntries_RejectsNonSerializedReferenceField()
        {
            var manager = ScriptableObject.CreateInstance<EntryManager>();
            var asset = ScriptableObject.CreateInstance<EntryAsset>();

            try
            {
                var attribute = new AutoRefAttribute(nameof(EntryManager), "nonSerializedReferenceEntries") { ReferenceFieldName = "asset" };
                LogAssert.Expect(LogType.Error, new Regex("Property asset was not serialized on entry type NonSerializedReferenceEntry for EntryManager"));
                var result = Invoke<bool>("UpdateAutoRefEntries", manager, attribute, typeof(EntryAsset), new List<Object> { asset });

                Assert.IsFalse(result);
            }
            finally
            {
                Object.DestroyImmediate(manager);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void UpdateAutoRefEntries_RejectsEntriesWithoutPublicParameterlessConstructor()
        {
            var manager = ScriptableObject.CreateInstance<EntryManager>();
            var asset = ScriptableObject.CreateInstance<EntryAsset>();

            try
            {
                var attribute = new AutoRefAttribute(nameof(EntryManager), "constructedEntries") { ReferenceFieldName = "asset" };
                LogAssert.Expect(LogType.Error, new Regex("Entry type ConstructedEntry for EntryManager couldn't be created"));
                var result = Invoke<bool>("UpdateAutoRefEntries", manager, attribute, typeof(EntryAsset), new List<Object> { asset });

                Assert.IsFalse(result);
            }
            finally
            {
                Object.DestroyImmediate(manager);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ResolveManagerType_ReturnsNullForAmbiguousSimpleNames()
        {
            LogAssert.Expect(LogType.Error, new Regex("More than one manager type found for DuplicateManager"));

            var result = Invoke<Type>("ResolveManagerType", "DuplicateManager");

            Assert.IsNull(result);
        }

        [Test]
        public void TryCallAutoRefPostProcessor_InvokesManagerHook()
        {
            var manager = ScriptableObject.CreateInstance<EntryManager>();

            try
            {
                Invoke<object>("TryCallAutoRefPostProcessor", manager, "entries");

                Assert.AreEqual("entries", manager.UpdatedFieldName);
            }
            finally
            {
                Object.DestroyImmediate(manager);
            }
        }

        [Test]
        public void CheckAutoRef_QueuesBaseAndDerivedAutoRefs()
        {
            var map = GetAutoRefMap();
            map.Clear();
            var asset = ScriptableObject.CreateInstance<DerivedAutoRefAsset>();

            try
            {
                Invoke<object>("CheckAutoRef", asset);

                Assert.AreEqual(2, map.Count);
            }
            finally
            {
                map.Clear();
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CheckAutoRef_QueuesMultipleAttributesOnSameType()
        {
            var map = GetAutoRefMap();
            map.Clear();
            var asset = ScriptableObject.CreateInstance<MultiAutoRefAsset>();

            try
            {
                Invoke<object>("CheckAutoRef", asset);

                Assert.AreEqual(2, map.Count);
            }
            finally
            {
                map.Clear();
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CheckAutoRef_IgnoresDuplicateTargetsOnSameType()
        {
            var map = GetAutoRefMap();
            map.Clear();
            var asset = ScriptableObject.CreateInstance<DuplicateAutoRefAsset>();

            try
            {
                Invoke<object>("CheckAutoRef", asset);

                Assert.AreEqual(1, map.Count);
            }
            finally
            {
                map.Clear();
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void AssetCreator_SelectsMatchingAutoRefForManagerField()
        {
            var manager = ScriptableObject.CreateInstance<AssetCreatorManager>();
            manager.name = nameof(AssetCreatorManager);
            var serializedObject = new SerializedObject(manager);

            try
            {
                var property = serializedObject.FindProperty("assets");
                var attribute = InvokeAssetCreator<AutoRefAttribute>("TryGetAttribute", serializedObject, property, typeof(AssetCreatorAsset));

                Assert.IsNotNull(attribute);
                Assert.AreEqual(nameof(AssetCreatorManager), attribute.ManagerType);
                Assert.AreEqual("assets", attribute.FieldName);
            }
            finally
            {
                Object.DestroyImmediate(manager);
            }
        }

        [Test]
        public void ShouldQueueImportedAsset_ReturnsTrueForAssetFiles()
        {
            var result = Invoke<bool>("ShouldQueueImportedAsset", "Assets/Test.asset");

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldQueueImportedAsset_ReturnsTrueForRegisteredExtensions()
        {
            var result = Invoke<bool>("ShouldQueueImportedAsset", "Assets/Test.dialogue");

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldQueueImportedAsset_ReturnsFalseForUnregisteredExtensions()
        {
            var result = Invoke<bool>("ShouldQueueImportedAsset", "Assets/Test.unregistered");

            Assert.IsFalse(result);
        }

        private static T Invoke<T>(string methodName, params object[] args)
        {
            var method = typeof(ObjectManagementProcessor).GetMethod(methodName, StaticPrivate);
            Assert.IsNotNull(method);
            return (T)method!.Invoke(null, args);
        }

        private static T InvokeAssetCreator<T>(string methodName, params object[] args)
        {
            var method = typeof(AssetCreator).GetMethod(methodName, StaticPrivate);
            Assert.IsNotNull(method);
            return (T)method!.Invoke(null, args);
        }

        private static IDictionary GetAutoRefMap()
        {
            var field = typeof(ObjectManagementProcessor).GetField("AutoRefMap", StaticPrivate);
            Assert.IsNotNull(field);
            return (IDictionary)field!.GetValue(null);
        }

        private sealed class EntryManager : ScriptableObject, IAutoRefPostProcessor
        {
            [SerializeField]
            private Entry[] entries = Array.Empty<Entry>();

            [SerializeField]
            private NonSerializedReferenceEntry[] nonSerializedReferenceEntries = Array.Empty<NonSerializedReferenceEntry>();

            [SerializeField]
            private ConstructedEntry[] constructedEntries = Array.Empty<ConstructedEntry>();

            private Entry[] reflectionOnlyEntries = Array.Empty<Entry>();

            public IReadOnlyList<Entry> Entries => this.entries;

            public string UpdatedFieldName { get; private set; }

            public void SetEntries(Entry[] value)
            {
                this.entries = value;
            }

            void IAutoRefPostProcessor.OnAutoRefUpdated(string fieldName)
            {
                this.UpdatedFieldName = fieldName;
            }

            [Serializable]
            public struct Entry
            {
                [SerializeField]
                private EntryAsset asset;

                [SerializeField]
                private int id;

                public Entry(EntryAsset asset, int id)
                {
                    this.asset = asset;
                    this.id = id;
                }

                public EntryAsset Asset => this.asset;

                public int Id => this.id;
            }
        }

        [Serializable]
        private struct NonSerializedReferenceEntry
        {
            private EntryAsset asset;
        }

        [Serializable]
        private sealed class ConstructedEntry
        {
            [SerializeField]
            private EntryAsset asset;

            public ConstructedEntry(EntryAsset asset)
            {
                this.asset = asset;
            }
        }

        [AutoRef(nameof(EntryManager), "entries", ReferenceFieldName = "asset")]
        private sealed class EntryAsset : ScriptableObject
        {
        }

        private sealed class BaseAutoRefManager : ScriptableObject
        {
        }

        private sealed class DerivedAutoRefManager : ScriptableObject
        {
        }

        [AutoRef(nameof(BaseAutoRefManager), "baseAssets")]
        private abstract class BaseAutoRefAsset : ScriptableObject
        {
        }

        [AutoRef(nameof(DerivedAutoRefManager), "derivedAssets")]
        private sealed class DerivedAutoRefAsset : BaseAutoRefAsset
        {
        }

        [AutoRef(nameof(BaseAutoRefManager), "baseAssets")]
        [AutoRef(nameof(DerivedAutoRefManager), "derivedAssets")]
        private sealed class MultiAutoRefAsset : ScriptableObject
        {
        }

        [AutoRef(nameof(BaseAutoRefManager), "baseAssets")]
        [AutoRef(nameof(BaseAutoRefManager), "baseAssets")]
        private sealed class DuplicateAutoRefAsset : ScriptableObject
        {
        }

        private sealed class AssetCreatorManager : ScriptableObject
        {
            [SerializeField]
            private AssetCreatorAsset[] assets = Array.Empty<AssetCreatorAsset>();
        }

        [AutoRef("OtherManager", "otherAssets")]
        [AutoRef(nameof(AssetCreatorManager), "assets")]
        private sealed class AssetCreatorAsset : ScriptableObject
        {
        }

        private sealed class DuplicateHostA
        {
            public sealed class DuplicateManager
            {
            }
        }

        private sealed class DuplicateHostB
        {
            public sealed class DuplicateManager
            {
            }
        }
    }
}
