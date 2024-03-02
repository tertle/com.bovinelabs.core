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
        private SerializedObject? serializedObject;
        private VisualElement? parent;

        protected virtual bool Inline { get; }

        protected VisualElement Parent => this.parent!;

        protected SerializedObject SerializedObject => this.serializedObject!;

        public override VisualElement CreatePropertyGUI(SerializedProperty rootProperty)
        {
            if (this.Inline)
            {
                this.parent = new VisualElement();
                this.parent.AddToClassList("unity-decorator-drawers-container");

                var label = new Label(rootProperty.displayName);
                label.AddToClassList("unity-header-drawer__label");
                this.Parent.Add(label);
                // TODO indent?
            }
            else
            {
                this.parent = new Foldout { text = rootProperty.displayName };
            }

            this.serializedObject = rootProperty.serializedObject;

            foreach (var property in SerializedHelper.GetChildren(rootProperty))
            {
                var element = this.CreateElement(property);
                this.Parent.Add(element);
            }

            this.PostElementCreation(this.Parent);

            return this.Parent;
        }

        protected static PropertyField CreatePropertyField(SerializedProperty property, SerializedObject serializedObject)
        {
            return PropertyUtil.CreateProperty(property, serializedObject);
        }

        protected virtual VisualElement CreateElement(SerializedProperty property)
        {
            return CreatePropertyField(property, this.SerializedObject);
        }

        protected virtual void PostElementCreation(VisualElement root)
        {
        }
    }
}
