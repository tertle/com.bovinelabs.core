// <copyright file="SubSceneLoadAuthoringDataEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Editor.SubScenes
{
    using BovineLabs.Core.Authoring.SubScenes;
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(SubSceneLoadAuthoring.Data))]
    public class SubSceneLoadAuthoringDataEditor : ElementProperty
    {
        /// <inheritdoc />
        protected override string GetDisplayName(SerializedProperty property)
        {
            var asset = property.FindPropertyRelative(nameof(SubSceneLoadAuthoring.Data.SceneAsset)).objectReferenceValue;
            return asset == null ? "Null" : asset.name;
        }
    }
}
#endif
