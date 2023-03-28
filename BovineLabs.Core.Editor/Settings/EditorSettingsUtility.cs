// <copyright file="EditorSettingsUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BovineLabs.Core.Editor.Helpers;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEngine;

    /// <summary> Utility for setting up and getting settings. </summary>
    public static class EditorSettingsUtility
    {
        private static Dictionary<Type, ISettings> cachedSettings = new();

        /// <summary> Gets a settings file. Create if it doesn't exist and ensures it is setup properly. </summary>
        /// <typeparam name="T"> The type. </typeparam>
        /// <returns> The settings instance. </returns>
        /// <exception cref="Exception"> Thrown if more than 1 instance found in project. </exception>
        public static T GetSettings<T>()
            where T : ScriptableObject, ISettings
        {
            var type = typeof(T);

            if (cachedSettings.TryGetValue(type, out var cached))
            {
                return (T)cached;
            }

            var settings = GetOrCreateSettings<T>(type);
            cachedSettings.Add(type, settings);
            return settings;
        }

        private static T GetOrCreateSettings<T>(Type type)
            where T : ScriptableObject, ISettings
        {
            var filter = type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}";
            var assets = AssetDatabase.FindAssets($"t:{filter}");

            string asset;

            switch (assets.Length)
            {
                case 0:

                    var directory = typeof(KSettings).IsAssignableFrom(typeof(T))
                        ? GetAssetDirectory(EditorFoldersSettings.KSettingsKey, EditorFoldersSettings.DefaultKSettingsDirectory)
                        : GetAssetDirectory(EditorFoldersSettings.SettingsKey, EditorFoldersSettings.DefaultSettingsDirectory);

                    var path = Path.Combine(directory, $"{typeof(T).Name}.asset");

                    // Search didn't work, for some reason this seems to fail sometimes due to library state
                    // So before creating a new instance, try to directly look it up where we expect it
                    var instance = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (instance != null)
                    {
                        return instance;
                    }

                    instance = ScriptableObject.CreateInstance<T>();
                    AssetDatabase.CreateAsset(instance, path);
                    AssetDatabase.SaveAssets();
                    return instance;

                case 1:
                    // Return
                    asset = assets.First();
                    break;

                default:
                    // Error
                    Debug.LogError($"More than 1 instance of {typeof(T)} found. {string.Join(",", assets)}");
                    asset = assets.First();
                    break;
            }

            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(asset));
        }

        public static string GetAssetDirectory(string key, string defaultDirectory)
        {
            var assets = AssetDatabase.FindAssets($"t:{nameof(EditorFoldersSettings)}");

            // No editor settings, use the default
            if (assets.Length != 0)
            {
                if (assets.Length > 2)
                {
                    Debug.LogError($"More than 1 EditorFoldersSettings found, using {AssetDatabase.GUIDToAssetPath(assets[0])}");
                }

                var settings = AssetDatabase.LoadAssetAtPath<EditorFoldersSettings>(AssetDatabase.GUIDToAssetPath(assets[0]));

                settings.GetOrAddPath(key, ref defaultDirectory);
            }

            AssetDatabaseHelper.CreateDirectories(defaultDirectory);

            return defaultDirectory;
        }
    }
}
