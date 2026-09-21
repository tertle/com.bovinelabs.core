namespace BovineLabs.Core.Editor.Inspectors
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public sealed class ToggleOption : VisualElement
    {
        private readonly SerializedObject _serializedObject;
        private readonly SerializedProperty _toggleProperty;
        private readonly PropertyField _valueField;
        private readonly Toggle _toggle;
        private VisualElement _valueInput;

        public ToggleOption(SerializedObject serializedObject, string toggleName, string valueName)
        {
            _serializedObject = serializedObject;
            _toggleProperty = serializedObject.FindProperty(toggleName);

            var valueProperty = serializedObject.FindProperty(valueName);

            if (_toggleProperty == null || valueProperty == null)
            {
                Add(new HelpBox($"ToggleOption could not find properties '{toggleName}' or '{valueName}'.", HelpBoxMessageType.Error));
                return;
            }

            style.flexDirection = FlexDirection.Row;

            _valueField = PropertyUtil.CreateProperty(valueProperty, serializedObject);
            _valueField.style.flexGrow = 1;
            _valueField.RegisterCallback<AttachToPanelEvent>(OnValueFieldAttached);
            Add(_valueField);

            _toggle = new Toggle
            {
                tooltip = _toggleProperty.tooltip,
                value = _toggleProperty.boolValue,
                style = { flexShrink = 0 },
            };

            _toggle.RegisterValueChangedCallback(OnToggleChanged);
            Add(_toggle);

            this.TrackPropertyValue(_toggleProperty, OnTogglePropertyChanged);
            UpdateVisibility();
        }

        private void OnToggleChanged(ChangeEvent<bool> evt)
        {
            _serializedObject.UpdateIfRequiredOrScript();
            _toggleProperty.boolValue = evt.newValue;
            _serializedObject.ApplyModifiedProperties();
            UpdateVisibility();
        }

        private void OnTogglePropertyChanged(SerializedProperty property)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            var isEnabled = _toggleProperty.boolValue;
            _toggle.SetValueWithoutNotify(isEnabled);
            _toggle.showMixedValue = _toggleProperty.hasMultipleDifferentValues;
            _valueField.SetEnabled(isEnabled);

            if (_valueInput != null)
            {
                ElementUtility.SetVisible(_valueInput, isEnabled);
            }
        }

        private void OnValueFieldAttached(AttachToPanelEvent evt)
        {
            _valueInput = _valueField.Q(className: "unity-property-field__input");
            _valueField.UnregisterCallback<AttachToPanelEvent>(OnValueFieldAttached);
            UpdateVisibility();
        }
    }
}
