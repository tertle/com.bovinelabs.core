namespace BovineLabs.Core.Editor.Inspectors
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public sealed class ToggleOption : VisualElement
    {
        private readonly SerializedObject serializedObject;
        private readonly SerializedProperty toggleProperty;
        private readonly PropertyField valueField;
        private readonly Toggle toggle;
        private VisualElement valueInput;

        public ToggleOption(SerializedObject serializedObject, string toggleName, string valueName)
        {
            this.serializedObject = serializedObject;
            this.toggleProperty = serializedObject.FindProperty(toggleName);

            var valueProperty = serializedObject.FindProperty(valueName);

            if (this.toggleProperty == null || valueProperty == null)
            {
                this.Add(new HelpBox($"ToggleOption could not find properties '{toggleName}' or '{valueName}'.", HelpBoxMessageType.Error));
                return;
            }

            this.style.flexDirection = FlexDirection.Row;

            this.valueField = PropertyUtil.CreateProperty(valueProperty, serializedObject);
            this.valueField.style.flexGrow = 1;
            this.valueField.RegisterCallback<AttachToPanelEvent>(this.OnValueFieldAttached);
            this.Add(this.valueField);

            this.toggle = new Toggle
            {
                tooltip = this.toggleProperty.tooltip,
                value = this.toggleProperty.boolValue,
                style = { flexShrink = 0 },
            };

            this.toggle.RegisterValueChangedCallback(this.OnToggleChanged);
            this.Add(this.toggle);

            this.TrackPropertyValue(this.toggleProperty, this.OnTogglePropertyChanged);
            this.UpdateVisibility();
        }

        private void OnToggleChanged(ChangeEvent<bool> evt)
        {
            this.serializedObject.UpdateIfRequiredOrScript();
            this.toggleProperty.boolValue = evt.newValue;
            this.serializedObject.ApplyModifiedProperties();
            this.UpdateVisibility();
        }

        private void OnTogglePropertyChanged(SerializedProperty property)
        {
            this.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            this.serializedObject.UpdateIfRequiredOrScript();

            var isEnabled = this.toggleProperty.boolValue;
            this.toggle.SetValueWithoutNotify(isEnabled);
            this.toggle.showMixedValue = this.toggleProperty.hasMultipleDifferentValues;
            this.valueField.SetEnabled(isEnabled);

            if (this.valueInput != null)
            {
                ElementUtility.SetVisible(this.valueInput, isEnabled);
            }
        }

        private void OnValueFieldAttached(AttachToPanelEvent evt)
        {
            this.valueInput = this.valueField.Q(className: "unity-property-field__input");
            this.valueField.UnregisterCallback<AttachToPanelEvent>(this.OnValueFieldAttached);
            this.UpdateVisibility();
        }
    }
}
