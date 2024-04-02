// <copyright file="ObjectDefinitionInspector.cs" company="BovineLabs">
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
                "prefab" => this.CreatePrefab(property),
                "categories" => this.CreateCategories(property),
                _ => CreatePropertyField(property),
            };
        }

        private VisualElement CreatePrefab(SerializedProperty property)
        {
            var prefabField = CreatePropertyField(property);
            prefabField.RegisterValueChangeCallback(PrefabFieldChanged);
            return prefabField;
        }

        private VisualElement CreateCategories(SerializedProperty property)
        {
            var categories = CreatePropertyField(property, this.serializedObject);
            categories.RegisterCallback<SerializedPropertyChangeEvent>(this.UpdateGroups);
            return categories;
        }

        private static void PrefabFieldChanged(SerializedPropertyChangeEvent evt)
        {
            var go = evt.changedProperty.objectReferenceValue as GameObject;
            if (go == null)
            {
                return;
            }

            var to = evt.changedProperty.serializedObject.targetObject as ObjectDefinition;
            if (to == null)
            {
                Debug.LogError("target is not a ObjectDefinition");
                return;
            }

            var authoring = go.GetComponent<ObjectDefinitionAuthoring>();
            if (authoring == null)
            {
                authoring = go.AddComponent<ObjectDefinitionAuthoring>();
                authoring.Definition = to;
            }
            else if (authoring.Definition != to)
            {
                Debug.LogError($"{to} and it's target prefab {authoring} don't match. This likely means it's being used in 2 places.");
                evt.changedProperty.objectReferenceValue = null;
            }
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
#endif
