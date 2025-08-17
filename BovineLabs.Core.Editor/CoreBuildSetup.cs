// <copyright file="CoreBuildSetup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using System.Linq;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    public class CoreBuildSetup : BuildPlayerProcessor, IPostprocessBuildWithReport
    {
        /// <inheritdoc/>
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            Revert();
            IncludeKSettings();
        }

        /// <inheritdoc/>
        public void OnPostprocessBuild(BuildReport report)
        {
            Revert();
        }

        private static void IncludeKSettings()
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();

            var kSettings = AssetDatabase.FindAssets($"t:{nameof(SettingsSingleton)}");

            foreach (var guid in kSettings)
            {
                var asset = AssetDatabase.LoadAssetAtPath<SettingsSingleton>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    preloadedAssets.Add(asset);
                }
            }

            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }

        private static void Revert()
        {
            // Revert back to original state by removing all KSettings  from preloaded assets.
            PlayerSettings.SetPreloadedAssets(PlayerSettings.GetPreloadedAssets().Where(x => x is not SettingsSingleton).ToArray());
        }
    }
}