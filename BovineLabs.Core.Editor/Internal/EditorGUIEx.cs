// <copyright file="EditorGUIEx.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    public static class EditorGUIEx
    {
        private static MethodInfo defaultPropertyField;

        public static bool DefaultPropertyField(Rect position, SerializedProperty property, GUIContent label)
        {
            if (defaultPropertyField == null)
            {
                defaultPropertyField = typeof(EditorGUI).GetMethod("DefaultPropertyField", BindingFlags.Static | BindingFlags.NonPublic);
                if (defaultPropertyField == null)
                {
                    Debug.LogError("EditorGUI.DefaultPropertyField signature changed");
                    return false;
                }
            }

            return (bool)defaultPropertyField.Invoke(null, new object[] { position, property, label });
        }
    }
}
