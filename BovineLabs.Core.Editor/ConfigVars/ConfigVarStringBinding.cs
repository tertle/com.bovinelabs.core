// <copyright file="ConfigVarStringBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using UnityEditor;
    using UnityEngine.UIElements;

    internal class ConfigVarStringBinding<TS> : IConfigVarBinding<string>
        where TS : unmanaged
    {
        private readonly BaseField<string> baseField;
        private readonly ConfigVarAttribute attribute;
        private readonly IConfigVarContainer<TS> container;

        private bool hasFocus;

        public ConfigVarStringBinding(BaseField<string> baseField, ConfigVarAttribute attribute, SharedStatic<TS> sharedStatic)
        {
            this.attribute = attribute;
            this.baseField = baseField;
            this.container = new ConfigVarSharedStaticStringContainer<TS>(sharedStatic);

            this.baseField.RegisterCallback<FocusInEvent>(this.GainFocus);
            this.baseField.RegisterCallback<FocusOutEvent>(this.LoseFocus);

            this.baseField.RegisterValueChangedCallback(evt =>
            {
                this.Value = evt.newValue;
                EditorPrefs.SetString(attribute.Name, evt.newValue.ToString());
            });
        }

        /// <inheritdoc/>
        public string Value
        {
            get => this.container.StringValue; // EditorPrefs.GetString(this.attribute.Name, this.attribute.DefaultValue);
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
        }

        private void GainFocus(FocusInEvent focus)
        {
            this.hasFocus = true;
        }

        private void LoseFocus(FocusOutEvent focus)
        {
            this.hasFocus = false;
        }
    }
}
