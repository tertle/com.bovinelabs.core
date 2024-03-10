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
            var iterateChildren = rootProperty.propertyType == SerializedPropertyType.Generic;

            if (this.Inline)
            {
                this.parent = new VisualElement();

                if (iterateChildren)
                {
                    // TODO indent?
                    this.parent.AddToClassList("unity-decorator-drawers-container");

                    var label = new Label(rootProperty.displayName);
                    label.AddToClassList("unity-header-drawer__label");
                    this.Parent.Add(label);
                }
            }
            else
            {
                this.parent = new Foldout { text = rootProperty.displayName };
                this.parent.AddToClassList("unity-collection-view");
                this.parent.AddToClassList("unity-list-view");
            }

            this.serializedObject = rootProperty.serializedObject;

            var createElements = this.PreElementCreation(this.parent);

            if (createElements)
            {
                if (iterateChildren)
                {
                    foreach (var property in SerializedHelper.GetChildren(rootProperty))
                    {
                        var element = this.CreateElement(property);
                        this.Parent.Add(element);
                    }
                }
                else
                {
                    var element = this.CreateElement(rootProperty);
                    this.Parent.Add(element);
                }
            }

            this.PostElementCreation(this.Parent, createElements);

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

        protected virtual bool PreElementCreation(VisualElement root)
        {
            return true;
        }

        protected virtual void PostElementCreation(VisualElement root, bool createdElements)
        {
        }

        /// <summary> Adds appropriate styles to make a label match the default <see cref="BaseField{TValueType}"/> alignment in an inspector. </summary>
        /// <param name="label"> The label to apply to. </param>
        protected static void AddLabelStyles(Label label)
        {
            label.AddToClassList(BaseField<string>.ussClassName);
            label.AddToClassList(BaseField<string>.labelUssClassName);
            label.AddToClassList(BaseField<string>.ussClassName + "__inspector-field");
            label.style.minHeight = new StyleLength(19); // bit gross but matches the element
        }
    }
}
