// <copyright file="ObjectManagementProcessor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.Utility;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;
    using Object = UnityEngine.Object;

    /// <summary> An <see cref="AssetPostprocessor" /> that ensures <see cref="IUID" /> types always have a unique ID even if 2 branches merge. </summary>
    public class ObjectManagementProcessor : AssetPostprocessor
    {
        private static readonly HashSet<string> AlreadyProcessedAssets = new();
        private static readonly HashSet<Type> AlreadyProcessedAutoRef = new();

        private static readonly Dictionary<Type, Processor> Processors = new();
        private static readonly Dictionary<Type, AutoRefAttribute> AutoRefMap = new();
        private static readonly GlobalProcessor Global = new();

        private static readonly HashSet<string> Delayed = new();

        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            // Clean up AlreadyProcessed otherwise if a user deleted then created a new asset with same name, it would be skipped
            foreach (var assetPath in deletedAssets)
            {
                AlreadyProcessedAssets.Remove(assetPath);
            }

            var runDelayed = false;

            foreach (var assetPath in importedAssets)
            {
                // Much faster check than LoadAssetAtPath
                if (!assetPath.EndsWith(".asset"))
                {
                    continue;
                }

                if (!AlreadyProcessedAssets.Add(assetPath))
                {
                    continue;
                }

                Delayed.Add(assetPath);
                runDelayed = true;
            }

            // We use delayed execution to greatly speed up mass duplication as each duplication comes as a separate OnPostprocessAllAssets
            // and this allows us to group them together
            if (runDelayed)
            {
                EditorApplication.delayCall -= DelayedExecution;
                EditorApplication.delayCall += DelayedExecution;
            }
        }

        private static void DelayedExecution()
        {
            using (TimeProfiler.Start("ObjectManagementProcessor"))
            {
                EditorApplication.delayCall -= DelayedExecution;

                Global.Reset();
                foreach (var p in Processors)
                {
                    p.Value.Reset();
                }

                AutoRefMap.Clear();

                foreach (var assetPath in Delayed)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

                    // Instead of just doing a LoadAssetsAtPath this helps us early out for all other type of assets
                    if (!asset)
                    {
                        continue;
                    }

                    ProcessAsset(asset);
                    foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
                    {
                        var so = subAsset as ScriptableObject;

                        if (!so)
                        {
                            continue;
                        }

                        ProcessAsset(so);
                    }
                }

                foreach (var manager in AutoRefMap)
                {
                    UpdateAutoRef(manager.Key, manager.Value);
                }

                Delayed.Clear();
            }
        }

        private static void ProcessAsset(Object asset)
        {
            CheckAutoRef(asset);
            if (CheckAutoID(asset))
            {
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        private static void CheckAutoRef(Object asset)
        {
            var type = asset.GetType();

            var attribute = type.GetCustomAttribute<AutoRefAttribute>();
            if (attribute == null)
            {
                return;
            }

            AutoRefMap[type] = attribute;
        }

        private static bool CheckAutoID(Object asset)
        {
            switch (asset)
            {
                case IUIDGlobal:
                {
                    return Global.Process(asset);
                }

                case IUID:
                {
                    var assetType = asset.GetType();
                    if (!Processors.TryGetValue(assetType, out var processor))
                    {
                        processor = Processors[assetType] = new Processor(assetType);
                    }

                    return processor.Process(asset);
                }
            }

            return false;
        }

        private static void UpdateAutoRef(Type type, AutoRefAttribute attribute)
        {
            if (!AlreadyProcessedAutoRef.Add(type))
            {
                return;
            }

            var managerGuid = AssetDatabase.FindAssets($"t:{attribute.ManagerType}");
            if (managerGuid.Length == 0)
            {
                BLGlobalLogger.LogErrorString($"No manager found for {attribute.ManagerType}");
                return;
            }

            if (managerGuid.Length > 1)
            {
                BLGlobalLogger.LogErrorString($"More than one manager found for {attribute.ManagerType}");
                return;
            }

            var manager = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(managerGuid[0]));
            if (!manager)
            {
                BLGlobalLogger.LogErrorString("Manager wasn't a ScriptableObject");
                return;
            }

            var so = new SerializedObject(manager);
            var sp = so.FindProperty(attribute.FieldName);
            if (sp == null)
            {
                BLGlobalLogger.LogErrorString($"Property {attribute.FieldName} not found for {attribute.ManagerType}");
                return;
            }

            if (!sp.isArray)
            {
                BLGlobalLogger.LogErrorString($"Property {attribute.FieldName} was not type of array for {attribute.ManagerType}");
                return;
            }

            if (sp.arrayElementType != $"PPtr<${type.Name}>")
            {
                BLGlobalLogger.LogErrorString($"Property {attribute.FieldName} was not type of {type.Name} for {attribute.ManagerType}");
                return;
            }

            var objects = AssetDatabase
                .FindAssets($"t:{type.Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct() // In case multi of same type on same path
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .Where(s => s && s.GetType() == type)
                .ToList();

            sp.arraySize = objects.Count;
            for (var i = 0; i < objects.Count; i++)
            {
                sp.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(manager);
        }

        private static int GetFirstFreeID(IReadOnlyDictionary<int, int> map)
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                if (!map.ContainsKey(i))
                {
                    return i;
                }
            }

            return 0; // You'd have to hit int.MaxValue ids to ever hit this case, you have other problems
        }

        private class Processor
        {
            private readonly string filter;
            private readonly Dictionary<int, int> map = new();
            private readonly Type type;

            private bool isInitialized;

            public Processor(Type type)
            {
                this.type = type;
                this.filter = $"t:{type.Name}";
            }

            public void Reset()
            {
                this.isInitialized = false;
                this.map.Clear();
            }

            public bool Process(Object obj)
            {
                var asset = (IUID)obj;

                if (!this.isInitialized)
                {
                    this.isInitialized = true;
                    this.BuildMap();
                }

                this.map.TryGetValue(asset.ID, out var count);

                if (count > 1)
                {
                    var newId = GetFirstFreeID(this.map);
                    this.map[asset.ID] = count - 1; // update the old ID
                    asset.ID = newId;
                    this.map[newId] = 1;

                    EditorUtility.SetDirty(obj);
                    return true;
                }

                return false;
            }

            private void BuildMap()
            {
                Assert.AreEqual(0, this.map.Count);

                var paths = AssetDatabase.FindAssets(this.filter).Select(AssetDatabase.GUIDToAssetPath).Distinct();

                foreach (var path in paths)
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    foreach (var asset in assets)
                    {
                        if (!asset)
                        {
                            continue;
                        }

                        if (asset.GetType() != this.type)
                        {
                            continue;
                        }

                        var uid = (IUID)asset;
                        this.map.TryGetValue(uid.ID, out var count);
                        count++;
                        this.map[uid.ID] = count;
                    }
                }
            }
        }

        private class GlobalProcessor
        {
            private const string Filter = "t:ScriptableObject";
            private readonly Dictionary<int, int> map = new();
            private bool isInitialized;

            public void Reset()
            {
                this.isInitialized = false;
                this.map.Clear();
            }

            public bool Process(Object obj)
            {
                var asset = (IUIDGlobal)obj;

                if (!this.isInitialized)
                {
                    this.isInitialized = true;
                    this.BuildMap();
                }

                this.map.TryGetValue(asset.ID, out var count);

                if (count > 1)
                {
                    var newId = GetFirstFreeID(this.map);
                    this.map[asset.ID] = count - 1; // update the old ID
                    asset.ID = newId;
                    this.map[newId] = 1;

                    EditorUtility.SetDirty(obj);
                    return true;
                }

                return false;
            }

            private void BuildMap()
            {
                Assert.AreEqual(0, this.map.Count);

                var paths = AssetDatabase.FindAssets(Filter).Select(AssetDatabase.GUIDToAssetPath).Distinct();

                foreach (var path in paths)
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    foreach (var asset in assets)
                    {
                        if (!asset)
                        {
                            continue;
                        }

                        if (asset is not IUIDGlobal uid)
                        {
                            continue;
                        }

                        this.map.TryGetValue(uid.ID, out var count);
                        count++;
                        this.map[uid.ID] = count;
                    }
                }
            }
        }
    }
}
