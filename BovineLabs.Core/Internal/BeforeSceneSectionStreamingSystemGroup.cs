// <copyright file="BeforeSceneSectionStreamingSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities;
    using Unity.Scenes;

    [UpdateBefore(typeof(SceneSectionStreamingSystem))]
    [UpdateAfter(typeof(ResolveSceneReferenceSystem))]
    [UpdateInGroup(typeof(SceneSystemGroup))]
    public partial class BeforeSceneSectionStreamingSystemGroup : ComponentSystemGroup
    {
    }
}
