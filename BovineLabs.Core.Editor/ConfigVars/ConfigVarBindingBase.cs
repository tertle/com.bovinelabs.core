namespace BovineLabs.Core.Editor.ConfigVars
{
    using System;
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal abstract class ConfigVarBindingBase<T> : IConfigVarBinding<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly BaseField<T> _baseField;
        private readonly ConfigVarAttribute _attribute;
        private readonly IConfigVarContainer<T> _container;
        private readonly ContextualMenuManipulator _contextMenuManipulator;

        private bool _hasFocus;

        protected ConfigVarBindingBase(BaseField<T> baseField, ConfigVarAttribute attribute, IConfigVarContainer<T> container)
        {
            _baseField = baseField;
            _attribute = attribute;
            _container = container;
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

        public T Value
        {
            get => _container.Value;
            set => _container.Value = value;
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

        private static IConfigVarContainer<T> CreateContainer(SharedStatic<T> sharedStatic)
        {
            if (typeof(T) == typeof(Color))
            {
                var container = new ConfigVarSharedStaticColorContainer((SharedStatic<Color>)(object)sharedStatic);
                return (IConfigVarContainer<T>)container;
            }

            if (typeof(T) == typeof(Vector4))
            {
                var container = new ConfigVarSharedStaticVector4Container((SharedStatic<Vector4>)(object)sharedStatic);
                return (IConfigVarContainer<T>)container;
            }

            if (typeof(T) == typeof(Rect))
            {
                var container = new ConfigVarSharedStaticRectContainer((SharedStatic<Rect>)(object)sharedStatic);
                return (IConfigVarContainer<T>)container;
            }

            return new ConfigVarSharedStaticContainer<T>(sharedStatic);
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Copy Name", _ => GUIUtility.systemCopyBuffer = _attribute.Name);
            evt.menu.AppendAction("Copy Value", _ => GUIUtility.systemCopyBuffer = Value.ToString());
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
