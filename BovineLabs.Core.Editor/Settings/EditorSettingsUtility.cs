// <copyright file="EditorSettingsUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
    using Unity.Assertions;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary> Utility for setting up and getting settings. </summary>
    public static class EditorSettingsUtility
    {
        private static readonly Dictionary<Type, ISettings> CachedSettings = new();

        /// <summary> Gets a settings file. Create if it doesn't exist and ensures it is setup properly. </summary>
        /// <typeparam name="T"> The type. </typeparam>
        /// <returns> The settings instance. </returns>
        /// <exception cref="Exception"> Thrown if more than 1 instance found in project. </exception>
        public static T GetSettings<T>()
            where T : ScriptableObject, ISettings
        {
            var type = typeof(T);
            return (T)GetSettings(type);
        }

        /// <summary> Gets a settings file. Create if it doesn't exist and ensures it is setup properly. </summary>
        /// <param name="type"> The type. </param>
        /// <returns> The settings instance. </returns>
        /// <exception cref="Exception"> Thrown if more than 1 instance found in project. </exception>
        public static ISettings GetSettings(Type type)
        {
            if (CachedSettings.TryGetValue(type, out var cached) && cached != null)
            {
                return cached;
            }

            var settings = GetOrCreateSettings(type);
            CachedSettings[type] = settings!;
            return settings!;
        }

        /// <summary> Gets a settings file. Create if it doesn't exist and ensures it is setup properly. </summary>
        /// <param name="settings"> The settings if found. </param>
        /// <typeparam name="T"> The settings type. </typeparam>
        /// <returns> True if settings is created. </returns>
        /// <exception cref="Exception"> Thrown if more than 1 instance found in project. </exception>
        public static bool TryGetSettings<T>(out T? settings)
        {
            var type = typeof(T);
            var result = TryGetSettings(type, out var settingsUntyped);
            settings = (T?)settingsUntyped;
            return result;
        }

        /// <summary> Gets a settings file. Create if it doesn't exist and ensures it is setup properly. </summary>
        /// <param name="type"> The type. </param>
        /// <param name="settings"> The settings if found. </param>
        /// <returns> True if settings is created. </returns>
        /// <exception cref="Exception"> Thrown if more than 1 instance found in project. </exception>
        public static bool TryGetSettings(Type type, out ISettings? settings)
        {
            if (CachedSettings.TryGetValue(type, out settings) && settings != null)
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
        public static string? GetAssetDirectory(string key, string defaultDirectory, string subDirectory = "", bool allowCreate = true)
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
            if (!editorSettings.DefaultSettingsAuthoring)
            {
                return;
            }

            var worlds = settingsBase.GetType().GetCustomAttribute<SettingsWorldAttribute>()?.Worlds;

            var authorings = new HashSet<SettingsAuthoring>();

            if (worlds == null)
            {
                authorings.Add(editorSettings.DefaultSettingsAuthoring);
            }
            else
            {
                var any = false;

                foreach (var world in worlds)
                {
                    SettingsAuthoring? authoring;

                    if (string.IsNullOrWhiteSpace(world))
                    {
                        authoring = editorSettings.DefaultSettingsAuthoring;
                    }
                    else
                    {
                        editorSettings.TryGetAuthoring(world, out authoring);
                    }

                    if (!authoring)
                    {
                        continue;
                    }

                    any = true;
                    authorings.Add(authoring);
                }

                // If no matches, then just pass to default
                if (!any)
                {
                    authorings.Add(editorSettings.DefaultSettingsAuthoring);
                }
            }

            foreach (var authoring in authorings)
            {
                var so = new SerializedObject(authoring);
                var settingsProperty = so.FindProperty("settings");

                // Clear up null references
                for (var index = settingsProperty.arraySize - 1; index >= 0; index--)
                {
                    var element = settingsProperty.GetArrayElementAtIndex(index);
                    if (element.objectReferenceValue)
                    {
                        continue;
                    }

                    settingsProperty.DeleteArrayElementAtIndex(index);
                }

                // Early out if it already exists
                for (var index = 0; index < settingsProperty.arraySize; index++)
                {
                    var element = settingsProperty.GetArrayElementAtIndex(index);
                    if (element.objectReferenceValue == settingsBase)
                    {
                        return;
                    }
                }

                var insert = settingsProperty.arraySize;
                settingsProperty.InsertArrayElementAtIndex(insert);
                settingsProperty.GetArrayElementAtIndex(insert).objectReferenceValue = settingsBase;

                var length = settingsProperty.arraySize;

                // Insertion sort
                for (var i = 1; i < length; i++)
                {
                    var key = settingsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                    var j = i - 1;

                    while (j >= 0 && Compare(settingsProperty.GetArrayElementAtIndex(j).objectReferenceValue, key))
                    {
                        settingsProperty.GetArrayElementAtIndex(j + 1).objectReferenceValue = settingsProperty.GetArrayElementAtIndex(j).objectReferenceValue;
                        j -= 1;
                    }

                    settingsProperty.GetArrayElementAtIndex(j + 1).objectReferenceValue = key;
                }

                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(authoring);
            }
        }

        private static ISettings? GetOrCreateSettings(Type type, bool allowCreate = true)
        {
            if (!typeof(ISettings).IsAssignableFrom(type))
            {
                throw new Exception("Settings must implement ISettings");
            }

            var filter = type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}";
            var assets = AssetDatabase.FindAssets($"t:{filter}");

            ScriptableObject? instance;

            switch (assets.Length)
            {
                case 0:
                {
                    var subDirectoryAttribute = type.GetCustomAttribute<SettingSubDirectoryAttribute>();
                    var subDirectory = subDirectoryAttribute != null ? subDirectoryAttribute.Directory : string.Empty;
                    var directory = GetAssetDirectory(EditorSettings.SettingsKey, EditorSettings.DefaultSettingsDirectory, subDirectory, allowCreate: allowCreate);

                    if (directory == null)
                    {
                        return null;
                    }

                    var path = Path.Combine(directory, $"{type.Name}.asset");

                    // Search didn't work, for some reason this seems to fail sometimes due to library state
                    // So before creating a new instance, try to directly look it up where we expect it
                    instance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                    if (!instance)
                    {
                        if (!allowCreate)
                        {
                            return null;
                        }

                        instance = ScriptableObject.CreateInstance(type);
                        AssetDatabase.CreateAsset(instance, path);
                        AssetDatabase.SaveAssets();
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
                    // Error
                    BLGlobalLogger.LogErrorString($"More than 1 instance of {type.Name} found. {string.Join(",", assets)}");
                    var asset = assets.First();
                    instance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(asset));
                    break;
                }
            }

            Assert.IsNotNull(instance, $"{type.Name} returned null from asset database. Might need to reimport something.");

            TryAddToSettingsAuthoring(instance);

            return (ISettings)instance;
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

        private static bool Compare(Object obj1, Object obj2)
        {
            if (!obj1)
            {
                return false;
            }

            if (!obj2)
            {
                return true;
            }

            return string.Compare(obj1.name, obj2.name, StringComparison.Ordinal) > 0;
        }

        private static EditorSettings? GetEditorSettings()
        {
            var assets = AssetDatabase.FindAssets($"t:{nameof(EditorSettings)}");

            // No editor settings, use the default
            if (assets.Length != 0)
            {
                if (assets.Length > 2)
                {
                    BLGlobalLogger.LogErrorString($"More than 1 EditorSettings found, using {AssetDatabase.GUIDToAssetPath(assets[0])}");
                }

                return AssetDatabase.LoadAssetAtPath<EditorSettings>(AssetDatabase.GUIDToAssetPath(assets[0]));
            }

            return null;
        }
    }
}
