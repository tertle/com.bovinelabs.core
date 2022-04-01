// <copyright file="EditorSettingsUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.IO;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEngine;

    /// <summary> Utility for setting up and getting settings. </summary>
    public static class EditorSettingsUtility
    {
        /// <summary> Gets a settings file. Create if it doesn't exist and ensures it is setup properly. </summary>
        /// <typeparam name="T"> The type. </typeparam>
        /// <returns> The settings instance. </returns>
        /// <exception cref="Exception"> Thrown if more than 1 instance found in project. </exception>
        public static T GetSettings<T>()
            where T : ScriptableObject, ISettings
        {
            var assets = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            string asset;

            switch (assets.Length)
            {
                case 0:

                    var directory = GetAssetDirectory();
                    var path = Path.Combine(directory, $"{typeof(T).Name}.asset");

                    // Search didn't work, for some reason this seems to fail sometimes due to library state
                    // So before creating a new instance, try to directly look it up where we expect it
                    var instance = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (instance != null)
                    {
                        return instance;
                    }

                    var combo = string.Empty;
                    var dir = directory.Split('/');
                    foreach (var d in dir)
                    {
                        if (string.IsNullOrWhiteSpace(d))
                        {
                            continue;
                        }

                        var p = Path.Combine(combo, d);

                        if (!UnityEditor.AssetDatabase.IsValidFolder(p))
                        {
                            UnityEditor.AssetDatabase.CreateFolder(combo, d);
                        }

                        combo = p;
                    }

                    instance = ScriptableObject.CreateInstance<T>();
                    UnityEditor.AssetDatabase.CreateAsset(instance, path);
                    UnityEditor.AssetDatabase.SaveAssets();
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

        private static string GetAssetDirectory()
        {
            var assets = AssetDatabase.FindAssets($"t:{nameof(EditorSettingsBase)}");

            // No editor settings, use the default
            if (assets.Length == 0)
            {
                return EditorSettingsBase.DefaultSettingsDirectory;
            }

            if (assets.Length > 2)
            {
                Debug.LogError($"More than 1 EditorSettingsBase found, using {AssetDatabase.GUIDToAssetPath(assets[0])}");
            }

            var settings = AssetDatabase.LoadAssetAtPath<EditorSettingsBase>(AssetDatabase.GUIDToAssetPath(assets[0]));
            return settings.SettingsPath;
        }
    }
}