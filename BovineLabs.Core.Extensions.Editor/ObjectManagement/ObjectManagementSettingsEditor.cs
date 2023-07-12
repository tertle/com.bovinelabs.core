// <copyright file="ObjectManagementSettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.ObjectManagement;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ObjectManagementSettings))]
    public class ObjectManagementSettingsEditor : ElementEditor
    {
        /// <inheritdoc />
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            return property.name switch
            {
                "objectDefinitions" => new ObjectDefinitionAssetCreator(this.serializedObject, property).Element,
                "objectGroups" => new ObjectGroupAssetCreator(this.serializedObject, property).Element,
                _ => base.CreateElement(property),
            };
        }

        private class ObjectDefinitionAssetCreator : AssetCreator<ObjectDefinition>
        {
            public ObjectDefinitionAssetCreator(SerializedObject serializedObject, SerializedProperty serializedProperty)
                : base(serializedObject, serializedProperty, "object.definitions", "Assets/Configs/Definitions", "Definition.asset")
            {
            }
        }

        private class ObjectGroupAssetCreator : AssetCreator<ObjectGroup>
        {
            public ObjectGroupAssetCreator(SerializedObject serializedObject, SerializedProperty serializedProperty)
                : base(serializedObject, serializedProperty, "object.groups", "Assets/Configs/Groups", "Group.asset")
            {
            }
        }
    }
}
#endif
