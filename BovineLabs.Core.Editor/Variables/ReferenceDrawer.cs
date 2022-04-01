// <copyright file="ReferenceDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !ODIN_INSPECTOR
namespace BovineLabs.Core.Editor.Variables
{
    using BovineLabs.Core.Variables;
    using UnityEditor;
    using UnityEngine;

    /// <summary> The custom drawer for <see cref="Reference{TR,T}"/>. </summary>
    [CustomPropertyDrawer(typeof(Reference<,>), true)]
    public class ReferenceDrawer : PropertyDrawer
    {
        private const float Indent = 15;
        private const float ToggleWidth = 16;
        private const float Spacing = 2;

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var useVariable = property.FindPropertyRelative("useVariable");

            return useVariable.boolValue
                ? base.GetPropertyHeight(property, label)
                : EditorGUI.GetPropertyHeight(property.FindPropertyRelative("constantValue"));
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var useVariable = property.FindPropertyRelative("useVariable");

            position.width -= EditorGUI.indentLevel * Indent;

            var fieldRect = new Rect(position.x, position.y, position.width - ToggleWidth - Spacing, position.height);

            if (useVariable.boolValue)
            {
                EditorGUI.PropertyField(fieldRect, property.FindPropertyRelative("variable"), label);
            }
            else
            {
                EditorGUI.PropertyField(fieldRect, property.FindPropertyRelative("constantValue"), label, true);
            }

            var useConstantRect = new Rect(position.x + position.width - ToggleWidth, position.y, ToggleWidth, base.GetPropertyHeight(property, label));
            EditorGUI.PropertyField(useConstantRect, useVariable, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
#endif