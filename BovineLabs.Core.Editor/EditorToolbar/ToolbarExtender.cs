// <copyright file="ToolbarExtender.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.EditorToolbar
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class ToolbarExtender
    {
        private static GUIStyle commandStyle;

        static ToolbarExtender()
        {
            ToolbarCallback.OnToolbarGUILeft = GUILeft;
            ToolbarCallback.OnToolbarGUIRight = GUIRight;
        }

        public static List<Action> LeftToolbarGUI { get; } = new();

        public static List<Action> RightToolbarGUI { get; } = new();

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
    }
}
