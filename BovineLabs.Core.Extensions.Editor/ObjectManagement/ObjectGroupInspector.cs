// <copyright file="ObjectGroupInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.Settings;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ObjectGroup))]
    public class ObjectGroupInspector : ElementEditor
    {
        private VisualElement? summary;

        protected override VisualElement CreateElement(SerializedProperty property)
        {
            var element = CreatePropertyField(property, this.serializedObject);
            element.RegisterCallback<SerializedPropertyChangeEvent>(this.CreateSummary);

            if (property.name == "autoGroups")
            {
                element.RegisterCallback<SerializedPropertyChangeEvent>(this.UpdateGroups);
            }

            return element;
        }

        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.CreateSummary(null);
        }

        private void UpdateGroups(SerializedPropertyChangeEvent evt)
        {
            var settings = EditorSettingsUtility.GetSettings<ObjectManagementSettings>();

            foreach (var objectDefinition in settings.ObjectDefinitions)
            {
                var definitionSo = new SerializedObject(objectDefinition);
                ObjectGroupUtil.UpdateGroup(this.serializedObject, definitionSo);
            }
        }

        private void CreateSummary(SerializedPropertyChangeEvent? evt)
        {
            this.summary?.parent?.Remove(this.summary);

            var foldout = new Foldout
            {
                text = "Summary",
                value = true,
            };

            this.summary = foldout;

            var objectGroup = (ObjectGroup)this.target;
            foreach (var definition in objectGroup.GetAllDefinitions())
            {
                if (definition == null)
                {
                    continue;
                }

                var field = new ObjectField
                {
                    objectType = typeof(ObjectDefinition),
                    value = definition,
                };

                field.SetEnabled(false);
                foldout.Add(field);
            }

            this.Parent.Add(this.summary);
        }
    }
}
#endif
