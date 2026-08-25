// <copyright file="AutoRefProcessor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Asset
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core;
    using BovineLabs.Core.Asset;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Settings;
    using BovineLabs.Core.Utility;
    using JetBrains.Annotations;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assemblies;
    using UnityEngine.Assertions;
    using Object = UnityEngine.Object;

    /// <summary> An <see cref="AssetPostprocessor" /> that ensures <see cref="IUID" /> types always have a unique ID even if 2 branches merge. </summary>
    public class AutoRefProcessor : AssetPostprocessor
    {
        [NoAutoStaticsCleanup]
        private static readonly HashSet<string> InFlightAssets = new(StringComparer.Ordinal);

        [NoAutoStaticsCleanup]
        private static readonly HashSet<AutoRefKey> AlreadyProcessedAutoRef = new();

        [NoAutoStaticsCleanup]
        private static readonly Dictionary<Type, Processor> Processors = new();

        [NoAutoStaticsCleanup]
        private static readonly Dictionary<AutoRefKey, (AutoRefAttribute Attribute, Type DefiningType)> AutoRefMap = new();

        [NoAutoStaticsCleanup]
        private static readonly GlobalProcessor Global = new();

        [NoAutoStaticsCleanup]
        private static readonly Dictionary<string, PreviousImportState> PreviousImports = new(StringComparer.Ordinal);

        [NoAutoStaticsCleanup]
        private static readonly HashSet<Type> QueuedAutoRefTypes = new();

        [NoAutoStaticsCleanup]
        private static readonly HashSet<string> ImportExtensions;

        [NoAutoStaticsCleanup]
        private static bool isProcessing;

        static AutoRefProcessor()
        {
            ImportExtensions = BuildImportExtensions();
        }

        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        private void OnPreprocessAsset()
        {
            if (isProcessing || InFlightAssets.Contains(this.assetPath) || !ShouldQueueImportedAsset(this.assetPath))
            {
                return;
            }

            PreviousImports[this.assetPath] = CapturePreviousImport(this.assetPath);
        }

        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (isProcessing)
            {
                return;
            }

            var runDelayed = false;

            foreach (var assetPath in deletedAssets)
            {
                PreviousImports.Remove(assetPath);
                InFlightAssets.Remove(assetPath);

                if (ShouldQueueImportedAsset(assetPath))
                {
                    QueueAllAutoRefTypes();
                    runDelayed = true;
                }
            }

            foreach (var assetPath in importedAssets)
            {
                QueueImportedAsset(assetPath, ref runDelayed);
            }

            for (var i = 0; i < movedAssets.Length; i++)
            {
                QueueImportedAsset(movedAssets[i], ref runDelayed);

                if (!ShouldQueueImportedAsset(movedAssets[i]) && ShouldQueueImportedAsset(movedFromAssetPaths[i]))
                {
                    QueueAllAutoRefTypes();
                    runDelayed = true;
                }
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
            if (isProcessing)
            {
                return;
            }

            isProcessing = true;
            EditorApplication.delayCall -= DelayedExecution;

            try
            {
                using (TimeProfiler.Start("AutoRefProcessor"))
                {
                    Global.Reset();
                    foreach (var p in Processors)
                    {
                        p.Value.Reset();
                    }

                    AutoRefMap.Clear();
                    AlreadyProcessedAutoRef.Clear();

                    foreach (var type in QueuedAutoRefTypes)
                    {
                        CheckAutoRef(type);
                    }

                    var loadedAssets = new List<(Object Asset, bool Restored)>();

                    foreach (var assetPath in InFlightAssets)
                    {
                        PreviousImports.TryGetValue(assetPath, out var previousImport);

                        foreach (var asset in LoadScriptableObjectsAtPath(assetPath))
                        {
                            loadedAssets.Add((asset, TryRestorePreviousID(asset, previousImport)));
                        }
                    }

                    foreach (var (asset, restored) in loadedAssets)
                    {
                        ProcessAsset(asset, restored);
                    }

                    foreach (var manager in AutoRefMap)
                    {
                        UpdateAutoRef(manager.Key, manager.Value.Attribute, manager.Value.DefiningType);
                    }
                }
            }
            finally
            {
                foreach (var assetPath in InFlightAssets)
                {
                    PreviousImports.Remove(assetPath);
                }

                QueuedAutoRefTypes.Clear();
                InFlightAssets.Clear();
                isProcessing = false;
            }
        }

        private static void ProcessAsset(Object asset, bool updated)
        {
            CheckAutoRef(asset);
            updated |= CheckAutoID(asset);

            if (updated)
            {
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        private static void CheckAutoRef(Object asset)
        {
            CheckAutoRef(asset.GetType());
        }

        private static void CheckAutoRef(Type type)
        {
            foreach (var (attribute, definingType) in GetAutoRefAttributes(type))
            {
                AutoRefMap.TryAdd(new AutoRefKey(definingType, attribute), (attribute, definingType));
            }
        }

        private static bool TryRestorePreviousID(Object asset, PreviousImportState previousImport)
        {
            if (previousImport == null || asset is not IUID uid || uid.ID != 0 ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localFileID) ||
                !previousImport.IDs.TryGetValue(new ImportedObjectKey(asset.GetType(), guid, localFileID), out var previousID))
            {
                return false;
            }

            uid.ID = previousID;
            EditorUtility.SetDirty(asset);
            return true;
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
                    var current = asset.GetType();

                    // We always implement it
                    while (true)
                    {
                        var baseType = current.BaseType!; // we will never hit the bottom

                        // We already know targetInterface is assignable from current somewhere in the chain,
                        // so we only need to find the first point where the base no longer has it.
                        if (!typeof(IUID).IsAssignableFrom(baseType))
                        {
                            if (!Processors.TryGetValue(current, out var processor))
                            {
                                processor = Processors[current] = new Processor(current);
                            }

                            return processor.Process(asset);
                        }

                        current = baseType;
                    }
                }
            }

            return false;
        }

        private static void UpdateAutoRef(AutoRefKey key, AutoRefAttribute attribute, Type definingType)
        {
            if (!AlreadyProcessedAutoRef.Add(key))
            {
                return;
            }

            var manager = ResolveManagerAsset(attribute);
            if (!manager)
            {
                return;
            }

            var useEntryMode = !string.IsNullOrEmpty(attribute.ReferenceFieldName);
            var objects = GetAutoRefObjects(definingType, useEntryMode);
            var updated = !useEntryMode
                ? UpdateAutoRefDirect(manager, attribute, definingType, objects)
                : UpdateAutoRefEntries(manager, attribute, definingType, objects);

            if (!updated)
            {
                return;
            }

            TryCallAutoRefPostProcessor(manager, attribute.FieldName);
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssetIfDirty(manager);
        }

        private static bool UpdateAutoRefDirect(ScriptableObject manager, AutoRefAttribute attribute, Type definingType, IReadOnlyList<Object> objects)
        {
            var so = new SerializedObject(manager);
            var sp = so.FindProperty(attribute.FieldName);
            if (sp == null)
            {
                BLGlobalLogger.LogErrorString($"Property {attribute.FieldName} not found for {attribute.ManagerType}");
                return false;
            }

            if (!sp.isArray)
            {
                BLGlobalLogger.LogErrorString($"Property {attribute.FieldName} was not type of array for {attribute.ManagerType}");
                return false;
            }

            if (sp.arrayElementType != $"PPtr<${definingType.Name}>")
            {
                BLGlobalLogger.LogErrorString($"Property {attribute.FieldName} was not type of defining type {definingType.Name} on {attribute.ManagerType}");
                return false;
            }

            sp.arraySize = objects.Count;
            for (var i = 0; i < objects.Count; i++)
            {
                sp.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static bool UpdateAutoRefEntries(ScriptableObject manager, AutoRefAttribute attribute, Type definingType, IReadOnlyList<Object> objects)
        {
            var so = new SerializedObject(manager);
            var sp = so.FindProperty(attribute.FieldName);
            if (sp == null)
            {
                BLGlobalLogger.LogErrorString($"Property {attribute.FieldName} not found for {attribute.ManagerType}");
                return false;
            }

            if (!sp.isArray)
            {
                BLGlobalLogger.LogErrorString($"Property {attribute.FieldName} was not type of array for {attribute.ManagerType}");
                return false;
            }

            var field = GetInstanceField(manager.GetType(), attribute.FieldName);
            if (field == null)
            {
                BLGlobalLogger.LogErrorString($"Property {attribute.FieldName} not found for {attribute.ManagerType}");
                return false;
            }

            var elementType = GetArrayOrListElementType(field.FieldType);
            if (elementType == null)
            {
                BLGlobalLogger.LogErrorString($"Property {attribute.FieldName} was not type of array for {attribute.ManagerType}");
                return false;
            }

            var referenceField = GetInstanceField(elementType, attribute.ReferenceFieldName);
            if (referenceField == null)
            {
                BLGlobalLogger.LogErrorString(
                    $"Property {attribute.ReferenceFieldName} not found on entry type {elementType.Name} for {attribute.ManagerType}");
                return false;
            }

            if (!HasSerializedEntryReferenceProperty(sp, attribute.ReferenceFieldName, attribute.ManagerType, elementType.Name))
            {
                return false;
            }

            if (!typeof(Object).IsAssignableFrom(referenceField.FieldType) || !referenceField.FieldType.IsAssignableFrom(definingType))
            {
                BLGlobalLogger.LogErrorString(
                    $"Property {attribute.ReferenceFieldName} was not type of defining type {definingType.Name} for entry type {elementType.Name} " +
                    $"on {attribute.ManagerType}");
                return false;
            }

            var discovered = new HashSet<Object>(objects);
            var seen = new HashSet<Object>();
            var entries = new List<object>(objects.Count);

            foreach (var entry in EnumerateEntries(field.GetValue(manager)))
            {
                var reference = referenceField.GetValue(entry) as Object;
                if (!reference || !discovered.Contains(reference) || !seen.Add(reference))
                {
                    continue;
                }

                entries.Add(entry);
            }

            foreach (var obj in objects)
            {
                if (!seen.Add(obj))
                {
                    continue;
                }

                if (!TryCreateDefaultEntry(elementType, attribute.ManagerType, out var entry))
                {
                    return false;
                }

                referenceField.SetValue(entry, obj);
                entries.Add(entry);
            }

            if (field.FieldType.IsArray)
            {
                var array = Array.CreateInstance(elementType, entries.Count);
                for (var i = 0; i < entries.Count; i++)
                {
                    array.SetValue(entries[i], i);
                }

                field.SetValue(manager, array);
                return true;
            }

            if (!TryGetMutableList(manager, field, attribute.ManagerType, out var list))
            {
                return false;
            }

            list.Clear();
            foreach (var entry in entries)
            {
                list.Add(entry);
            }

            return true;
        }

        private static bool ShouldQueueImportedAsset(string assetPath)
        {
            if (assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var extension = Path.GetExtension(assetPath);
            return !string.IsNullOrEmpty(extension) && ImportExtensions.Contains(extension);
        }

        private static PreviousImportState CapturePreviousImport(string assetPath)
        {
            var state = new PreviousImportState();

            foreach (var asset in LoadScriptableObjectsAtPath(assetPath))
            {
                state.Types.Add(asset.GetType());

                if (asset is not IUID { ID: > 0 } uid ||
                    !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localFileID))
                {
                    continue;
                }

                state.IDs[new ImportedObjectKey(asset.GetType(), guid, localFileID)] = uid.ID;
            }

            return state;
        }

        private static void QueueImportedAsset(string assetPath, ref bool runDelayed)
        {
            if (!ShouldQueueImportedAsset(assetPath))
            {
                return;
            }

            if (PreviousImports.TryGetValue(assetPath, out var previousImport))
            {
                QueuedAutoRefTypes.UnionWith(previousImport.Types);
            }

            if (InFlightAssets.Add(assetPath))
            {
                runDelayed = true;
            }
        }

        private static void QueueAllAutoRefTypes()
        {
            QueuedAutoRefTypes.UnionWith(TypeCache.GetTypesWithAttribute<AutoRefAttribute>());
        }

        private static IEnumerable<ScriptableObject> LoadScriptableObjectsAtPath(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset)
            {
                yield return asset;
            }

            foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
            {
                if (subAsset is ScriptableObject so && so)
                {
                    yield return so;
                }
            }
        }

        private static ScriptableObject ResolveManagerAsset(AutoRefAttribute attribute)
        {
            var managerGuid = AssetDatabase.FindAssets($"t:{attribute.ManagerType}");
            if (managerGuid.Length > 1)
            {
                BLGlobalLogger.LogErrorString($"More than one manager found for {attribute.ManagerType}");
                return null;
            }

            if (managerGuid.Length == 1)
            {
                var manager = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(managerGuid[0]));
                if (!manager)
                {
                    BLGlobalLogger.LogErrorString("Manager wasn't a ScriptableObject");
                }

                return manager;
            }

            var managerType = ResolveManagerType(attribute.ManagerType);
            if (managerType == null || !typeof(ScriptableObject).IsAssignableFrom(managerType) || !typeof(ISettings).IsAssignableFrom(managerType))
            {
                BLGlobalLogger.LogErrorString($"No manager found for {attribute.ManagerType}");
                return null;
            }

            return (ScriptableObject)EditorSettingsUtility.GetSettings(managerType);
        }

        private static Type ResolveManagerType(string managerType)
        {
            var nameMatches = new List<Type>();

            foreach (var assembly in CurrentAssemblies.GetLoadedAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (type.FullName == managerType)
                    {
                        return type;
                    }

                    if (type.Name == managerType)
                    {
                        nameMatches.Add(type);
                    }
                }
            }

            if (nameMatches.Count > 1)
            {
                BLGlobalLogger.LogErrorString($"More than one manager type found for {managerType}");
                return null;
            }

            return nameMatches.FirstOrDefault();
        }

        private static List<Object> GetAutoRefObjects(Type definingType, bool orderByAssetPath)
        {
            var paths = new HashSet<string>();

            foreach (var searchType in GetAutoRefSearchTypes(definingType))
            {
                foreach (var guid in AssetDatabase.FindAssets($"t:{searchType.Name}"))
                {
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
                }
            }

            var objects = paths
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .Where(s => s && definingType.IsInstanceOfType(s));

            if (orderByAssetPath)
            {
                objects = objects
                    .OrderBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal)
                    .ThenBy(s => s.name, StringComparer.Ordinal);
            }

            return objects.ToList();
        }

        private static IEnumerable<Type> GetAutoRefSearchTypes(Type definingType)
        {
            if (!definingType.IsAbstract)
            {
                yield return definingType;
            }

            foreach (var type in TypeCache.GetTypesDerivedFrom(definingType))
            {
                if (type.IsAbstract || !typeof(ScriptableObject).IsAssignableFrom(type))
                {
                    continue;
                }

                yield return type;
            }
        }

        private static FieldInfo GetInstanceField(Type type, string fieldName)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static Type GetArrayOrListElementType(Type fieldType)
        {
            if (fieldType.IsArray)
            {
                return fieldType.GetElementType();
            }

            return typeof(IList).IsAssignableFrom(fieldType) && fieldType.IsGenericType ? fieldType.GetGenericArguments()[0] : null;
        }

        private static bool HasSerializedEntryReferenceProperty(
            SerializedProperty entriesProperty, string referenceFieldName, string managerType, string entryTypeName)
        {
            var originalSize = entriesProperty.arraySize;
            if (originalSize == 0)
            {
                entriesProperty.arraySize = 1;
            }

            var referenceProperty = entriesProperty.GetArrayElementAtIndex(0).FindPropertyRelative(referenceFieldName);

            if (originalSize == 0)
            {
                entriesProperty.arraySize = originalSize;
            }

            if (referenceProperty != null)
            {
                return true;
            }

            BLGlobalLogger.LogErrorString($"Property {referenceFieldName} was not serialized on entry type {entryTypeName} for {managerType}");
            return false;
        }

        private static bool TryCreateDefaultEntry(Type elementType, string managerType, out object entry)
        {
            try
            {
                entry = Activator.CreateInstance(elementType);
                return true;
            }
            catch (Exception ex)
            {
                BLGlobalLogger.LogErrorString($"Entry type {elementType.Name} for {managerType} couldn't be created: {ex.Message}");
                entry = null;
                return false;
            }
        }

        private static bool TryGetMutableList(ScriptableObject manager, FieldInfo field, string managerType, out IList list)
        {
            var value = field.GetValue(manager);
            if (value == null)
            {
                try
                {
                    value = Activator.CreateInstance(field.FieldType);
                }
                catch (Exception ex)
                {
                    BLGlobalLogger.LogErrorString($"Property {field.Name} couldn't create list type {field.FieldType.Name} for {managerType}: {ex.Message}");
                    list = null;
                    return false;
                }

                field.SetValue(manager, value);
            }

            list = (IList)value;
            return true;
        }

        private static IEnumerable<object> EnumerateEntries(object entries)
        {
            if (entries == null)
            {
                yield break;
            }

            foreach (var entry in (IEnumerable)entries)
            {
                if (entry != null)
                {
                    yield return entry;
                }
            }
        }

        private static void TryCallAutoRefPostProcessor(ScriptableObject manager, string fieldName)
        {
            if (manager is IAutoRefPostProcessor postProcessor)
            {
                postProcessor.OnAutoRefUpdated(fieldName);
            }
        }

        private static HashSet<string> BuildImportExtensions()
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in TypeCache.GetTypesWithAttribute<AutoRefImportAttribute>())
            {
                foreach (var attribute in type.GetCustomAttributes<AutoRefImportAttribute>(false))
                {
                    var extension = NormalizeExtension(attribute.Extension);
                    if (!string.IsNullOrEmpty(extension))
                    {
                        extensions.Add(extension);
                    }
                }
            }

            return extensions;
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return string.Empty;
            }

            extension = extension.Trim();
            return extension[0] == '.' ? extension : $".{extension}";
        }

        private static int GetFirstFreeID(IReadOnlyDictionary<int, int> map)
        {
            for (var i = 1; i < int.MaxValue; i++)
            {
                if (!map.ContainsKey(i))
                {
                    return i;
                }
            }

            return 0; // You'd have to hit int.MaxValue ids to ever hit this case, you have other problems
        }

        private static IEnumerable<(AutoRefAttribute Attribute, Type DefiningType)> GetAutoRefAttributes(Type start)
        {
            for (var t = start; t != null; t = t.BaseType)
            {
                foreach (var attribute in t.GetCustomAttributes<AutoRefAttribute>(false))
                {
                    yield return (attribute, t);
                }
            }
        }

        private readonly struct AutoRefKey : IEquatable<AutoRefKey>
        {
            private readonly Type definingType;
            private readonly string managerType;
            private readonly string fieldName;
            private readonly string referenceFieldName;

            public AutoRefKey(Type definingType, AutoRefAttribute attribute)
            {
                this.definingType = definingType;
                this.managerType = attribute.ManagerType;
                this.fieldName = attribute.FieldName;
                this.referenceFieldName = attribute.ReferenceFieldName ?? string.Empty;
            }

            public bool Equals(AutoRefKey other)
            {
                return this.definingType == other.definingType &&
                    string.Equals(this.managerType, other.managerType, StringComparison.Ordinal) &&
                    string.Equals(this.fieldName, other.fieldName, StringComparison.Ordinal) &&
                    string.Equals(this.referenceFieldName, other.referenceFieldName, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is AutoRefKey other && this.Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = this.definingType.GetHashCode();
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(this.managerType);
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(this.fieldName);
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(this.referenceFieldName);
                    return hashCode;
                }
            }
        }

        private readonly struct ImportedObjectKey : IEquatable<ImportedObjectKey>
        {
            private readonly Type type;
            private readonly string guid;
            private readonly long localFileID;

            public ImportedObjectKey(Type type, string guid, long localFileID)
            {
                this.type = type;
                this.guid = guid;
                this.localFileID = localFileID;
            }

            public bool Equals(ImportedObjectKey other)
            {
                return this.type == other.type && this.localFileID == other.localFileID &&
                    string.Equals(this.guid, other.guid, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is ImportedObjectKey other && this.Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = this.type.GetHashCode();
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(this.guid);
                    hashCode = (hashCode * 397) ^ this.localFileID.GetHashCode();
                    return hashCode;
                }
            }
        }

        private sealed class PreviousImportState
        {
            public Dictionary<ImportedObjectKey, int> IDs { get; } = new();

            public HashSet<Type> Types { get; } = new();
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

                if (asset.ID == 0 || count > 1)
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

                        if (!this.type.IsInstanceOfType(asset))
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

                if (asset.ID == 0 || count > 1)
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
