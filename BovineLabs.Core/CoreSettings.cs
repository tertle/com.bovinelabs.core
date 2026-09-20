namespace BovineLabs.Core
{
    using BovineLabs.Core.Settings;
    using UnityEngine;
    using UnityEngine.UIElements;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [SettingsGroup("Core")]
    public sealed class CoreSettings : SettingsSingleton<CoreSettings>
    {
        [SerializeField]
        private StyleSheet themeStyleSheet;

        [SerializeField]
        [Tooltip("Include Core settings and their referenced assets in release players as well as development builds.")]
        private bool includeInReleaseBuild;

        public StyleSheet ThemeStyleSheet => this.themeStyleSheet;

        public bool IncludeInReleaseBuild => this.includeInReleaseBuild;

#if UNITY_EDITOR
        private void Reset()
        {
            this.themeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.bovinelabs.core/BovineLabs.Core/UI/Themes/BovineLabs.uss");
        }
#endif
    }
}
