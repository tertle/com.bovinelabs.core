// <copyright file="SubSceneSettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Editor.SubScenes
{
    using BovineLabs.Core.Authoring.SubScenes;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.ObjectManagement;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(SubSceneSettings))]
    public class SubSceneSettingsEditor : ElementEditor
    {
        /// <inheritdoc />
        protected override VisualElement? CreateElement(SerializedProperty property)
        {
            return property.name switch
            {
                nameof(SubSceneSettings.SceneSets) => new AssetCreator<SubSceneSet>(this.serializedObject, property, "bl.subscene-set",
                    "Assets/Settings/Scenes/", "SceneSet.asset").Element,
                nameof(SubSceneSettings.EditorSceneSets) => new AssetCreator<SubSceneEditorSet>(this.serializedObject, property, "bl.subscene-editor-set",
                    "Assets/Settings/Scenes/Editor", "EditorSceneSet.asset").Element,
                nameof(SubSceneSettings.AssetSets) => new AssetCreator<AssetSet>(this.serializedObject, property, "bl.subscene-asset-set",
                    "Assets/Settings/Scenes/Assets", "AssetSet.asset").Element,
                _ => base.CreateElement(property),
            };
        }
    }
}
#endif
