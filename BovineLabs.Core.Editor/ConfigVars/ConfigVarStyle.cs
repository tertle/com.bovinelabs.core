// <copyright file="ConfigVarStyle.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using UnityEditor;
    using UnityEngine;

    // TODO REMOVE CHANGE TO USS
    public static class ConfigVarStyle
    {
        private static readonly ThemeStyle DarkTheme = new()
        {
            HighlightColor = new Color32(255, 255, 255, 32),
        };

        private static readonly ThemeStyle LightTheme = new()
        {
            HighlightColor = new Color32(0, 0, 0, 32),
        };

        static ConfigVarStyle()
        {
            Style = EditorGUIUtility.isProSkin ? DarkTheme : LightTheme;
        }

        public static ThemeStyle Style { get; }

        public struct ThemeStyle
        {
            public Color HighlightColor;
        }
    }
}
