// <copyright file="ConfigVarBindingBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        private readonly BaseField<T> baseField;
        private readonly ConfigVarAttribute attribute;
        private readonly IConfigVarContainer<T> container;
        private readonly ContextualMenuManipulator contextMenuManipulator;

        private bool hasFocus;

        protected ConfigVarBindingBase(BaseField<T> baseField, ConfigVarAttribute attribute, IConfigVarContainer<T> container)
        {
            this.baseField = baseField;
            this.attribute = attribute;
            this.container = container;
            this.contextMenuManipulator = new ContextualMenuManipulator(this.OnContextMenu);

            this.baseField.RegisterCallback<FocusInEvent>(this.GainFocus);
            this.baseField.RegisterCallback<FocusOutEvent>(this.LoseFocus);
            this.baseField.AddManipulator(this.contextMenuManipulator);

            this.baseField.RegisterValueChangedCallback(evt =>
            {
                this.Value = evt.newValue;
                EditorPrefs.SetString(attribute.Name, evt.newValue.ToString());
            });
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
        public T Value
        {
            get => this.container.Value;
            set => this.container.Value = value;
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
            evt.menu.AppendAction("Copy Name", _ => GUIUtility.systemCopyBuffer = this.attribute.Name);
            evt.menu.AppendAction("Copy Value", _ => GUIUtility.systemCopyBuffer = this.Value.ToString());
            evt.menu.AppendSeparator();
            evt.menu.AppendAction(
                "Reset To Default",
                _ => this.ResetToDefault(),
                _ => this.attribute.IsReadOnly ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
        }

        private void ResetToDefault()
        {
            EditorPrefs.DeleteKey(this.attribute.Name);
            this.container.StringValue = this.attribute.DefaultValue;
            this.baseField.SetValueWithoutNotify(this.Value);
        }
    }
}
