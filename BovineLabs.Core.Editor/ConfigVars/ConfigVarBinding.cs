// <copyright file="ConfigVarBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using System;
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using UnityEditor;
    using UnityEngine.UIElements;

    internal class ConfigVarBinding<T> : IConfigVarBinding<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly BaseField<T> baseField;
        private readonly ConfigVarAttribute attribute;
        private readonly IConfigVarContainer<T> container;

        private bool hasFocus;

        public ConfigVarBinding(BaseField<T> baseField, ConfigVarAttribute attribute, SharedStatic<T> sharedStatic)
        {
            this.baseField = baseField;
            this.attribute = attribute;
            this.container = new ConfigVarSharedStaticContainer<T>(sharedStatic);

            this.baseField.RegisterCallback<FocusInEvent>(this.GainFocus);
            this.baseField.RegisterCallback<FocusOutEvent>(this.LoseFocus);

            this.baseField.RegisterValueChangedCallback(evt =>
            {
                this.Value = evt.newValue;
                EditorPrefs.SetString(attribute.Name, evt.newValue.ToString());
            });
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

        public T Value
        {
            get => this.container.Value; // (T)Convert.ChangeType(EditorPrefs.GetString(this.attribute.Name, this.attribute.DefaultValue), typeof(T));
            set => this.container.Value = value;
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
