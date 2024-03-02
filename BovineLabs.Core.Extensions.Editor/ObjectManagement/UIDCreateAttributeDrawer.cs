// <copyright file="UIDCreateAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.ObjectManagement;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(UIDCreateAttribute))]
    public class UIDCreateAttributeDrawer : PropertyDrawer
    {
        private Type? objectType;
        private string fileName = string.Empty;
        private PropertyField? propertyField;
        private SerializedProperty? serializedProperty;
        private ContextualMenuManipulator? manipulator;

        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            this.serializedProperty = property;
            this.propertyField = PropertyUtil.CreateProperty(property, property.serializedObject);

            var type = this.fieldInfo.FieldType;
            if (typeof(IUID).IsAssignableFrom(type))
            {
                this.objectType = type;
                this.fileName = this.fieldInfo.Name;
                this.manipulator = new ContextualMenuManipulator(this.MenuBuilder);
                this.propertyField.AddManipulator(this.manipulator);
            }

            return this.propertyField;
        }

        private void MenuBuilder(ContextualMenuPopulateEvent evt)
        {
            if (this.serializedProperty!.objectReferenceValue != null)
            {
                return;
            }

            evt.menu.AppendAction("Create", _ =>
            {
                if (!AssetCreator.TryGetDirectory(this.objectType!, this.fileName.FirstCharToUpper() + ".asset", out var path))
                {
                    return;
                }

                var instance = ScriptableObject.CreateInstance(this.objectType);
                AssetDatabase.CreateAsset(instance, AssetDatabase.GenerateUniqueAssetPath(path));
                this.serializedProperty!.objectReferenceValue = instance;
                this.serializedProperty.serializedObject.ApplyModifiedProperties();

                EditorGUIUtility.PingObject(instance);
            });
        }
    }
}
#endif
