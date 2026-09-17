namespace BovineLabs.Core.Authoring.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class AuthoringSettingsUtility
    {
        [NoAutoStaticsCleanup]
        private static readonly Dictionary<Type, ISettings> CachedSettings = new();

        public static T GetSettings<T>()
            where T : ScriptableObject, ISettings
        {
            var type = typeof(T);

            if (CachedSettings.TryGetValue(type, out var cached) && cached as Object != null)
            {
                return (T)cached;
            }

            var result = TryGetSettings<T>(type, out var settings);
            if (!result)
            {
                throw new Exception($"Settings not found for {typeof(T)}, ensure they've been created by opening the settings window");
            }

            CachedSettings[type] = settings!;
            return settings!;
        }

        public static bool TryGetSettings<T>(out T settings)
            where T : ScriptableObject, ISettings
        {
            var type = typeof(T);
            return TryGetSettings(type, out settings);
        }

        private static bool TryGetSettings<T>(Type type, out T settings)
            where T : ScriptableObject, ISettings
        {
            var filter = type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}";
            var assets = AssetDatabase.FindAssets($"t:{filter}");

            if (assets.Length == 0)
            {
                settings = null;
                return false;
            }

            settings = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(assets.First()));
            return true;
        }
    }
}
