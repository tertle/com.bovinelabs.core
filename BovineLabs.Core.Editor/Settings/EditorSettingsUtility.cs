namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Editor.Helpers;
    using BovineLabs.Core.Settings;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class EditorSettingsUtility
    {
        [NoAutoStaticsCleanup]
        private static readonly Dictionary<Type, ISettings> CachedSettings = new();

        public static T GetSettings<T>() where T : ScriptableObject, ISettings
        {
            var type = typeof(T);
            return (T)GetSettings(type);
        }

        public static ISettings GetSettings(Type type)
        {
            if (CachedSettings.TryGetValue(type, out var cached) && cached as Object != null)
            {
                return cached;
            }

            var settings = GetOrCreateSettings(type);
            CachedSettings[type] = settings!;
            return settings!;
        }

        public static bool TryGetSettings<T>(out T settings)
        {
            var type = typeof(T);
            var result = TryGetSettings(type, out var settingsUntyped);
            settings = (T)settingsUntyped;
            return result;
        }

        public static bool TryGetSettings(Type type, out ISettings settings)
        {
            if (CachedSettings.TryGetValue(type, out settings) && settings as Object != null)
            {
                return true;
            }

            settings = GetOrCreateSettings(type, false);
            if (settings == null)
            {
                return false;
            }

            CachedSettings[type] = settings;
            return true;
        }

        // Can only be null if allowCreate is false
        public static string GetAssetDirectory(string key, string defaultDirectory, string subDirectory = "", bool allowCreate = true)
        {
            GetEditorSettings()?.GetOrAddPath(key, ref defaultDirectory);

            if (!string.IsNullOrWhiteSpace(subDirectory))
            {
                defaultDirectory = Path.Combine(defaultDirectory, subDirectory);
            }

            if (!AssetDatabaseHelper.CheckOrCreateDirectories(ref defaultDirectory, allowCreate))
            {
                return null;
            }

            return defaultDirectory;
        }

        public static void AddSettingsToAuthoring(EditorSettings editorSettings, SettingsBase settingsBase)
        {
            GetConfiguredAuthorings(editorSettings);
            var assignments = new Dictionary<SettingsAuthoring, List<SettingsBase>>();

            foreach (var authoring in ResolveAuthorings(editorSettings, settingsBase))
            {
                var settings = ReadAssignedSettings(authoring);
                if (!settings.Contains(settingsBase))
                {
                    if (settings.Any(setting => setting.GetType() == settingsBase.GetType()))
                    {
                        var path = AssetDatabase.GetAssetPath(authoring);
                        throw new InvalidOperationException($"Settings prefab '{path}' already contains a {settingsBase.GetType().FullName}.");
                    }

                    settings.Add(settingsBase);
                    settings.Sort(Compare);
                    assignments.Add(authoring, settings);
                }
            }

            var changedPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assignment in assignments)
            {
                if (ApplySettings(assignment.Key, assignment.Value))
                {
                    changedPaths.Add(AssetDatabase.GetAssetPath(assignment.Key));
                }
            }

            foreach (var path in changedPaths.OrderBy(path => path, StringComparer.Ordinal))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        internal static bool UpdateSettings(EditorSettings editorSettings)
        {
            if (GetEditorSettings() != editorSettings)
            {
                throw new InvalidOperationException("The configured EditorSettings asset could not be uniquely resolved.");
            }

            var authorings = GetConfiguredAuthorings(editorSettings);
            var settingsByType = new Dictionary<Type, SettingsBase>();
            var assetGuids = new HashSet<string>(AssetDatabase.FindAssets("t:SettingsBase"), StringComparer.Ordinal);

            foreach (var type in TypeCache.GetTypesDerivedFrom<SettingsBase>().Where(type => !type.IsAbstract && !type.ContainsGenericParameters)
                         .OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                var filter = type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}";
                assetGuids.UnionWith(AssetDatabase.FindAssets($"t:{filter}"));

                // Type searches can also be incomplete while the library is being imported.
                var expectedPath = GetExpectedSettingsPath(editorSettings, type);
                var expectedAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(expectedPath);
                if (expectedAsset)
                {
                    if (!type.IsInstanceOfType(expectedAsset))
                    {
                        throw new InvalidOperationException($"Settings asset '{expectedPath}' must be a {type.FullName}.");
                    }

                    AddResolvedSetting((SettingsBase)expectedAsset);
                }
                else if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(expectedPath, AssetPathToGUIDOptions.OnlyExistingAssets)))
                {
                    throw new InvalidOperationException($"Settings asset '{expectedPath}' could not be loaded. Reimport it before reloading the domain.");
                }
            }

            foreach (var guid in assetGuids.OrderBy(guid => AssetDatabase.GUIDToAssetPath(guid), StringComparer.Ordinal))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var settingsBase = AssetDatabase.LoadAssetAtPath<SettingsBase>(path);
                if (!settingsBase)
                {
                    throw new InvalidOperationException($"Settings asset '{path}' ({guid}) could not be loaded. Reimport it before reloading the domain.");
                }

                AddResolvedSetting(settingsBase);
            }

            var assignments = authorings.ToDictionary(authoring => authoring, _ => new List<SettingsBase>());

            foreach (var authoring in authorings)
            {
                foreach (var setting in ReadAssignedSettings(authoring, skipUnresolved: true))
                {
                    AddResolvedSetting(setting);
                }
            }

            foreach (var setting in settingsByType.Values.OrderBy(setting => setting.name, StringComparer.Ordinal)
                         .ThenBy(setting => AssetDatabase.GetAssetPath(setting), StringComparer.Ordinal))
            {
                foreach (var authoring in ResolveAuthorings(editorSettings, setting))
                {
                    assignments[authoring].Add(setting);
                }
            }

            // Publish only after every asset and route has been resolved, without saving empty intermediate arrays.
            var changedPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assignment in assignments)
            {
                if (ApplySettings(assignment.Key, assignment.Value))
                {
                    changedPaths.Add(AssetDatabase.GetAssetPath(assignment.Key));
                }
            }

            foreach (var path in changedPaths.OrderBy(path => path, StringComparer.Ordinal))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            return changedPaths.Count != 0;

            void AddResolvedSetting(SettingsBase setting)
            {
                var type = setting.GetType();
                if (settingsByType.TryGetValue(type, out var existing) && existing != setting)
                {
                    var existingPath = AssetDatabase.GetAssetPath(existing);
                    var settingPath = AssetDatabase.GetAssetPath(setting);
                    throw new InvalidOperationException(
                        $"More than one {type.FullName} settings asset exists: '{existingPath}', '{settingPath}'.");
                }

                settingsByType[type] = setting;
            }
        }

        private static List<SettingsBase> ReadAssignedSettings(SettingsAuthoring authoring, bool skipUnresolved = false)
        {
            var serializedObject = new SerializedObject(authoring);
            var settingsProperty = serializedObject.FindProperty("_settings");
            var settings = new List<SettingsBase>(settingsProperty.arraySize);
            for (var index = 0; index < settingsProperty.arraySize; index++)
            {
                var setting = settingsProperty.GetArrayElementAtIndex(index).objectReferenceValue as SettingsBase;
                if (!setting)
                {
                    if (skipUnresolved)
                    {
                        continue;
                    }

                    var path = AssetDatabase.GetAssetPath(authoring);
                    throw new InvalidOperationException($"Settings prefab '{path}' has an unresolved settings reference at index {index}.");
                }

                settings.Add(setting);
            }

            return settings;
        }

        private static bool ApplySettings(SettingsAuthoring authoring, IReadOnlyList<SettingsBase> settings)
        {
            var serializedObject = new SerializedObject(authoring);
            var settingsProperty = serializedObject.FindProperty("_settings");
            var matches = settingsProperty.arraySize == settings.Count;
            for (var index = 0; matches && index < settings.Count; index++)
            {
                matches = settingsProperty.GetArrayElementAtIndex(index).objectReferenceValue == settings[index];
            }

            if (matches)
            {
                return false;
            }

            settingsProperty.arraySize = settings.Count;
            for (var index = 0; index < settings.Count; index++)
            {
                settingsProperty.GetArrayElementAtIndex(index).objectReferenceValue = settings[index];
            }

            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(authoring);
            return true;
        }

        private static HashSet<SettingsAuthoring> GetConfiguredAuthorings(EditorSettings editorSettings)
        {
            if (!editorSettings.DefaultSettingsAuthoring)
            {
                throw new InvalidOperationException("EditorSettings must reference a default settings authoring prefab.");
            }

            var authorings = new HashSet<SettingsAuthoring> { editorSettings.DefaultSettingsAuthoring };
            var worlds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var route in editorSettings.SettingsAuthorings)
            {
                if (route == null || string.IsNullOrWhiteSpace(route.World) || !worlds.Add(route.World))
                {
                    throw new InvalidOperationException("EditorSettings routes must have nonempty, unique world names.");
                }

                if (!route.Authoring)
                {
                    throw new InvalidOperationException($"EditorSettings world '{route.World}' must reference a settings authoring prefab.");
                }

                authorings.Add(route.Authoring);
            }

            foreach (var authoring in authorings)
            {
                if (!EditorUtility.IsPersistent(authoring))
                {
                    throw new InvalidOperationException($"Settings authoring '{authoring.name}' must reference a prefab asset.");
                }

                SettingsAuthoring.GetPrefabGuid(authoring);
            }

            return authorings;
        }

        private static HashSet<SettingsAuthoring> ResolveAuthorings(EditorSettings editorSettings, SettingsBase settings)
        {
            var authorings = new HashSet<SettingsAuthoring>();
            var worlds = settings.GetType().GetCustomAttribute<SettingsWorldAttribute>()?.Worlds;
            if (worlds != null)
            {
                foreach (var world in worlds)
                {
                    SettingsAuthoring authoring;
                    if (string.IsNullOrWhiteSpace(world))
                    {
                        authoring = editorSettings.DefaultSettingsAuthoring;
                    }
                    else
                    {
                        editorSettings.TryGetAuthoring(world, out authoring);
                    }

                    if (authoring)
                    {
                        authorings.Add(authoring);
                    }
                }
            }

            if (authorings.Count == 0)
            {
                authorings.Add(editorSettings.DefaultSettingsAuthoring);
            }

            return authorings;
        }

        private static ISettings GetOrCreateSettings(Type type, bool allowCreate = true)
        {
            if (!typeof(ISettings).IsAssignableFrom(type))
            {
                throw new Exception("Settings must implement ISettings");
            }

            var filter = type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}";
            var assets = AssetDatabase.FindAssets($"t:{filter}");

            ScriptableObject instance;
            var created = false;

            switch (assets.Length)
            {
                case 0:
                {
                    string path;
                    if (allowCreate)
                    {
                        var subDirectoryAttribute = type.GetCustomAttribute<SettingSubDirectoryAttribute>();
                        var subDirectory = subDirectoryAttribute != null ? subDirectoryAttribute.Directory : string.Empty;
                        var directory = GetAssetDirectory(EditorSettings.SettingsKey, EditorSettings.DefaultSettingsDirectory, subDirectory);

                        if (directory == null)
                        {
                            return null;
                        }

                        path = Path.Combine(directory, $"{type.Name}.asset");
                    }
                    else
                    {
                        path = GetExpectedSettingsPath(GetEditorSettings(), type);
                    }

                    // Search didn't work, for some reason this seems to fail sometimes due to library state
                    // So before creating a new instance, try to directly look it up where we expect it
                    instance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                    if (!instance)
                    {
                        if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets)))
                        {
                            throw new InvalidOperationException($"Settings asset '{path}' could not be loaded. Reimport it before reloading the domain.");
                        }

                        if (!allowCreate)
                        {
                            return null;
                        }

                        instance = ScriptableObject.CreateInstance(type);
                        AssetDatabase.CreateAsset(instance, path);
                        AssetDatabase.SaveAssets();
                        created = true;
                    }

                    break;
                }

                case 1:
                {
                    // Return
                    var asset = assets.First();
                    instance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(asset));
                    break;
                }

                default:
                {
                    var paths = assets.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal);
                    throw new InvalidOperationException($"More than one {type.FullName} settings asset exists: {string.Join(", ", paths)}.");
                }
            }

            if (!instance || !type.IsInstanceOfType(instance))
            {
                throw new InvalidOperationException($"{type.FullName} could not be loaded from the asset database. Reimport it before reloading the domain.");
            }

            if (created && instance is EditorSettings editorSettings)
            {
                editorSettings.InitializeCreatedAsset();
            }

            if (created && instance is SettingsSingleton settingsSingleton)
            {
                settingsSingleton.InitializeCreatedAsset();
            }

            if (allowCreate)
            {
                TryAddToSettingsAuthoring(instance);
            }

            return (ISettings)instance;
        }

        private static string GetExpectedSettingsPath(EditorSettings editorSettings, Type type)
        {
            var directory = EditorSettings.DefaultSettingsDirectory;
            if (editorSettings)
            {
                var serializedObject = new SerializedObject(editorSettings);
                var paths = serializedObject.FindProperty("_paths");
                for (var index = 0; index < paths.arraySize; index++)
                {
                    var path = paths.GetArrayElementAtIndex(index);
                    if (string.Equals(path.FindPropertyRelative("Key").stringValue, EditorSettings.SettingsKey, StringComparison.OrdinalIgnoreCase))
                    {
                        directory = path.FindPropertyRelative("Path").stringValue;
                        break;
                    }
                }
            }

            var subDirectory = type.GetCustomAttribute<SettingSubDirectoryAttribute>()?.Directory ?? string.Empty;
            return Path.Combine(directory, subDirectory, $"{type.Name}.asset").Replace('\\', '/');
        }

        private static void TryAddToSettingsAuthoring(ScriptableObject settings)
        {
            if (settings is not SettingsBase settingsBase)
            {
                return;
            }

            var editorSettings = GetEditorSettings();
            if (!editorSettings)
            {
                return;
            }

            AddSettingsToAuthoring(editorSettings, settingsBase);
        }

        private static int Compare(Object obj1, Object obj2)
        {
            var nameComparison = string.Compare(obj1.name, obj2.name, StringComparison.Ordinal);
            return nameComparison != 0 ? nameComparison :
                string.Compare(AssetDatabase.GetAssetPath(obj1), AssetDatabase.GetAssetPath(obj2), StringComparison.Ordinal);
        }

        private static EditorSettings GetEditorSettings()
        {
            var assets = AssetDatabase.FindAssets($"t:{nameof(EditorSettings)}");
            if (assets.Length == 0)
            {
                return null;
            }

            if (assets.Length > 1)
            {
                var paths = assets.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal);
                throw new InvalidOperationException($"More than one EditorSettings asset exists: {string.Join(", ", paths)}.");
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
            var settings = AssetDatabase.LoadAssetAtPath<EditorSettings>(assetPath);
            if (!settings)
            {
                throw new InvalidOperationException($"EditorSettings asset '{assetPath}' could not be loaded.");
            }

            return settings;
        }
    }
}
