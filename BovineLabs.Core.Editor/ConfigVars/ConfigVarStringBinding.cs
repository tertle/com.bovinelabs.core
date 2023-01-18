// <copyright file="ConfigVarStringBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using BovineLabs.Core.ConfigVars;
    using UnityEditor;
    using UnityEngine.UIElements;

    internal class ConfigVarStringBinding : IConfigVarBinding<string>
    {
        private readonly BaseField<string> baseField;
        private readonly ConfigVarAttribute attribute;

        private bool hasFocus;

        public ConfigVarStringBinding(BaseField<string> baseField, ConfigVarAttribute attribute)
        {
            this.attribute = attribute;
            this.baseField = baseField;
            this.baseField.RegisterCallback<FocusInEvent>(this.GainFocus);
            this.baseField.RegisterCallback<FocusOutEvent>(this.LoseFocus);
        }

        public string Value => EditorPrefs.GetString(this.attribute.Name, this.attribute.DefaultValue);

        public void PreUpdate()
        {
        }

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
