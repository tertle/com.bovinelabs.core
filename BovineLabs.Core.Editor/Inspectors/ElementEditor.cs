namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Editor.Helpers;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public abstract class ElementEditor : Editor
    {
        private VisualElement _parent;

        protected VisualElement Parent => _parent!;

        protected virtual bool IncludeScript => true;

        protected bool MultiEditing => targets.Length > 1;

        public sealed override VisualElement CreateInspectorGUI()
        {
            _parent = new VisualElement();

            if (IncludeScript)
            {
                var scriptProperty = serializedObject.FindProperty("m_Script");
                var scriptElement = CreatePropertyField(scriptProperty, serializedObject);
                scriptElement.SetEnabled(false);
                Parent.Add(scriptElement);
            }

            var createElements = PreElementCreation(_parent);
            if (createElements)
            {
                foreach (var property in SerializedHelper.IterateAllChildren(serializedObject, false))
                {
                    var element = CreateElement(property);
                    if (element != null)
                    {
                        Parent.Add(element);
                    }
                }
            }

            PostElementCreation(Parent, createElements);

            return Parent;
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
            return CreatePropertyField(property, serializedObject);
        }

        protected virtual bool PreElementCreation(VisualElement root)
        {
            return true;
        }

        protected virtual void PostElementCreation(VisualElement root, bool createdElements)
        {
        }

        protected static Foldout CreateFoldout(string text, bool value = false)
        {
            var foldout = new Foldout { text = text };
            foldout.AddToClassList("unity-list-view__foldout-header");
            foldout.contentContainer.style.marginLeft = 0;
            foldout.Q<Toggle>().style.marginLeft = -12;
            foldout.value = value;
            return foldout;
        }
    }
}
