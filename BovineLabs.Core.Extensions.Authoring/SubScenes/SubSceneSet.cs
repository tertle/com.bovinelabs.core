// <copyright file="SubSceneSet.cs" company="BovineLabs">
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

    [AutoRef(nameof(SubSceneSettings), nameof(SubSceneSettings.SceneSets))]
    public class SubSceneSet : ScriptableObject, IUID
    {
        public int ID;

        public List<SceneAsset> Scenes = new();

#if UNITY_NETCODE
        public SubSceneLoadFlags TargetWorld = SubSceneLoadFlags.Game | SubSceneLoadFlags.Client | SubSceneLoadFlags.Server;
#else
        public SubSceneLoadFlags TargetWorld = SubSceneLoadFlags.Game;
#endif

        public bool IsRequired;
        public bool WaitForLoad = true;
        public bool AutoLoad;

        /// <inheritdoc/>
        int IUID.ID
        {
            get => this.ID;
            set => this.ID = value;
        }
    }
}
#endif
