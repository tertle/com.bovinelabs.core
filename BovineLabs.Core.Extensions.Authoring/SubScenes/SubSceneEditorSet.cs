// <copyright file="SubSceneEditorSet.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Authoring.SubScenes
{
    using System.Collections.Generic;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.SubScenes;
    using UnityEditor;
    using UnityEngine;

    [AutoRef(nameof(SubSceneSettings), nameof(SubSceneSettings.EditorSceneSets))]
    public class SubSceneEditorSet : ScriptableObject
    {
        public List<SceneAsset> Scenes = new();

#if UNITY_NETCODE
        public SubSceneLoadFlags TargetWorld = SubSceneLoadFlags.Game | SubSceneLoadFlags.Client | SubSceneLoadFlags.Server;
#else
        public SubSceneLoadFlags TargetWorld = SubSceneLoadFlags.Game;
#endif
    }
}
#endif
