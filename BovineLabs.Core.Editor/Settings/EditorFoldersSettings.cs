// <copyright file="EditorFoldersSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEngine;

    public class EditorFoldersSettings : ScriptableObject, ISettings
    {
        public const string SettingsKey = "settings";
        public const string DefaultSettingsDirectory = "Assets/Configs/Settings/";

        public const string KSettingsKey = "ksettings";
        public const string DefaultKSettingsDirectory = "Assets/Configs/Settings/Resources";

        [SerializeField]
        private List<KeyPath> paths = new();

        public void GetOrAddPath(string key, ref string path)
        {
            var result = this.paths.FirstOrDefault(k => k.Key == key);
            if (result == null)
            {
                var serializedObject = new SerializedObject(this);
                serializedObject.Update();

                var serializedProperty = serializedObject.FindProperty("paths");

                var index = serializedProperty.arraySize;
                serializedProperty.InsertArrayElementAtIndex(index);
                var keyPath = serializedProperty.GetArrayElementAtIndex(index);
                keyPath.FindPropertyRelative("Key").stringValue = key;
                keyPath.FindPropertyRelative("Path").stringValue = path;

                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(this);
                return;
            }

            path = result.Path;
        }

        [Serializable]
        public class KeyPath
        {
            [InspectorReadOnly]
            public string Key = string.Empty;

            public string Path = string.Empty;
        }
    }
}
