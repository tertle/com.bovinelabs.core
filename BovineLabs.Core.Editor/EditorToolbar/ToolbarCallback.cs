// <copyright file="ToolbarCallback.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>
// Based off work from https://github.com/marijnz/unity-toolbar-extender

namespace BovineLabs.Core.Editor.EditorToolbar
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public static class ToolbarCallback
    {
        /// <summary>
        /// Callback for toolbar OnGUI method.
        /// </summary>
        public static Action OnToolbarGUI;

#if UNITY_2021_1_OR_NEWER
        public static Action OnToolbarGUILeft;

        public static Action OnToolbarGUIRight;
#endif

        private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static readonly Type GUIViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");
        private static readonly Type WindowBackendType = typeof(Editor).Assembly.GetType("UnityEditor.IWindowBackend");

        private static readonly PropertyInfo WindowBackend =
            GUIViewType.GetProperty("windowBackend", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly PropertyInfo ViewVisualTree =
            WindowBackendType.GetProperty("visualTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo IMGUIContainerOnGui =
            typeof(IMGUIContainer).GetField("m_OnGUIHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

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
#if UNITY_2021_1_OR_NEWER
					var root = currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
					var rawRoot = root.GetValue(currentToolbar);
					var mRoot = rawRoot as VisualElement;
					RegisterCallback("ToolbarZoneLeftAlign", OnToolbarGUILeft);
					RegisterCallback("ToolbarZoneRightAlign", OnToolbarGUIRight);

					void RegisterCallback(string root, Action cb) {
						var toolbarZone = mRoot.Q(root);

						var parent = new VisualElement()
						{
							style = {
								flexGrow = 1,
								flexDirection = FlexDirection.Row,
							}
						};
						var container = new IMGUIContainer();
						container.onGUIHandler += () => {
							cb?.Invoke();
						};
						parent.Add(container);
						toolbarZone.Add(parent);
					}
#else
                    var windowBackend = WindowBackend.GetValue(currentToolbar);

                    // Get it's visual tree
                    var visualTree = (VisualElement)ViewVisualTree.GetValue(windowBackend, null);


                    // Get first child which 'happens' to be toolbar IMGUIContainer
                    var container = (IMGUIContainer)visualTree[0];

                    // (Re)attach handler
                    var handler = (Action)IMGUIContainerOnGui.GetValue(container);
                    handler -= OnGUI;
                    handler += OnGUI;
                    IMGUIContainerOnGui.SetValue(container, handler);
#endif
                }
            }
        }

        private static void OnGUI()
        {
            var handler = OnToolbarGUI;
            handler?.Invoke();
        }
    }
}