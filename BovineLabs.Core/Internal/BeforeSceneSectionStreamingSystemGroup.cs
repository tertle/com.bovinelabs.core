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
