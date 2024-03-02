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

            if (CachedSettings.TryGetValue(type, out var cached))
            {
                return (T)cached;
            }

            var settings = GetOrCreateSettings<T>(type);
            CachedSettings.Add(type, settings);
            return settings;
        }

        public static string GetAssetDirectory(string key, string defaultDirectory, string subDirectory = "")
        {
            GetEditorSettings()?.GetOrAddPath(key, ref defaultDirectory);

            if (!string.IsNullOrWhiteSpace(subDirectory))
            {
                defaultDirectory = Path.Combine(defaultDirectory, subDirectory);
            }

            AssetDatabaseHelper.CreateDirectories(ref defaultDirectory);

            return defaultDirectory;
        }

        private static T GetOrCreateSettings<T>(Type type)
            where T : ScriptableObject, ISettings
        {
            var filter = type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}";
            var assets = AssetDatabase.FindAssets($"t:{filter}");

            T? instance;

            switch (assets.Length)
            {
                case 0:
                {
                    string directory;
                    var resourceAttribute = type.GetCustomAttribute<ResourceSettingsAttribute>();
                    if (resourceAttribute != null)
                    {
                        var resources = "Resources";
                        if (!string.IsNullOrWhiteSpace(resourceAttribute.Directory))
                        {
                            resources = Path.Combine(resources, resourceAttribute.Directory);
                        }

                        directory = GetAssetDirectory(EditorSettings.SettingsResourceKey, EditorSettings.DefaultSettingsResourceDirectory, resources);
                    }
                    else
                    {
                        directory = GetAssetDirectory(EditorSettings.SettingsKey, EditorSettings.DefaultSettingsDirectory);
                    }

                    var path = Path.Combine(directory, $"{typeof(T).Name}.asset");

                    // Search didn't work, for some reason this seems to fail sometimes due to library state
                    // So before creating a new instance, try to directly look it up where we expect it
                    instance = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (instance == null)
                    {
                        instance = ScriptableObject.CreateInstance<T>();
                        AssetDatabase.CreateAsset(instance, path);
                        AssetDatabase.SaveAssets();
                    }

                    break;
                }

                case 1:
                {
                    // Return
                    var asset = assets.First();
                    instance = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(asset));
                    break;
                }

                default:
                {
                    // Error
                    Debug.LogError($"More than 1 instance of {typeof(T)} found. {string.Join(",", assets)}");
                    var asset = assets.First();
                    instance = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(asset));
                    break;
                }
            }

            TryAddToSettingsAuthoring(instance);
            return instance;
        }

        private static void TryAddToSettingsAuthoring<T>(T settings)
            where T : ScriptableObject, ISettings
        {
            if (settings is not SettingsBase settingsBase)
            {
                return;
            }

            var editorSettings = GetEditorSettings();
            if (editorSettings == null)
            {
                return;
            }

            var world = settings.GetType().GetCustomAttribute<SettingsWorldAttribute>()?.World;

            SettingsAuthoring? authoring;

            if (string.IsNullOrWhiteSpace(world))
            {
                authoring = editorSettings.DefaultSettingsAuthoring;
            }
            else
            {
                if (!editorSettings.TryGetAuthoring(world!, out authoring))
                {
                    Debug.LogError($"No authoring found for {world} in {nameof(EditorSettings)}");
                }
            }

            if (authoring == null)
            {
                return;
            }

            var so = new SerializedObject(authoring);
            var settingsProperty = so.FindProperty("settings");

            for (var index = 0; index < settingsProperty.arraySize; index++)
            {
                var element = settingsProperty.GetArrayElementAtIndex(index);

                var obj = element.objectReferenceValue;

                if (obj == null)
                {
                    continue;
                }

                if (obj == settingsBase)
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

        private static bool Compare(Object obj1, Object obj2)
        {
            if (obj1 == null)
            {
                return false;
            }

            if (obj2 == null)
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
                    Debug.LogError($"More than 1 EditorSettings found, using {AssetDatabase.GUIDToAssetPath(assets[0])}");
                }

                return AssetDatabase.LoadAssetAtPath<EditorSettings>(AssetDatabase.GUIDToAssetPath(assets[0]));
            }

            return null;
        }
    }
}
