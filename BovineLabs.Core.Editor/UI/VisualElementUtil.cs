// <copyright file="VisualElementUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.UIElements;

    public static class VisualElementUtil
    {
        public static EditorWindow GetEditorWindow(VisualElement ve)
        {
            var ownerObject = ve.panel?.GetType().GetProperty("ownerObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            Assert.IsNotNull(ownerObject);
            var dockArea = ownerObject!.GetValue(ve.panel);
            var actualView = dockArea?.GetType().GetProperty("actualView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

            Assert.IsNotNull(actualView);
            return (EditorWindow)actualView!.GetValue(dockArea);
        }

        public static Vector2 GetScreenPosition(VisualElement ve)
        {
            var editorWindow = GetEditorWindow(ve);
            return editorWindow.position.position + ve.worldBound.position;
        }
    }
}
