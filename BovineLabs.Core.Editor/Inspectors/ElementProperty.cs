namespace BovineLabs.Core.Editor.Inspectors
{
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.Helpers;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public abstract class ElementProperty : PropertyDrawer
    {
        private SerializedObject _serializedObject;
        private VisualElement _parent;

        [NoAutoStaticsCleanup]
        private static readonly Dictionary<SerializedProperty, object> Caches = new();

        protected enum ParentTypes : byte
        {
            Foldout,
            Label,
            None,
        }

        protected virtual ParentTypes ParentType { get; } = ParentTypes.Foldout;

        protected virtual bool SkipSingleRoot => false;

        protected virtual bool IterateChildren => true;

        protected VisualElement Parent => _parent!;

        protected SerializedObject SerializedObject => _serializedObject!;

        protected SerializedProperty RootProperty { get; private set; }

        public sealed override VisualElement CreatePropertyGUI(SerializedProperty rootProperty)
        {
            RootProperty = rootProperty;
            _serializedObject = rootProperty.serializedObject;

            var iterateChildren = IterateChildren && rootProperty.propertyType == SerializedPropertyType.Generic;

            switch (ParentType)
            {
                case ParentTypes.Label:
                    _parent = new VisualElement();

                    if (iterateChildren)
                    {
                        if (!rootProperty.displayName.StartsWith("Element "))
                        {
                            _parent.AddToClassList("unity-decorator-drawers-container");
                        }

                        var label = new Label(GetDisplayName(rootProperty));
                        label.AddToClassList("unity-header-drawer__label");
                        _parent.Add(label);
                    }

                    break;
                case ParentTypes.None:
                    _parent = new VisualElement();
                    break;

                case ParentTypes.Foldout:
                default:
                    _parent = new Foldout { text = GetDisplayName(rootProperty), tooltip = GetTooltip(rootProperty) };
                    _parent.AddToClassList("unity-collection-view");
                    _parent.AddToClassList("unity-list-view");
                    _parent.AddToClassList("unity-list-view__foldout-header");
                    break;
            }

            var createElements = PreElementCreation(_parent);

            if (createElements)
            {
                if (iterateChildren)
                {
                    foreach (var property in SerializedHelper.GetChildren(rootProperty, SkipSingleRoot))
                    {
                        var element = CreateElement(property);
                        if (element != null)
                        {
                            Parent.Add(element);
                        }
                    }
                }
                else
                {
                    var root = SkipSingleRoot && SerializedHelper.TryGetSingleChildRoot(rootProperty, out var singleChildRoot)
                        ? singleChildRoot
                        : rootProperty;

                    var element = CreateElement(root);

                    if (element is PropertyField pf)
                    {
                        pf.label = GetDisplayName(rootProperty);
                    }

                    if (element != null)
                    {
                        Parent.Add(element);
                    }
                }
            }

            PostElementCreation(Parent, createElements);

            return Parent;
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
            if (!Caches.TryGetValue(RootProperty!, out var cache))
            {
                Caches[RootProperty!] = cache = new T();
            }

            return (T)cache;
        }

        protected virtual string GetDisplayName(SerializedProperty property)
        {
            return property.displayName;
        }

        protected virtual string GetTooltip(SerializedProperty property)
        {
            return property.tooltip;
        }

        protected virtual VisualElement CreateElement(SerializedProperty property)
        {
            return CreatePropertyField(property, SerializedObject);
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
