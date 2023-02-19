// <copyright file="LoadsSubSceneAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.SubScenes
{
    using BovineLabs.Core.SubScenes;
    using Unity.Entities;
    using UnityEngine;

    public class LoadsSubSceneAuthoring : MonoBehaviour
    {
    }

    public class LoadsSubSceneBaker : Baker<LoadsSubSceneAuthoring>
    {
        public override void Bake(LoadsSubSceneAuthoring authoring)
        {
            this.AddComponent<LoadsSubScene>();
        }
    }
}
