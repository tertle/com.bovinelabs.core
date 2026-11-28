namespace BovineLabs.Core.Authoring.Settings
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Settings;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEngine;

    public static class AuthoringSettingsUtility
    {
        [NoAutoStaticsCleanup]
        private static readonly Dictionary<Type, (uint Revision, ScriptableObject Asset)> CachedSettings = new();

        public static T GetSettings<T>()
            where T : ScriptableObject, ISettings
        {
            return GetSettings<T>(required: true);
        }

        /// <summary> Finds existing settings, returning null for a missing asset when it is optional. </summary>
        public static T GetSettings<T>(bool required)
            where T : ScriptableObject, ISettings
        {
            var type = typeof(T);
            var revision = AssetDatabase.GlobalArtifactDependencyVersion;
            T settings;
            if (CachedSettings.TryGetValue(type, out var cached) && cached.Revision == revision &&
                (cached.Asset is null || cached.Asset != null))
            {
                settings = (T)cached.Asset;
            }
            else
            {
                var filter = type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}";
                var assets = AssetDatabase.FindAssets($"t:{filter}");
                settings = assets.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(assets[0]));

                // An in-progress refresh can temporarily hide settings from the search.
                if (settings != null || (assets.Length == 0 && !EditorApplication.isUpdating))
                {
                    CachedSettings[type] = (revision, settings);
                }
            }

            if (required && settings == null)
            {
                throw new Exception($"Settings not found for {type}, ensure they've been created by opening the settings window");
            }

            return settings;
        }
    }
}
