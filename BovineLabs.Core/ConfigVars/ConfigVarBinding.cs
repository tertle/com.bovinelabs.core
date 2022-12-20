// <copyright file="ConfigVarBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using UnityEngine.UIElements;

    internal class ConfigVarBinding<T> : IBinding
        where T : struct, IEquatable<T>
    {
        private readonly BaseField<T> baseField;
        private readonly ConfigVarSharedStaticContainer<T> container;

        private bool hasFocus;

        public ConfigVarBinding(BaseField<T> baseField, ConfigVarSharedStaticContainer<T> container)
        {
            this.container = container;
            this.baseField = baseField;

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
                if (!this.baseField.value.Equals(this.container.DirectValue))
                {
                    this.baseField.SetValueWithoutNotify(this.container.DirectValue);
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
