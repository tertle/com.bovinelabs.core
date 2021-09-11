// <copyright file="ReferenceDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Variables
{
    using BovineLabs.Core.Variables;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(Reference<,>), true)]
    public class ReferenceDrawer : PropertyDrawer
    {
        private const float ToggleWidth = 16;
        private const float Spacing = 2;

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var useVariable = property.FindPropertyRelative("useVariable");

            position.width -= EditorGUI.indentLevel * 15;

            var fieldRect = new Rect(position.x, position.y, position.width - ToggleWidth - Spacing, position.height);
            var useConstantRect = new Rect(position.x + position.width - ToggleWidth, position.y, ToggleWidth, position.height);

            if (useVariable.boolValue)
            {
                EditorGUI.PropertyField(fieldRect, property.FindPropertyRelative("variable"), label);
            }
            else
            {
                EditorGUI.PropertyField(fieldRect, property.FindPropertyRelative("constantValue"), label);
            }

            EditorGUI.PropertyField(useConstantRect, useVariable, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}