// <copyright file="SettingsSingleton.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEngine;

    [Serializable]
    public abstract class SettingsSingleton : ScriptableObject, ISettings
    {
        // Simple helper for setting up singletons
        protected static T GetSingleton<T>(ref T field)
            where T : SettingsSingleton
        {
            if (!field)
            {
                field = CreateInstance<T>();
            }

            return field;
        }

        protected abstract void Initialize();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void LoadAll()
        {
#if UNITY_EDITOR
            var settingsGuids = AssetDatabase.FindAssets($"t:{nameof(SettingsSingleton)} a:all");

            foreach (var guid in settingsGuids)
            {
                var settingsPath = AssetDatabase.GUIDToAssetPath(guid);
                var setting = AssetDatabase.LoadAssetAtPath<SettingsSingleton>(settingsPath);
#else
            var kvSettings = Resources.FindObjectsOfTypeAll<SettingsSingleton>();
            foreach (var setting in kvSettings)
            {
#endif
                setting.Initialize();
            }
        }

#if UNITY_EDITOR

        [InitializeOnLoadMethod]
        private static void InitializeInEditor()
        {
            LoadAll();
        }
#endif
    }
}
