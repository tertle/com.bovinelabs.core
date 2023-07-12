// <copyright file="ElementEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Editor.Helpers;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> Provides an editor with custom element but will fall back to PropertyField if not overriden. </summary>
    public abstract class ElementEditor : Editor
    {
        private VisualElement? parent;

        protected VisualElement Parent => this.parent!;

        public sealed override VisualElement CreateInspectorGUI()
        {
            this.parent = new VisualElement();

            foreach (var property in SerializedHelper.IterateAllChildren(this.serializedObject))
            {
                var element = this.CreateElement(property);
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
            return CreatePropertyField(property, this.serializedObject);
        }

        protected virtual void PostElementCreation(VisualElement root)
        {
        }
    }
}
