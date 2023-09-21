// <copyright file="ObjectDefinitionInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ObjectManagement
{
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.Settings;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(ObjectDefinition))]
    public class ObjectDefinitionInspector : ElementEditor
    {
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            return property.name switch
            {
                "categories" => this.CreateCategories(property),
                _ => base.CreateElement(property),
            };
        }

        private VisualElement CreateCategories(SerializedProperty property)
        {
            var categories = CreatePropertyField(property, this.serializedObject);
            categories.RegisterCallback<SerializedPropertyChangeEvent>(this.UpdateGroups);
            return categories;
        }

        private void UpdateGroups(SerializedPropertyChangeEvent evt)
        {
            var settings = EditorSettingsUtility.GetSettings<ObjectManagementSettings>();

            foreach (var objectGroup in settings.ObjectGroups)
            {
                var groupSo = new SerializedObject(objectGroup);
                ObjectGroupUtil.UpdateGroup(groupSo, this.serializedObject);
            }
        }
    }
}
