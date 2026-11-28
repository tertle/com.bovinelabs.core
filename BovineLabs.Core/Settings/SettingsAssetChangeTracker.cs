#if UNITY_EDITOR
namespace BovineLabs.Core.Settings
{
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Experimental;

    internal sealed class SettingsAssetChangeTracker : AssetsModifiedProcessor
    {
        private const string RevisionKey = "BovineLabs.Core.SettingsAssetChangeTracker.Revision";

        internal static int Revision => SessionState.GetInt(RevisionKey, 0);

        protected override void OnAssetsModified(string[] changedAssets, string[] addedAssets, string[] deletedAssets, AssetMoveInfo[] movedAssets)
        {
            // Detect file changes before code can reload. Imports may subsequently change which assets are available.
            RecordChanges(changedAssets, deletedAssets, movedAssets.Length);
        }

        private static void RecordChanges(string[] changedAssets, string[] deletedAssets, int movedCount)
        {
            if (movedCount != 0 || changedAssets.Any(IsNonScriptAsset) || deletedAssets.Any(IsNonScriptAsset))
            {
                SessionState.SetInt(RevisionKey, unchecked(Revision + 1));
            }
        }

        private static bool IsNonScriptAsset(string path)
        {
            return !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class ImportChanges : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                RecordChanges(importedAssets, deletedAssets, movedAssets.Length);
            }
        }
    }
}
#endif
