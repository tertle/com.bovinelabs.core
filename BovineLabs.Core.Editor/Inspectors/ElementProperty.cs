// <copyright file="ElementProperty.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{

    using BovineLabs.Core.Editor.Helpers;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    /// <summary> Provides an editor with custom element but will fall back to PropertyField if not overriden. </summary>
    public abstract class ElementProperty : PropertyDrawer
    {
        protected VisualElement Parent { get; private set; }

        protected SerializedObject SerializedObject { get; private set; }

        public override VisualElement CreatePropertyGUI(SerializedProperty rootProperty)
        {
            this.Parent = new VisualElement();

            this.SerializedObject = rootProperty.serializedObject;

            foreach (var property in SerializedHelper.GetChildren(rootProperty))
            {
                var element = this.CreateElement(property) ?? CreatePropertyField(property, this.SerializedObject);
                this.Parent.Add(element);
            }

            this.PostElementCreation(this.Parent);

            return this.Parent;
        }

        protected static PropertyField CreatePropertyField(SerializedProperty property, SerializedObject serializedObject)
        {
            var field = new PropertyField(property);
            field.Bind(serializedObject);
            return field;
        }

        protected virtual VisualElement CreateElement(SerializedProperty property)
        {
            return null;
        }

        protected virtual void PostElementCreation(VisualElement parent)
        {
        }
    }
}
