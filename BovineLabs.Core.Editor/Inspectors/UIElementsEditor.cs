// <copyright file="UIElementsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Editor.Helpers;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public abstract class UIElementsEditor : Editor
    {
        protected VisualElement Parent { get; private set; }

        public override VisualElement CreateInspectorGUI()
        {
            this.Parent = new VisualElement();

            foreach (var property in SerializedHelper.IterateAllChildren(this.serializedObject))
            {
                var element = this.CreateElement(property) ?? CreatePropertyField(property, this.serializedObject);
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
