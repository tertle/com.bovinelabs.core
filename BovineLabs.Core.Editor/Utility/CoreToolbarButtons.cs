// <copyright file="CoreToolbarButtons.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Utility
{
    using BovineLabs.Core.Editor.ConfigVars;
    using BovineLabs.Core.Editor.Settings;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;

    [UsedImplicitly]
    public static class CoreToolbarButtons
    {
        private const string SettingsPath = "BovineLabs/Settings";
        private const string ConfigVarsPath = "BovineLabs/ConfigVars";

        [MainToolbarElement(SettingsPath, defaultDockPosition = MainToolbarDockPosition.Left)]
        [UsedImplicitly]
        private static MainToolbarElement Settings()
        {
            var icon = EditorGUIUtility.IconContent("Settings").image as Texture2D;
            var content = new MainToolbarContent(icon, "Open BovineLabs Core Settings.");
            return new MainToolbarButton(content, SettingsWindow.OpenSettings);
        }

        [MainToolbarElement(ConfigVarsPath, defaultDockPosition = MainToolbarDockPosition.Left)]
        [UsedImplicitly]
        private static MainToolbarElement ConfigVars()
        {
            var icon = EditorGUIUtility.IconContent("VerticalLayoutGroup Icon").image as Texture2D;
            var content = new MainToolbarContent(icon, "Open BovineLabs Core ConfigVars.");
            return new MainToolbarButton(content, ConfigVarsWindow.OpenSettings);
        }
    }
}
