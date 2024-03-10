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

            if (this.IncludeScript)
            {
                var scriptProperty = this.serializedObject.FindProperty("m_Script");
                var scriptElement = CreatePropertyField(scriptProperty, this.serializedObject);
                scriptElement.SetEnabled(false);
                this.Parent.Add(scriptElement);
            }

            var createElements = this.PreElementCreation(this.parent);
            if (createElements)
            {
                foreach (var property in SerializedHelper.IterateAllChildren(this.serializedObject, this.IncludeScript))
                {
                    if (property.propertyPath == "m_Script")
                    {
                        continue;
                    }

                    var element = this.CreateElement(property);

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

        protected static PropertyField CreatePropertyField(SerializedProperty property)
        {
            return CreatePropertyField(property, property.serializedObject);
        }

        protected virtual VisualElement CreateElement(SerializedProperty property)
        {
            return CreatePropertyField(property, this.serializedObject);
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

        /// <summary> Create a foldout without margins so it lines up with the inspector listviews. </summary>
        /// <param name="text"> Text value of the foldout. </param>
        /// <returns> A new foldout. </returns>
        protected static Foldout CreateFoldout(string text)
        {
            var foldout = new Foldout { text = text };
            foldout.AddToClassList("unity-list-view__foldout-header");
            foldout.contentContainer.style.marginLeft = 0;
            foldout.value = false;
            return foldout;
        }
    }
}
