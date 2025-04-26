// <copyright file="AssetSet.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Authoring.SubScenes
{
    using System.Collections.Generic;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.SubScenes;
    using UnityEngine;

    [AutoRef(nameof(SubSceneSettings), nameof(SubSceneSettings.AssetSets))]
    public class AssetSet : ScriptableObject
    {
        public List<GameObject> Assets = new();

        public SubSceneLoadFlags TargetWorld = SubSceneLoadFlags.Service;
    }
}
#endif
