// <copyright file="ConfigVarStringBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        private readonly BaseField<string> baseField;
        private readonly ConfigVarAttribute attribute;
        private readonly IConfigVarContainer<TS> container;
        private readonly ContextualMenuManipulator contextMenuManipulator;

        private bool hasFocus;

        public ConfigVarStringBinding(BaseField<string> baseField, ConfigVarAttribute attribute, SharedStatic<TS> sharedStatic)
        {
            this.attribute = attribute;
            this.baseField = baseField;
            this.container = new ConfigVarSharedStaticStringContainer<TS>(sharedStatic);
            this.contextMenuManipulator = new ContextualMenuManipulator(this.OnContextMenu);

            this.baseField.RegisterCallback<FocusInEvent>(this.GainFocus);
            this.baseField.RegisterCallback<FocusOutEvent>(this.LoseFocus);
            this.baseField.AddManipulator(this.contextMenuManipulator);

            this.baseField.RegisterValueChangedCallback(evt =>
            {
                this.Value = evt.newValue;
                EditorPrefs.SetString(ConfigVarManager.GetEditorPrefsKey(attribute.Name), evt.newValue.ToString());
            });
        }

        /// <inheritdoc/>
        public string Value
        {
            get => this.container.StringValue;
            set => this.container.StringValue = value;
        }

        /// <inheritdoc/>
        public void PreUpdate()
        {
        }

        /// <inheritdoc/>
        public void Update()
        {
            if (!this.hasFocus)
            {
                var v = this.Value;
                if (!this.baseField.value.Equals(v))
                {
                    this.baseField.SetValueWithoutNotify(v);
                }
            }
        }

        /// <inheritdoc/>
        public void Release()
        {
            this.baseField.UnregisterCallback<FocusInEvent>(this.GainFocus);
            this.baseField.UnregisterCallback<FocusOutEvent>(this.LoseFocus);
            this.baseField.RemoveManipulator(this.contextMenuManipulator);
        }

        private void GainFocus(FocusInEvent focus)
        {
            this.hasFocus = true;
        }

        private void LoseFocus(FocusOutEvent focus)
        {
            this.hasFocus = false;
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Copy Name", _ => GUIUtility.systemCopyBuffer = this.attribute.Name);
            evt.menu.AppendAction("Copy Value", _ => GUIUtility.systemCopyBuffer = this.Value);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction(
                "Reset To Default",
                _ => this.ResetToDefault(),
                _ => this.baseField.enabledSelf ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private void ResetToDefault()
        {
            EditorPrefs.DeleteKey(ConfigVarManager.GetEditorPrefsKey(this.attribute.Name));
            this.container.StringValue = this.attribute.DefaultValue;
            this.baseField.SetValueWithoutNotify(this.Value);
        }
    }
}
