namespace BovineLabs.Core.Editor.ConfigVars
{
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal class ConfigVarStringBinding<TS> : IConfigVarBinding<string>
        where TS : unmanaged
    {
        private readonly BaseField<string> _baseField;
        private readonly ConfigVarAttribute _attribute;
        private readonly IConfigVarContainer<TS> _container;
        private readonly ContextualMenuManipulator _contextMenuManipulator;

        private bool _hasFocus;

        public ConfigVarStringBinding(BaseField<string> baseField, ConfigVarAttribute attribute, SharedStatic<TS> sharedStatic)
        {
            _attribute = attribute;
            _baseField = baseField;
            _container = new ConfigVarSharedStaticStringContainer<TS>(sharedStatic);
            _contextMenuManipulator = new ContextualMenuManipulator(OnContextMenu);

            _baseField.RegisterCallback<FocusInEvent>(GainFocus);
            _baseField.RegisterCallback<FocusOutEvent>(LoseFocus);
            _baseField.AddManipulator(_contextMenuManipulator);

            _baseField.RegisterValueChangedCallback(evt =>
            {
                Value = evt.newValue;
                EditorPrefs.SetString(ConfigVarManager.GetEditorPrefsKey(attribute.Name), evt.newValue.ToString());
            });
        }

        public string Value
        {
            get => _container.StringValue;
            set => _container.StringValue = value;
        }

        public void PreUpdate()
        {
        }

        public void Update()
        {
            if (!_hasFocus)
            {
                var v = Value;
                if (!_baseField.value.Equals(v))
                {
                    _baseField.SetValueWithoutNotify(v);
                }
            }
        }

        public void Release()
        {
            _baseField.UnregisterCallback<FocusInEvent>(GainFocus);
            _baseField.UnregisterCallback<FocusOutEvent>(LoseFocus);
            _baseField.RemoveManipulator(_contextMenuManipulator);
        }

        private void GainFocus(FocusInEvent focus)
        {
            _hasFocus = true;
        }

        private void LoseFocus(FocusOutEvent focus)
        {
            _hasFocus = false;
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Copy Name", _ => GUIUtility.systemCopyBuffer = _attribute.Name);
            evt.menu.AppendAction("Copy Value", _ => GUIUtility.systemCopyBuffer = Value);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction(
                "Reset To Default",
                _ => ResetToDefault(),
                _ => _baseField.enabledSelf ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private void ResetToDefault()
        {
            EditorPrefs.DeleteKey(ConfigVarManager.GetEditorPrefsKey(_attribute.Name));
            _container.StringValue = _attribute.DefaultValue;
            _baseField.SetValueWithoutNotify(Value);
        }
    }
}
