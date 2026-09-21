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
        private VisualElement _parent = null!;
        private ObjectField _rootField = null!;
        private SerializedProperty _rootProperty = null!;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            _rootProperty = property;

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return new Label("InlineObjectAttribute can only be used on Objects");
            }

            _parent = new VisualElement();
            _rootField = new ObjectField(property.name)
            {
                objectType = property.GetFieldType(),
                value = property.objectReferenceValue,
            };

            _parent.Add(_rootField);

            _rootField.AddToClassList(BaseField<Object>.alignedFieldUssClassName);
            _rootField.RegisterValueChangedCallback(Callback);

            Rebuild();

            return _parent;
        }

        private void Callback(ChangeEvent<Object> changeEvent)
        {
            _rootProperty.objectReferenceValue = changeEvent.newValue;
            _rootProperty.serializedObject.ApplyModifiedProperties();

            Rebuild();
        }

        private void Rebuild()
        {
            _parent.Clear();
            _parent.Add(_rootField);

            if (_rootProperty.objectReferenceValue == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(_rootProperty.objectReferenceValue);

            foreach (var linkedProperty in SerializedHelper.IterateAllChildren(serializedObject, false))
            {
                var element = PropertyUtil.CreateProperty(linkedProperty, serializedObject);
                _parent.Add(element);
            }
        }
    }
}
