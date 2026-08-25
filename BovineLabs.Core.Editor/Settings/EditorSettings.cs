// <copyright file="EditorSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEngine;

    public class EditorSettings : ScriptableObject, ISettings
    {
        public const string SettingsKey = "bl.settings";
        public const string DefaultSettingsDirectory = "Assets/Settings/Settings";

        [SerializeField]
        private List<string> scriptingDefineSymbols = new List<string>();

        [SerializeField]
        private KeyPath[] paths = Array.Empty<KeyPath>();

        [Header("Settings")]
        [SerializeField]
        private SettingsAuthoring defaultSettingsAuthoring;

        [Tooltip("Additional world settings routes loaded as fallbacks in edit mode. Default settings are always included.")]
        [SerializeField]
        private string[] additionalEditorWorldSettings = { "client" };

        [SerializeField]
        private KeyAuthoring[] settingAuthoring = { new() { World = "service" } };

        public IReadOnlyList<string> ScriptingDefineSymbols => this.scriptingDefineSymbols;

        public SettingsAuthoring DefaultSettingsAuthoring => this.defaultSettingsAuthoring;

        public IReadOnlyList<string> AdditionalEditorWorldSettings => this.additionalEditorWorldSettings;

        public IReadOnlyList<KeyAuthoring> SettingsAuthorings => this.settingAuthoring;

        public void GetOrAddPath(string key, ref string path)
        {
            var result = this.paths.FirstOrDefault(k => k.Key.ToLower() == key);
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

        public bool TryGetAuthoring(string world, out SettingsAuthoring authoring)
        {
            world = world.ToLower();

            authoring = this.settingAuthoring.FirstOrDefault(k => k.World.ToLower() == world)?.Authoring;
            return authoring;
        }

        public void EnsureDefines(IReadOnlyList<string> add, IReadOnlyList<string> remove = null)
        {
            bool changes = false;

            foreach (var d in add)
            {
                if (!this.scriptingDefineSymbols.Contains(d))
                {
                    this.scriptingDefineSymbols.Add(d);
                    changes = true;
                }
            }

            if (remove != null)
            {
                foreach (var d in remove)
                {
                    if (this.scriptingDefineSymbols.Contains(d))
                    {
                        this.scriptingDefineSymbols.Remove(d);
                        changes = true;
                    }
                }
            }

            if (changes)
            {
                EditorUtility.SetDirty(this);
            }

            ScriptingDefineSymbolsEditor.ApplyDefinesToAll(add, remove ?? Array.Empty<string>());
        }

        [Serializable]
        public class KeyPath
        {
            [InspectorReadOnly]
            public string Key = string.Empty;

            public string Path = string.Empty;
        }

        [Serializable]
        public class KeyAuthoring
        {
            public string World = string.Empty;

            public SettingsAuthoring Authoring;
        }
    }
}
