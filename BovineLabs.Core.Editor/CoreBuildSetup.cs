// <copyright file="CoreBuildSetup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using System.Linq;
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
            IncludeSettingsSingleton();
        }

        /// <inheritdoc/>
        public void OnPostprocessBuild(BuildReport report)
        {
            Revert();
        }

        private static void IncludeSettingsSingleton()
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();

            var kSettings = AssetDatabase.FindAssets($"t:{nameof(SettingsSingleton)}");

            foreach (var guid in kSettings)
            {
                var asset = AssetDatabase.LoadAssetAtPath<SettingsSingleton>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && asset.IncludeInBuild)
                {
                    preloadedAssets.Add(asset);
                }
            }

            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }

        private static void Revert()
        {
            // Revert back to original state by removing all SettingsSingleton from preloaded assets.
            PlayerSettings.SetPreloadedAssets(PlayerSettings.GetPreloadedAssets().Where(x => x is not SettingsSingleton).ToArray());
        }
    }
}