// <copyright file="InlineObjectProperty.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Editor.Helpers;
    using BovineLabs.Core.PropertyDrawers;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(InlineObjectAttribute))]
    public class InlineObjectProperty : PropertyDrawer
    {
        private VisualElement parent = null!;
        private ObjectField rootField = null!;
        private SerializedProperty rootProperty = null!;

        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            this.rootProperty = property;

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return new Label("InlineObjectAttribute can only be used on Objects");
            }

            this.parent = new VisualElement();
            this.rootField = new ObjectField(property.name)
            {
                objectType = property.GetFieldType(),
                value = property.objectReferenceValue,
            };

            this.parent.Add(this.rootField);

            this.rootField.AddToClassList(BaseField<Object>.alignedFieldUssClassName);
            this.rootField.RegisterValueChangedCallback(this.Callback);

            this.Rebuild();

            return this.parent;
        }

        private void Callback(ChangeEvent<Object> changeEvent)
        {
            this.rootProperty.objectReferenceValue = changeEvent.newValue;
            this.rootProperty.serializedObject.ApplyModifiedProperties();

            this.Rebuild();
        }

        private void Rebuild()
        {
            this.parent.Clear();
            this.parent.Add(this.rootField);

            if (this.rootProperty.objectReferenceValue == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(this.rootProperty.objectReferenceValue);

            foreach (var linkedProperty in SerializedHelper.IterateAllChildren(serializedObject, false))
            {
                var element = PropertyUtil.CreateProperty(linkedProperty, serializedObject);
                this.parent.Add(element);
            }
        }
    }
}
