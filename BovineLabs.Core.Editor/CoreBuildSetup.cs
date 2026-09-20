namespace BovineLabs.Core.Editor
{
    using System.Linq;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEditor.Build;

    public class CoreBuildSetup : IPreprocessBuildWithContext, IPostprocessBuildWithContext
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildCallbackContext context)
        {
            if (!context.IsPlayerBuild)
            {
                return;
            }

            Revert();
            IncludeSettingsSingleton((context.Report.summary.options & BuildOptions.Development) != 0);
        }

        public void OnPostprocessBuild(BuildCallbackContext context)
        {
            if (!context.IsPlayerBuild)
            {
                return;
            }

            Revert();
        }

        private static void IncludeSettingsSingleton(bool developmentBuild)
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();

            var kSettings = AssetDatabase.FindAssets($"t:{nameof(SettingsSingleton)}");

            foreach (var guid in kSettings)
            {
                var asset = AssetDatabase.LoadAssetAtPath<SettingsSingleton>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset is CoreSettings coreSettings && !developmentBuild && !coreSettings.IncludeInReleaseBuild)
                {
                    continue;
                }

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
