// <copyright file="EditorToolbar.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public enum EditorToolbarPosition : byte
    {
        RightLeft,
        RightCenter,
        RightRight,
        LeftLeft,
        LeftCenter,
        LeftRight,
    }

    public static class EditorToolbar
    {
        private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject currentToolbar;
        private static bool isInitialized;

        static EditorToolbar()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            currentToolbar = ScriptableObject.CreateInstance<ScriptableObject>();

            LeftParent = CreateParentElement();
            RightParent = CreateParentElement();

            LeftParent.Add(LeftLeftParent = CreateSectionElement());
            LeftParent.Add(LeftCenterParent = CreateSectionElement());
            LeftParent.Add(LeftRightParent = CreateSectionElement());

            RightParent.Add(RightLeftParent = CreateSectionElement());
            RightParent.Add(RightCenterParent = CreateSectionElement());
            RightParent.Add(RightRightParent = CreateSectionElement());

            LeftCenterParent.style.justifyContent = Justify.Center;
            RightCenterParent.style.justifyContent = Justify.Center;

            LeftRightParent.style.justifyContent = Justify.FlexEnd;
            RightRightParent.style.justifyContent = Justify.FlexEnd;
        }

        public static VisualElement LeftParent { get; }

        public static VisualElement LeftLeftParent { get; }

        public static VisualElement LeftCenterParent { get; }

        public static VisualElement LeftRightParent { get; }

        public static VisualElement RightParent { get; }

        public static VisualElement RightLeftParent { get; }

        public static VisualElement RightCenterParent { get; }

        public static VisualElement RightRightParent { get; }

        private static void OnUpdate()
        {
            // Relying on the fact that toolbar is ScriptableObject and gets deleted when layout changes
            if (currentToolbar == null || !isInitialized)
            {
                CreateToolbar();
            }
        }

        private static void CreateToolbar()
        {
            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            if (toolbars.Length == 0)
            {
                return;
            }

            currentToolbar = (ScriptableObject)toolbars[0];
            var root = currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            var mRoot = (VisualElement)root!.GetValue(currentToolbar);

            mRoot.Q("ToolbarZoneLeftAlign").Add(LeftParent);
            mRoot.Q("ToolbarZoneRightAlign").Add(RightParent);
            isInitialized = true;
        }

        private static VisualElement CreateParentElement()
        {
            return new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                },
            };
        }

        private static VisualElement CreateSectionElement()
        {
            var ve = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                },
            };

            ve.AddToClassList("unity-editor-main-toolbar");
            ve.name = "PlayMode";
            return ve;
        }
    }
}
