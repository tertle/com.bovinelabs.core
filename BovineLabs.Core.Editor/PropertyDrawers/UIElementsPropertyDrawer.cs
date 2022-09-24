// <copyright file="UIElementsPropertyDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.PropertyDrawers
{
    using BovineLabs.Core.Editor.Helpers;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public abstract class UIElementsPropertyDrawer : PropertyDrawer
    {
        protected VisualElement Parent { get; private set; }

        protected virtual bool Inline { get; }

        public override VisualElement CreatePropertyGUI(SerializedProperty root)
        {
            if (this.Inline)
            {
                this.Parent = new VisualElement();
                var label = new Label(root.displayName);
                this.Parent.Add(label);
                // TODO indent?
            }
            else
            {
                this.Parent = new Foldout { text = root.displayName };
            }

            foreach (var property in SerializedHelper.GetChildren(root))
            {
                var element = this.CreateElement(property) ?? CreatePropertyField(property);
                this.Parent.Add(element);
            }

            this.PostElementCreation(this.Parent);

            return this.Parent;
        }

        protected virtual VisualElement CreateElement(SerializedProperty property)
        {
            return null;
        }

        protected virtual void PostElementCreation(VisualElement parent)
        {
        }

        protected static PropertyField CreatePropertyField(SerializedProperty property)
        {
            var field = new PropertyField(property);
            field.BindProperty(property);
            return field;
        }
    }
}
