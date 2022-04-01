// <copyright file="EditorSettingsBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System.Collections.Generic;
    using BovineLabs.Core.Settings;
    using Sirenix.OdinInspector;
    using UnityEditor;
    using UnityEngine;

    public abstract class EditorSettingsBase : ScriptableObject, ISettings
    {
        public const string DefaultSettingsDirectory = "Assets/Configs/Settings/";

        [Title("Settings")]
        [SerializeField]
        private string settingsPath = DefaultSettingsDirectory;

        [SerializeField]
        private GameObject corePrefab;

        [SerializeField]
        private SceneAsset[] settingSubScenes;

        public string SettingsPath => this.settingsPath;

        public IEnumerable<SceneAsset> SettingSubScenes => this.settingSubScenes;

        public GameObject CorePrefab => this.corePrefab;
    }
}
