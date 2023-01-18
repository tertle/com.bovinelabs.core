// <copyright file="ConfigVarBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using System;
    using BovineLabs.Core.ConfigVars;
    using UnityEditor;
    using UnityEngine.UIElements;

    internal class ConfigVarBinding<T> : IConfigVarBinding<T>
        where T : struct, IEquatable<T>
    {
        private readonly BaseField<T> baseField;
        private readonly ConfigVarAttribute attribute;

        private bool hasFocus;

        public ConfigVarBinding(BaseField<T> baseField, ConfigVarAttribute attribute)
        {
            this.baseField = baseField;
            this.attribute = attribute;

            this.baseField.RegisterCallback<FocusInEvent>(this.GainFocus);
            this.baseField.RegisterCallback<FocusOutEvent>(this.LoseFocus);
        }

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

        public T Value => (T)Convert.ChangeType(EditorPrefs.GetString(this.attribute.Name, this.attribute.DefaultValue), typeof(T));

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
