// <copyright file="EditorToolbarInitialize.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using System;
    using System.Reflection;
    using BovineLabs.Core.Utility;
    using Unity;
    using UnityEditor;
    using UnityEngine.UIElements;

    [InitializeOnLoad]
    internal static class EditorToolbarInitialize
    {
        static EditorToolbarInitialize()
        {
            foreach (var m in ReflectionUtility.GetMethodsWithAttribute<EditorToolbarAttribute>())
            {
                if (!m.IsStatic)
                {
                    Debug.LogError($"{nameof(EditorToolbarAttribute)} is not on a static method");
                    continue;
                }

                if (m.ReturnType != typeof(VisualElement))
                {
                    Debug.LogError($"{nameof(EditorToolbarAttribute)} returns a method returning {nameof(VisualElement)}");
                    continue;
                }

                if (m.GetParameters().Length != 0)
                {
                    Debug.LogError($"{nameof(EditorToolbarAttribute)} must have a parameterless a method");
                    continue;
                }

                var ve = (VisualElement)m.Invoke(null, null);

                if (ve == null)
                {
                    continue;
                }

                var parent = m.GetCustomAttribute<EditorToolbarAttribute>().Position switch
                {
                    EditorToolbarPosition.RightLeft => EditorToolbar.RightLeftParent,
                    EditorToolbarPosition.RightCenter => EditorToolbar.RightCenterParent,
                    EditorToolbarPosition.RightRight => EditorToolbar.RightRightParent,
                    EditorToolbarPosition.LeftLeft => EditorToolbar.LeftLeftParent,
                    EditorToolbarPosition.LeftCenter => EditorToolbar.LeftCenterParent,
                    EditorToolbarPosition.LeftRight => EditorToolbar.LeftRightParent,
                    _ => throw new ArgumentOutOfRangeException(),
                };

                // Force a margin between things
                if (ve.style.marginLeft.keyword == StyleKeyword.Null)
                {
                    ve.style.marginLeft = 1;
                }

                if (ve.style.marginRight.keyword == StyleKeyword.Null)
                {
                    ve.style.marginRight = 1;
                }

                parent.Add(ve);
            }
        }
    }
}
