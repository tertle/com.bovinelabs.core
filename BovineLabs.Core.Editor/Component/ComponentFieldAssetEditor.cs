namespace BovineLabs.Core.Editor.Component
{
    using System.Collections.Generic;
    using System.Reflection;
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ComponentFieldAsset))]
    public class ComponentFieldAssetEditor : ElementEditor
    {
        private readonly List<string> _fieldNames = new();

        private SerializedProperty _componentProperty;
        private SerializedProperty _fieldNameProperty;

        private PropertyField _componentField;
        private DropdownField _fieldNameField;

        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "_component":
                    _componentProperty = property;
                    return _componentField = CreatePropertyField(property);
                case "_fieldName":

                    _fieldNameProperty = property;
                    _fieldNameField = new DropdownField { label = _fieldNameProperty.displayName };

                    _fieldNameField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
                    _fieldNameField.RegisterValueChangedCallback(FieldNameChanged);

                    return _fieldNameField;
            }

            return base.CreateElement(property);
        }

        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            SetupDropDown();

            _componentField!.RegisterValueChangeCallback(_ => SetupDropDown());
        }

        private void FieldNameChanged(ChangeEvent<string> evt)
        {
            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                return;
            }

            _fieldNameProperty!.stringValue = evt.newValue;
            _fieldNameProperty.serializedObject.ApplyModifiedProperties();
        }

        private void SetupDropDown()
        {
            _fieldNames.Clear();

            var componentAsset = _componentProperty!.objectReferenceValue as ComponentAsset;
            if (componentAsset)
            {
                var type = componentAsset.ResolveType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    _fieldNames.Add(field.Name);
                }
            }

            _fieldNameField!.choices = _fieldNames;
            _fieldNameField.value = _fieldNameProperty!.stringValue;
        }
    }
}
