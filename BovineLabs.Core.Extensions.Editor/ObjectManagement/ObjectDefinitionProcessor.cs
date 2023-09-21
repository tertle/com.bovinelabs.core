// <copyright file="ObjectDefinitionProcessor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.ObjectManagement;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary> An <see cref="AssetPostprocessor"/> that ensures <see cref="IUID" /> types always have a unique ID even if 2 branches merge. </summary>
    public class ObjectDefinitionProcessor : AssetPostprocessor
    {
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        [SuppressMessage("ReSharper", "Unity.IncorrectMethodSignature", Justification = "Changed in 2021")]
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload || importedAssets.Length == 0)
            {
                return;
            }

            var processors = new Dictionary<Type, Processor>();

            foreach (var assetPath in importedAssets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

                // Instead of just doing a LoadAssetsAtPath tHis helps us early out for all other type of assets
                if (asset == null)
                {
                    continue;
                }

                ProcessAsset(asset, processors);
                foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
                {
                    ProcessAsset(subAsset, processors);
                }
            }

            foreach (var p in processors)
            {
                UpdateManager(p.Value.Type);
            }
        }

        private static void ProcessAsset(Object asset, Dictionary<Type, Processor> processors)
        {
            if (asset is not IUID)
            {
                return;
            }

            var assetType = asset.GetType();
            if (!processors.TryGetValue(assetType, out var processor))
            {
                processor = processors[assetType] = new Processor(assetType);
            }

            processor.Process(asset);
        }

        private static void UpdateManager(Type type)
        {
            var attribute = type.GetCustomAttribute<UIDManagerAttribute>();
            if (attribute == null)
            {
                return;
            }

            var managerGuid = AssetDatabase.FindAssets($"t:{attribute.Manager}");
            if (managerGuid.Length == 0)
            {
                Debug.LogError($"No manager found for {attribute.Manager}");
                return;
            }

            if (managerGuid.Length > 1)
            {
                Debug.LogError($"More than one manager found for {attribute.Manager}");
                return;
            }

            var manager = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(managerGuid[0]));
            if (manager == null)
            {
                Debug.LogError("Manager wasn't a ScriptableObject");
                return;
            }

            var so = new SerializedObject(manager);
            var sp = so.FindProperty(attribute.Property);
            if (sp == null)
            {
                Debug.LogError($"Property {attribute.Property} not found for {attribute.Manager}");
                return;
            }

            if (!sp.isArray)
            {
                Debug.LogError($"Property {attribute.Property} was not type of array for {attribute.Manager}");
                return;
            }

            if (sp.arrayElementType != $"PPtr<${type.Name}>")
            {
                Debug.LogError($"Property {attribute.Property} was not type of {type.Name} for {attribute.Manager}");
                return;
            }

            var objects = AssetDatabase.FindAssets($"t:{type.Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct() // In case multi of same type on same path
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .Where(s => s.GetType() == type)
                .ToList();

            sp.arraySize = objects.Count;
            for (var i = 0; i < objects.Count; i++)
            {
                sp.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(manager);
        }

        private class Processor
        {
            private readonly string filter;
            private Dictionary<int, int>? map;

            public Processor(Type type)
            {
                this.Type = type;
                this.filter = $"t:{type.Name}";
            }

            public Type Type { get; }

            public void Process(Object obj)
            {
                var asset = (IUID)obj;

                this.map ??= this.GetIDMap();
                this.map.TryGetValue(asset.ID, out var count);

                if (count > 1)
                {
                    var newId = GetFirstFreeID(this.map);
                    this.map[asset.ID] = count - 1; // update the old ID
                    asset.ID = newId;
                    this.map[newId] = 1;
                    EditorUtility.SetDirty(obj);
                    AssetDatabase.SaveAssetIfDirty(obj);
                }
            }

            private Dictionary<int, int> GetIDMap()
            {
                var idMap = new Dictionary<int, int>();

                var paths = AssetDatabase.FindAssets(this.filter).Select(AssetDatabase.GUIDToAssetPath).Distinct();

                foreach (var path in paths)
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    foreach (var asset in assets)
                    {
                        if (asset.GetType() != this.Type)
                        {
                            continue;
                        }

                        var uid = (IUID)asset;
                        idMap.TryGetValue(uid.ID, out var count);
                        count++;
                        idMap[uid.ID] = count;
                    }
                }

                return idMap;
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

                return -1; // You'd have to hit int.MaxValue ids to ever hit this case, you have other problems
            }
        }
    }
}
#endif
