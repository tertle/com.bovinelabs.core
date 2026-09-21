namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Editor.Helpers;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEngine;
    using PackageInfo = UnityEditor.PackageManager.PackageInfo;

    public class EditorSettings : ScriptableObject, ISettings
    {
        public const string SettingsKey = "bl.settings";
        public const string DefaultSettingsDirectory = "Assets/Settings/Settings";
        public const string DefaultSettingsPrefabDirectory = "Assets/Settings/Prefabs";

        [SerializeField]
        private List<string> _scriptingDefineSymbols = new List<string>();

        [SerializeField]
        private KeyPath[] _paths = Array.Empty<KeyPath>();

        [Header("Settings")]
        [SerializeField]
        private SettingsAuthoring _defaultSettingsAuthoring;

        [Tooltip("Additional world settings routes loaded as fallbacks in edit mode. Default settings are always included.")]
        [SerializeField]
        private string[] _additionalEditorWorldSettings = { "client" };

        [SerializeField]
        private KeyAuthoring[] _settingAuthoring = { new() { World = "service" } };

        public IReadOnlyList<string> ScriptingDefineSymbols => _scriptingDefineSymbols;

        public SettingsAuthoring DefaultSettingsAuthoring => _defaultSettingsAuthoring;

        public IReadOnlyList<string> AdditionalEditorWorldSettings => _additionalEditorWorldSettings;

        public IReadOnlyList<KeyAuthoring> SettingsAuthorings => _settingAuthoring;

        public void GetOrAddPath(string key, ref string path)
        {
            var result = _paths.FirstOrDefault(k => k.Key.ToLower() == key);
            if (result == null)
            {
                var serializedObject = new SerializedObject(this);
                serializedObject.Update();

                var serializedProperty = serializedObject.FindProperty("_paths");

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

            authoring = _settingAuthoring.FirstOrDefault(k => k.World.ToLower() == world)?.Authoring;

#if !UNITY_NETCODE
            if (!authoring && world is "client" or "server")
            {
                authoring = this.defaultSettingsAuthoring;
            }
#endif

            return authoring;
        }

        public void EnsureDefines(IReadOnlyList<string> add, IReadOnlyList<string> remove = null)
        {
            bool changes = false;

            foreach (var d in add)
            {
                if (!_scriptingDefineSymbols.Contains(d))
                {
                    _scriptingDefineSymbols.Add(d);
                    changes = true;
                }
            }

            if (remove != null)
            {
                foreach (var d in remove)
                {
                    if (_scriptingDefineSymbols.Contains(d))
                    {
                        _scriptingDefineSymbols.Remove(d);
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

        internal void InitializeCreatedAsset()
        {
            var directory = DefaultSettingsPrefabDirectory;
            AssetDatabaseHelper.CreateDirectories(ref directory);

            _defaultSettingsAuthoring = GetOrCreateSettingsAuthoring(directory, "GameSettings");

            var authorings = new List<KeyAuthoring>
            {
                new() { World = "service", Authoring = GetOrCreateSettingsAuthoring(directory, "ServiceSettings") },
            };

            var hasNetcode = PackageInfo.FindForPackageName("com.unity.netcode") != null;
            if (hasNetcode)
            {
                authorings.Add(new KeyAuthoring { World = "server", Authoring = GetOrCreateSettingsAuthoring(directory, "ServerSettings") });
                authorings.Add(new KeyAuthoring { World = "client", Authoring = GetOrCreateSettingsAuthoring(directory, "ClientSettings") });
            }

            authorings.Add(new KeyAuthoring { World = "menu", Authoring = GetOrCreateSettingsAuthoring(directory, "MenuSettings") });

            _settingAuthoring = authorings.ToArray();

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSettingsUtility.UpdateSettings(this);
        }

        private static SettingsAuthoring GetOrCreateSettingsAuthoring(string directory, string name)
        {
            var path = $"{directory}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing)
            {
                var existingAuthoring = existing.GetComponent<SettingsAuthoring>();
                if (!existingAuthoring)
                {
                    throw new InvalidOperationException($"Settings prefab '{path}' must have {nameof(SettingsAuthoring)} on its root.");
                }

                return existingAuthoring;
            }

            var instance = new GameObject(name);
            try
            {
                instance.AddComponent<SettingsAuthoring>();
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
                if (!prefab)
                {
                    throw new InvalidOperationException($"Could not create settings prefab '{path}'.");
                }

                return prefab.GetComponent<SettingsAuthoring>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
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
