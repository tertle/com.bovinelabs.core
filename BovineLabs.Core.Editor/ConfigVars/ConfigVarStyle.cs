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
        public const int LabelWidth = 220;
        public const int IndentLevel = 0;

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
            var highlightTexture = new Texture2D(1, 1);
            highlightTexture.SetPixel(0, 0, Style.HighlightColor);
            highlightTexture.Apply();

            HighlightStyle = new GUIStyle { normal = { background = highlightTexture } };

            Style = EditorGUIUtility.isProSkin ? DarkTheme : LightTheme;
        }

        public static ThemeStyle Style { get; }

        public static GUIStyle None { get; } = new();

        public static GUIStyle HighlightStyle { get; }

        public struct ThemeStyle
        {
            public Color HighlightColor;
        }
    }
}