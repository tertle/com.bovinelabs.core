// <copyright file="ElementProperty.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.Helpers;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    /// <summary> Provides an inspector ([CustomPropertyDrawer(typeof(T))]) with custom element but will fall back to PropertyField if not overriden. </summary>
    public abstract class ElementProperty : PropertyDrawer
    {
        protected enum ParentTypes : byte
        {
            Foldout,
            Label,
            None,
        }

        private SerializedObject? serializedObject;
        private VisualElement? parent;

        private static readonly Dictionary<SerializedProperty, object> Caches = new();

        protected virtual ParentTypes ParentType { get; } = ParentTypes.Foldout;

        protected VisualElement Parent => this.parent!;

        protected SerializedObject SerializedObject => this.serializedObject!;

        protected SerializedProperty? RootProperty { get; private set; }

        public sealed override VisualElement CreatePropertyGUI(SerializedProperty rootProperty)
        {
            this.RootProperty = rootProperty;
            this.serializedObject = rootProperty.serializedObject;

            var iterateChildren = rootProperty.propertyType == SerializedPropertyType.Generic;

            switch (this.ParentType)
            {
                case ParentTypes.Label:
                    this.parent = new VisualElement();

                    if (iterateChildren)
                    {
                        if (!rootProperty.displayName.StartsWith("Element "))
                        {
                            this.parent.AddToClassList("unity-decorator-drawers-container");
                        }

                        var label = new Label(this.GetDisplayName(rootProperty));
                        label.AddToClassList("unity-header-drawer__label");
                        this.parent.Add(label);
                    }

                    break;
                case ParentTypes.None:
                    this.parent = new VisualElement();
                    break;

                case ParentTypes.Foldout:
                default:
                    this.parent = new Foldout { text = this.GetDisplayName(rootProperty) };
                    this.parent.AddToClassList("unity-collection-view");
                    this.parent.AddToClassList("unity-list-view");
                    break;
            }

            var createElements = this.PreElementCreation(this.parent);

            if (createElements)
            {
                if (iterateChildren)
                {
                    foreach (var property in SerializedHelper.GetChildren(rootProperty))
                    {
                        var element = this.CreateElement(property);
                        if (element != null)
                        {
                            this.Parent.Add(element);
                        }
                    }
                }
                else
                {
                    var element = this.CreateElement(rootProperty);
                    if (element != null)
                    {
                        this.Parent.Add(element);
                    }
                }
            }

            this.PostElementCreation(this.Parent, createElements);

            return this.Parent;
        }

        protected static PropertyField CreatePropertyField(SerializedProperty property)
        {
            return PropertyUtil.CreateProperty(property, property.serializedObject);
        }

        protected static PropertyField CreatePropertyField(SerializedProperty property, SerializedObject serializedObject)
        {
            return PropertyUtil.CreateProperty(property, serializedObject);
        }

        protected T Cache<T>()
            where T : class, new()
        {
            if (!Caches.TryGetValue(this.RootProperty!, out var cache))
            {
                Caches[this.RootProperty!] = cache = new T();
            }

            return (T)cache;
        }

        protected virtual string GetDisplayName(SerializedProperty property)
        {
            return property.displayName;
        }

        protected virtual VisualElement? CreateElement(SerializedProperty property)
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
    }
}
