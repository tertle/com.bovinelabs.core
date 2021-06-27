// <copyright file="EditorSettingsUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEngine;

    /// <summary> Utility for setting up and getting settings. </summary>
    public static class EditorSettingsUtility
    {
        private const string ResourceSettingsBasePath = "Settings/{0}";

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

                    // Search didn't work, for some reason this seems to fail sometimes due to library state
                    // So before creating a new instance, try to directly look it up where we expect it
                    const string directory = "Prefabs";
                    var path = $"Assets/{directory}/{GetResourcePath(typeof(T))}.asset";

                    var instance = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (instance != null)
                    {
                        return instance;
                    }

                    // Still couldn't find it, create a new one
                    if (!UnityEditor.AssetDatabase.IsValidFolder($"Assets/{directory}"))
                    {
                        UnityEditor.AssetDatabase.CreateFolder("Assets", directory);
                    }

                    if (!UnityEditor.AssetDatabase.IsValidFolder($"Assets/{directory}/Settings"))
                    {
                        UnityEditor.AssetDatabase.CreateFolder($"Assets/{directory}", "Settings");
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

// #if BL_ADDRESSABLES
//             var directory = $"Prefabs";
// #else
//             var directory = $"Resources";
// #endif
//
//             var path = $"Assets/{directory}/{SettingsUtility.GetResourcePath(typeof(T)).RuntimeKey}.asset";
//
//             var instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
//
//             if (instance == null)
//             {
//                 if (!UnityEditor.AssetDatabase.IsValidFolder($"Assets/{directory}"))
//                 {
//                     UnityEditor.AssetDatabase.CreateFolder("Assets", directory);
//                 }
//
//                 if (!UnityEditor.AssetDatabase.IsValidFolder($"Assets/{directory}/Settings"))
//                 {
//                     UnityEditor.AssetDatabase.CreateFolder($"Assets/{directory}", "Settings");
//                 }
//
//                 var existingAsset = AssetDatabase.FindAssets($"t:{typeof(T)}");
//                 if (existingAsset.Length == 0)
//                 {
//                     instance = ScriptableObject.CreateInstance<T>();
//                     UnityEditor.AssetDatabase.CreateAsset(instance, path);
//                     UnityEditor.AssetDatabase.SaveAssets();
//                 }
//                 else if (existingAsset.Length == 1)
//                 {
//                     instance = MoveSettings<T>(existingAsset[0], path);
//                 }
//                 else
//                 {
//                     var existing = existingAsset.Where(s => !AssetDatabase.GUIDToAssetPath(s).StartsWith("Packages/")).ToArray();
//
//                     if (existing.Length == 1)
//                     {
//                         instance = MoveSettings<T>(existing[0], path);
//                     }
//                     else
//                     {
//                         throw new Exception($"More than one {typeof(T)} found in project, not sure what to use");
//                     }
//                 }
//             }
//
// #if BL_ADDRESSABLES
//
//             if (UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings == null)
//             {
//                 throw new Exception("Addressable default group not setup");
//             }
//
//             var aaSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
//             var guid = AssetDatabase.AssetPathToGUID(path);
//
//             var entry = aaSettings.FindAssetEntry(guid) ?? aaSettings.CreateOrMoveEntry(guid, aaSettings.DefaultGroup);
//
//             var key = SettingsUtility.GetResourcePath(typeof(T)).RuntimeKey.ToString();
//
//             if (entry.address != key)
//             {
//                 Debug.Log($"Updating address of {typeof(T).Name}");
//                 entry.SetAddress(key);
//                 entry.parentGroup.SetDirty(
//                     UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryModified,
//                     new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> { entry },
//                     false);
//             }
// #endif
            // return instance;
        }

        // private static T MoveSettings<T>(string existingAsset, string path)
        //     where T : ScriptableObject, ISettings
        // {
        //     var existingPath = AssetDatabase.GUIDToAssetPath(existingAsset);
        //
        //     if (existingPath.StartsWith("Packages/"))
        //     {
        //         Debug.Log($"Found existing settings ({typeof(T)}) at {existingPath}, copying to {path} for use in project");
        //         UnityEditor.AssetDatabase.CopyAsset(existingPath, path);
        //     }
        //     else
        //     {
        //         Debug.Log($"Found existing settings ({typeof(T)}) at {existingPath}, moving to {path} for use in project");
        //         UnityEditor.AssetDatabase.MoveAsset(existingPath, path);
        //     }
        //
        //     return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        // }

        private static string GetResourcePath(Type type)
        {
            return string.Format($"{ResourceSettingsBasePath}", type.Name);
        }
    }
}