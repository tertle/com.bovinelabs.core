namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Authoring.Settings;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEngine;

    internal sealed class EditorSettingsAssetPostprocessor : AssetPostprocessor
    {
        [NoAutoStaticsCleanup]
        private static uint revision;

        internal static uint Revision => revision;

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload || importedAssets.Any(IsSettingsAsset) || movedAssets.Any(IsSettingsAsset) ||
                deletedAssets.Any(IsSettingsPath) || movedFromAssetPaths.Any(IsSettingsPath))
            {
                revision++;
            }
        }

        private static bool IsSettingsAsset(string path)
        {
            if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                return type == null || typeof(SettingsBase).IsAssignableFrom(type) || type == typeof(EditorSettings);
            }

            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return !prefab || prefab.GetComponent<SettingsAuthoring>();
        }

        private static bool IsSettingsPath(string path)
        {
            return path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        }
    }
}
