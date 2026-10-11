namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
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
            var changed = didDomainReload;
            var inventoryChanged = false;
            foreach (var path in importedAssets)
            {
                CheckImportedAsset(path, ref changed, ref inventoryChanged);
            }

            foreach (var path in movedAssets)
            {
                CheckImportedAsset(path, ref changed, ref inventoryChanged);
            }

            foreach (var path in deletedAssets)
            {
                if (SettingsAssetCache.IsTrackedPath(path))
                {
                    changed = true;
                    inventoryChanged = true;
                }
            }

            foreach (var path in movedFromAssetPaths)
            {
                if (SettingsAssetCache.IsTrackedPath(path))
                {
                    changed = true;
                    inventoryChanged = true;
                }
            }

            if (inventoryChanged)
            {
                SettingsAssetCache.Invalidate();
            }

            if (changed)
            {
                EditorSettingsUtility.ClearCachedSettings();
                revision++;
            }
        }

        private static void CheckImportedAsset(string path, ref bool changed, ref bool inventoryChanged)
        {
            if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == null || typeof(ISettings).IsAssignableFrom(type) || SettingsAssetCache.IsTrackedPath(path))
                {
                    changed = true;
                    inventoryChanged |= !SettingsAssetCache.HasSameAsset(path, type);
                }

                return;
            }

            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            changed |= !prefab || prefab.GetComponent<SettingsAuthoring>() || SettingsAssetCache.IsTrackedPath(path);
        }
    }
}
