// <copyright file="ToolbarCallback.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.EditorToolbar
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public static class ToolbarCallback
    {
        public static Action OnToolbarGUILeft;
        public static Action OnToolbarGUIRight;

        private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");

        private static ScriptableObject currentToolbar;

        static ToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            // Relying on the fact that toolbar is ScriptableObject and gets deleted when layout changes
            if (currentToolbar == null)
            {
                // Find toolbar
                var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
                currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
                if (currentToolbar != null)
                {
                    var root = currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    var mRoot = (VisualElement)root!.GetValue(currentToolbar);
                    RegisterCallback(mRoot, "ToolbarZoneLeftAlign", OnToolbarGUILeft);
                    RegisterCallback(mRoot, "ToolbarZoneRightAlign", OnToolbarGUIRight);
                }
            }
        }

        private static void RegisterCallback(VisualElement elementRoot, string root, Action cb)
        {
            var toolbarZone = elementRoot.Q(root);

            var parent = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                },
            };
            var container = new IMGUIContainer();
            container.onGUIHandler += () => { cb?.Invoke(); };
            parent.Add(container);
            toolbarZone.Add(parent);
        }
    }
}
