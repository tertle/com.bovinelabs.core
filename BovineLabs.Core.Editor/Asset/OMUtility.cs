// <copyright file="OMUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Asset
{
    using System;
    using System.IO;
    using BovineLabs.Core.Asset;
    using BovineLabs.Core.Editor.Settings;
    using UnityEditor;
    using UnityEngine;

    public static class OMUtility
    {
        public static void CreateInstance(Type type, string path)
        {
            var instance = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(instance, AssetDatabase.GenerateUniqueAssetPath(path));
            EditorGUIUtility.PingObject(instance);
        }

        public static string GetDefaultPath(AutoRefAttribute attr)
        {
            var directory = GetDefaultPathWithoutFileName(attr);
            return Path.Combine(directory, attr.DefaultFileName);
        }

        public static string GetDefaultPathWithoutFileName(AutoRefAttribute attr)
        {
            return EditorSettingsUtility.GetAssetDirectory(attr.DirectoryKey, attr.DefaultDirectory)!;
        }
    }
}