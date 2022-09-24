// <copyright file="ToolbarExtender.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>
// Based off work from https://github.com/marijnz/unity-toolbar-extender

namespace BovineLabs.Core.Editor.EditorToolbar
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class ToolbarExtender
    {
        public const float DropdownWidth = 80;
        private const float Space = 8;
        private const float LargeSpace = 25;
        private const float ButtonWidth = 32;
        private const float PlayPauseStopWidth = 140;

        private static readonly int ToolCount;
        private static GUIStyle commandStyle;

        static ToolbarExtender()
        {
            var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");

            const string fieldName = "k_ToolCount";

            var toolIcons = toolbarType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            ToolCount = toolIcons != null ? ((int)toolIcons.GetValue(null)) : 8;

            ToolbarCallback.OnToolbarGUI = OnGUI;
#if UNITY_2021_1_OR_NEWER
            ToolbarCallback.OnToolbarGUILeft = GUILeft;
            ToolbarCallback.OnToolbarGUIRight = GUIRight;
#endif
        }

        public static List<Action> LeftToolbarGUI { get; } = new();

        public static List<Action> RightToolbarGUI { get; } = new();

        private static void OnGUI()
        {
            // Create two containers, left and right
            // Screen is whole toolbar
            commandStyle ??= new GUIStyle("CommandLeft");

            var screenWidth = EditorGUIUtility.currentViewWidth;

            // Following calculations match code reflected from Toolbar.OldOnGUI()
            float playButtonsPosition = Mathf.RoundToInt((screenWidth - PlayPauseStopWidth) / 2);

            var leftRect = new Rect(0, 0, screenWidth, Screen.height);
            leftRect.xMin += Space; // Spacing left
            leftRect.xMin += ButtonWidth * ToolCount; // Tool buttons
            leftRect.xMin += Space; // Spacing between tools and pivot

            leftRect.xMin += 64 * 2; // Pivot buttons
            leftRect.xMax = playButtonsPosition;
            leftRect.xMax -= LargeSpace;

            var rightRect = new Rect(0, 0, screenWidth, Screen.height);
            rightRect.xMin = playButtonsPosition;
            rightRect.xMin += commandStyle.fixedWidth * 3; // Play buttons
            rightRect.xMin += LargeSpace;
            // rightRect.xMin += ButtonWidth; // Live link
            // rightRect.xMin += Space;
            rightRect.xMax = screenWidth;
            rightRect.xMax -= Space; // Spacing right
            rightRect.xMax -= DropdownWidth; // Layout
            rightRect.xMax -= Space; // Spacing between layout and layers
            rightRect.xMax -= DropdownWidth; // Layers
            rightRect.xMax -= Space; // Spacing between layers and account

            rightRect.xMax -= DropdownWidth; // Account
            rightRect.xMax -= Space; // Spacing between account and cloud
            rightRect.xMax -= ButtonWidth; // Cloud
            rightRect.xMax -= Space; // Spacing between cloud and collab
            rightRect.xMax -= 78; // Colab

            // Add spacing around existing controls
            leftRect.xMin += Space;
            leftRect.xMax -= Space;
            rightRect.xMin += Space;
            rightRect.xMax -= Space;

            // Add top and bottom margins
            leftRect.y = 4;
            leftRect.height = 22;
            rightRect.y = 4;
            rightRect.height = 22;

            if (leftRect.width > 0)
            {
                GUILayout.BeginArea(leftRect);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                foreach (var handler in LeftToolbarGUI)
                {
                    handler();
                }

                // GUILayout.Space(LargeSpace);
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            if (rightRect.width > 0)
            {
                GUILayout.BeginArea(rightRect);
                GUILayout.BeginHorizontal();

                foreach (var handler in RightToolbarGUI)
                {
                    handler();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

#if UNITY_2021_1_OR_NEWER
        private static void GUILeft()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in LeftToolbarGUI)
            {
                handler();
            }

            GUILayout.EndHorizontal();
        }

        private static void GUIRight()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in RightToolbarGUI)
            {
                handler();
            }

            GUILayout.EndHorizontal();
        }
#endif
    }
}