// <copyright file="ObjectManagementSettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.Editor.Inspectors;
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
                "objectDefinitions" => new AssetCreator<ObjectDefinition>(this.serializedObject, property, "object.definitions", "Assets/Settings/Definitions",
                    "Definition.asset").Element,
                "objectGroups" => new AssetCreator<ObjectGroup>(this.serializedObject, property, "object.groups", "Assets/Settings/Groups", "Group.asset")
                    .Element,
                _ => CreatePropertyField(property),
            };
        }
    }
}
#endif
