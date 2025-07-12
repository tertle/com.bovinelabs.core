// <copyright file="SubSceneEditorSet.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Authoring.SubScenes
{
    using BovineLabs.Core.ObjectManagement;

    [AutoRef(nameof(SubSceneSettings), nameof(SubSceneSettings.EditorSceneSets), nameof(SubSceneEditorSet), "Scenes/Editor", createNull:false)]
    public class SubSceneEditorSet : SubSceneSetBase
    {
    }
}
#endif
