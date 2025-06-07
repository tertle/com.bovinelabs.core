// <copyright file="AutoRefUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System;
    using System.IO;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.ObjectManagement;
    using UnityEditor;
    using UnityEngine;

    public static class OMUtility
    {
        public static string GetDefaultPath(AutoRefAttribute attr)
        {
            var directory = EditorSettingsUtility.GetAssetDirectory(attr.DirectoryKey, attr.DefaultDirectory);
            return Path.Combine(directory, attr.DefaultFileName);
        }

        internal static string GetNullDefaultPath(AutoRefAttribute attr)
        {
            var directory = EditorSettingsUtility.GetAssetDirectory(attr.DirectoryKey, attr.DefaultDirectory);
            return Path.Combine(directory, $"Null{attr.DefaultFileName}");
        }

        public static void CreateInstance(Type type, string path)
        {
            var instance = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(instance, AssetDatabase.GenerateUniqueAssetPath(path));
            EditorGUIUtility.PingObject(instance);
        }
    }
}
