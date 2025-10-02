// <copyright file="ComponentFieldAssetEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Component
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ComponentFieldAsset))]
    public class ComponentFieldAssetEditor : ElementEditor
    {
        private readonly List<string> fieldNames = new();

        private SerializedProperty? componentProperty;
        private SerializedProperty? fieldNameProperty;

        private PropertyField? componentField;
        private DropdownField? fieldNameField;

        /// <inheritdoc/>
        protected override VisualElement? CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "component":
                    this.componentProperty = property;
                    return this.componentField = CreatePropertyField(property);
                case "fieldName":

                    this.fieldNameProperty = property;
                    this.fieldNameField = new DropdownField { label = this.fieldNameProperty.displayName };

                    this.fieldNameField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
                    this.fieldNameField.RegisterValueChangedCallback(this.FieldNameChanged);

                    return this.fieldNameField;
            }

            return base.CreateElement(property);
        }

        /// <inheritdoc/>
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.SetupDropDown();

            this.componentField!.RegisterValueChangeCallback(_ => this.SetupDropDown());
        }

        private void FieldNameChanged(ChangeEvent<string> evt)
        {
            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                return;
            }

            this.fieldNameProperty!.stringValue = evt.newValue;
            this.fieldNameProperty.serializedObject.ApplyModifiedProperties();
        }

        private void SetupDropDown()
        {
            this.fieldNames.Clear();

            var componentAsset = this.componentProperty!.objectReferenceValue as ComponentAssetBase;
            if (componentAsset)
            {
                try
                {
                    var type = componentAsset.GetComponentType();
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        this.fieldNames.Add(field.Name);
                    }
                }
                catch (InvalidCastException)
                {
                }
            }

            this.fieldNameField!.choices = this.fieldNames;
            this.fieldNameField.value = this.fieldNameProperty!.stringValue;
        }
    }
}
