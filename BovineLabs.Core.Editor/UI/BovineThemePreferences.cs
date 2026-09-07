// <copyright file="BovineThemePreferences.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using BovineLabs.Core.UI;
    using UnityEditor;
    using UnityEngine.UIElements;

    /// <summary>Persists the editor theme independently of each project's player preferences.</summary>
    [InitializeOnLoad]
    internal static class BovineThemePreferences
    {
        static BovineThemePreferences()
        {
            var saved = EditorPrefs.GetInt(BovineThemeUtility.PreferenceKey, (int)BovineTheme.BovineWorks);
            BovineThemeUtility.Theme = saved == (int)BovineTheme.Curator ? BovineTheme.Curator : BovineTheme.BovineWorks;
            BovineThemeUtility.ThemeChanged += Save;
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Preferences/BovineLabs/Appearance", SettingsScope.User)
            {
                label = "Appearance",
                keywords = new[] { "theme", "Bovine Works", "Curator", "BovineLabs", "UI" },
                activateHandler = (_, root) =>
                {
                    // Unity reuses root for other preference pages; only theme the content we own.
                    var content = new BovineThemeRoot();
                    content.AddToClassList(BovineThemeUtility.WindowClass);
                    content.AddToClassList("bl-appearance-preferences");
                    var title = new Label("BovineLabs appearance");
                    title.AddToClassList("bl-section-title");
                    content.Add(title);
                    content.Add(new BovineThemeSelector());
                    var help = new Label("Applies immediately to sample panels and tools that opt in. Players store their own theme preference.");
                    help.AddToClassList("bl-description");
                    content.Add(help);
                    root.Add(content);
                },
            };
        }

        private static void Save(BovineTheme theme)
        {
            EditorPrefs.SetInt(BovineThemeUtility.PreferenceKey, (int)theme);
        }
    }
}
