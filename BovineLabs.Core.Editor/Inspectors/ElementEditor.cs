// <copyright file="ElementEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Editor.Helpers;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    /// <summary> Provides a custom editor ([CustomEditor(typeof(T))]) with custom element but will fall back to PropertyField if not overriden. </summary>
    public abstract class ElementEditor : Editor
    {
        private VisualElement? parent;

        protected VisualElement Parent => this.parent!;

        protected virtual bool IncludeScript => true;

        public sealed override VisualElement CreateInspectorGUI()
        {
            this.parent = new VisualElement();

            this.PreElementCreation(this.parent);

            foreach (var property in SerializedHelper.IterateAllChildren(this.serializedObject, this.IncludeScript))
            {
                VisualElement element;

                if (this.IncludeScript && property.propertyPath == "m_Script")
                {
                    element = CreatePropertyField(property, this.serializedObject);
                    element.SetEnabled(false);
                }
                else
                {
                    element = this.CreateElement(property);
                }

                this.Parent.Add(element);
            }

            this.PostElementCreation(this.Parent);

            return this.Parent;
        }

        protected static PropertyField CreatePropertyField(SerializedProperty property, SerializedObject serializedObject)
        {
            var field = new PropertyField(property);
            field.name = "PropertyField:" + property.propertyPath;
            field.Bind(serializedObject);
            return field;
        }

        protected static PropertyField CreatePropertyField(SerializedProperty property)
        {
            return CreatePropertyField(property, property.serializedObject);
        }

        protected virtual VisualElement CreateElement(SerializedProperty property)
        {
            return CreatePropertyField(property, this.serializedObject);
        }

        protected virtual void PreElementCreation(VisualElement root)
        {
        }

        protected virtual void PostElementCreation(VisualElement root)
        {
        }
    }
}
