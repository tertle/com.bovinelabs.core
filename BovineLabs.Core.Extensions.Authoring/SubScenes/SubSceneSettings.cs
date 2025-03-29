// <copyright file="SubSceneSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Authoring.SubScenes
{
    using System.Collections.Generic;
    using BovineLabs.Core.Settings;
    using UnityEngine;

    [SettingsGroup("Core")]
    public class SubSceneSettings : ScriptableObject, ISettings
    {
        public List<SubSceneSet> SceneSets = new();
        public List<SubSceneEditorSet> EditorSceneSets = new();
    }
}
#endif
